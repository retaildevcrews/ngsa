// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CorrelationVector;
using Ngsa.LodeRunner.Model;
using Ngsa.LodeRunner.Validators;
using Ngsa.Middleware;

namespace Ngsa.LodeRunner
{
    /// <summary>
    /// LodeRunner Test
    /// </summary>
    public partial class ValidationTest
    {
        /// <summary>
        /// Correlation Vector http header name
        /// </summary>
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            IgnoreNullValues = true,
        };

        private static List<Request> requestList;

        private readonly Dictionary<string, PerfTarget> targets = new Dictionary<string, PerfTarget>();
        private Config config;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationTest"/> class
        /// </summary>
        /// <param name="config">Config</param>
        public ValidationTest(Config config)
        {
            if (config == null || config.Files == null || config.Server == null || config.Server.Count == 0)
            {
                throw new ArgumentNullException(nameof(config));
            }

            this.config = config;

            // load the performance targets
            targets = LoadPerfTargets();

            // load the requests from json files
            requestList = LoadValidateRequests(config.Files);

            if (requestList == null || requestList.Count == 0)
            {
                throw new ArgumentException("RequestList is empty");
            }
        }

        /// <summary>
        /// Gets UtcNow as an ISO formatted date string
        /// </summary>
        public static string Now => DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

        /// <summary>
        /// Run the validation test one time
        /// </summary>
        /// <param name="config">configuration</param>
        /// <param name="token">cancellation token</param>
        /// <returns>bool</returns>
        public async Task<int> RunOnce(Config config, CancellationToken token)
        {
            if (config == null)
            {
                Console.WriteLine("RunOnce:Config is null");
                return -1;
            }

            DisplayStartupMessage(config);

            int duration;
            PerfLog pl;
            int errorCount = 0;
            int validationFailureCount = 0;

            // loop through each server
            for (int ndx = 0; ndx < config.Server.Count; ndx++)
            {
                // reset error counts
                if (config.Server.Count > 0)
                {
                    if (ndx > 0)
                    {
                        Console.WriteLine();
                        errorCount = 0;
                        validationFailureCount = 0;
                    }
                }

                using HttpClient client = OpenClient(ndx);

                // send each request
                foreach (Request r in requestList)
                {
                    try
                    {
                        if (token.IsCancellationRequested)
                        {
                            break;
                        }

                        // stop after MaxErrors errors
                        if ((errorCount + validationFailureCount) >= config.MaxErrors)
                        {
                            break;
                        }

                        // execute the request
                        pl = await ExecuteRequest(client, config.Server[ndx], r).ConfigureAwait(false);

                        if (pl.Failed)
                        {
                            errorCount++;
                        }

                        if (!pl.Failed && !pl.Validated)
                        {
                            validationFailureCount++;
                        }

                        // sleep if configured
                        if (config.Sleep > 0)
                        {
                            duration = config.Sleep - (int)pl.Duration;

                            if (duration > 0)
                            {
                                await Task.Delay(duration, token).ConfigureAwait(false);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // ignore any exception caused by ctl-c or stop signal
                        if (token.IsCancellationRequested)
                        {
                            break;
                        }

                        // log error and keep processing
                        Console.WriteLine($"{Now}\tException: {ex.Message}");
                        errorCount++;
                    }
                }

                // log validation failure count
                if (validationFailureCount > 0)
                {
                    Console.WriteLine($"Validation Errors: {validationFailureCount}");
                }

                // log error count
                if (errorCount > 0)
                {
                    Console.WriteLine($"Failed: {errorCount} Errors");
                }

                // log MaxErrors exceeded
                if (errorCount + validationFailureCount >= config.MaxErrors)
                {
                    Console.Write($"Failed: Errors: {errorCount + validationFailureCount} >= MaxErrors: {config.MaxErrors}");
                }
            }

            // return non-zero exit code on failure
            return errorCount > 0 || validationFailureCount >= config.MaxErrors ? errorCount + validationFailureCount : 0;
        }

        /// <summary>
        /// Run the validation tests in a loop
        /// </summary>
        /// <param name="config">Config</param>
        /// <param name="token">CancellationToken</param>
        /// <returns>0 on success</returns>
        public int RunLoop(Config config, CancellationToken token)
        {
            this.config = config ?? throw new ArgumentNullException(nameof(config));

            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            DateTime dtMax = DateTime.MaxValue;

            // only run for duration (seconds)
            if (config.Duration > 0)
            {
                dtMax = DateTime.UtcNow.AddSeconds(config.Duration);
            }

            if (config.Sleep < 1)
            {
                config.Sleep = 1;
            }

            DisplayStartupMessage(config);

            List<TimerRequestState> states = new List<TimerRequestState>();

            foreach (string svr in config.Server)
            {
                // create the shared state
                TimerRequestState state = new TimerRequestState
                {
                    Server = svr,
                    Client = OpenHttpClient(svr),
                    MaxIndex = requestList.Count,
                    Test = this,
                    RequestList = requestList,

                    // current hour
                    CurrentLogTime = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, DateTime.UtcNow.Hour, 0, 0),

                    Token = token,
                };

                if (config.Random)
                {
                    state.Random = new Random(DateTime.UtcNow.Millisecond);
                }

                states.Add(state);

                state.Run(config.Sleep, config.MaxConcurrent);
            }

            int frequency = int.MaxValue;
            int initialDelay = int.MaxValue;

            if (config.SummaryMinutes > 0)
            {
                foreach (TimerRequestState trs in states)
                {
                    // get current summary
                    int cMin = DateTime.UtcNow.Minute / config.SummaryMinutes * config.SummaryMinutes;
                    trs.CurrentLogTime = trs.CurrentLogTime.AddMinutes(cMin);
                    initialDelay = (int)trs.CurrentLogTime.AddMinutes(config.SummaryMinutes).Subtract(DateTime.UtcNow).TotalMilliseconds;
                    frequency = config.SummaryMinutes * 60 * 1000;

                    // start the summary log timer
                    using Timer logTimer = new Timer(new TimerCallback(SummaryLogTask), trs, initialDelay, frequency);
                }
            }

            try
            {
                // run the wait loop
                if (dtMax == DateTime.MaxValue)
                {
                    Task.Delay(-1, token).Wait(token);
                }
                else
                {
                    // wait one hour to keep total milliseconds from overflowing
                    while (dtMax.Subtract(DateTime.UtcNow).TotalHours > 1)
                    {
                        Task.Delay(60 * 60 * 1000, token).Wait(token);
                    }

                    int delay = (int)dtMax.Subtract(DateTime.UtcNow).TotalMilliseconds;

                    if (delay > 0)
                    {
                        Task.Delay(delay, token).Wait(token);
                    }
                }
            }
            catch (TaskCanceledException tce)
            {
                // log exception
                if (!tce.Task.IsCompleted)
                {
                    Console.WriteLine($"Exception: {tce}");
                    return 1;
                }

                // task is completed
                return 0;
            }
            catch (OperationCanceledException oce)
            {
                // log exception
                if (!token.IsCancellationRequested)
                {
                    Console.Write($"Exception: {oce}");
                    return 1;
                }

                // Operation was cancelled
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex}");
                return -1;
            }

            // graceful exit
            return 0;
        }

        /// <summary>
        /// Execute a single validation test
        /// </summary>
        /// <param name="client">http client</param>
        /// <param name="server">server URL</param>
        /// <param name="request">Request</param>
        /// <returns>PerfLog</returns>
        public async Task<PerfLog> ExecuteRequest(HttpClient client, string server, Request request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            PerfLog perfLog;
            ValidationResult valid;

            // send the request
            using (HttpRequestMessage req = new HttpRequestMessage(new HttpMethod(request.Verb), request.Path))
            {
                DateTime dt = DateTime.UtcNow;

                // add the headers to the http request
                if (request.Headers != null && request.Headers.Count > 0)
                {
                    foreach (string key in request.Headers.Keys)
                    {
                        req.Headers.Add(key, request.Headers[key]);
                    }
                }

                // create correlation vector and add to headers
                CorrelationVector cv = new CorrelationVector(CorrelationVectorVersion.V2);
                req.Headers.Add(CorrelationVector.HeaderName, cv.Value);

                // add the body to the http request
                if (!string.IsNullOrWhiteSpace(request.Body))
                {
                    if (!string.IsNullOrWhiteSpace(request.ContentMediaType))
                    {
                        req.Content = new StringContent(request.Body, Encoding.UTF8, request.ContentMediaType);
                    }
                    else
                    {
                        req.Content = new StringContent(request.Body);
                    }
                }

                try
                {
                    // process the response
                    using HttpResponseMessage resp = await client.SendAsync(req).ConfigureAwait(false);
                    string body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                    double duration = DateTime.UtcNow.Subtract(dt).TotalMilliseconds;

                    // validate the response
                    valid = ResponseValidator.Validate(request, resp, body);

                    // check the performance
                    perfLog = CreatePerfLog(server, request, valid, duration, (long)resp.Content.Headers.ContentLength, (int)resp.StatusCode);

                    // add correlation vector to perf log
                    perfLog.CorrelationVector = cv.Value;
                    perfLog.CorrelationVectorBase = cv.GetBase();
                }
                catch (Exception ex)
                {
                    double duration = Math.Round(DateTime.UtcNow.Subtract(dt).TotalMilliseconds, 0);
                    valid = new ValidationResult { Failed = true };
                    valid.ValidationErrors.Add($"Exception: {ex.Message}");
                    perfLog = CreatePerfLog(server, request, valid, duration, 0, 500);
                }
            }

            // log the test
            LogToConsole(request, valid, perfLog);

            return perfLog;
        }

        /// <summary>
        /// Create a PerfLog
        /// </summary>
        /// <param name="server">server URL</param>
        /// <param name="request">Request</param>
        /// <param name="validationResult">validation errors</param>
        /// <param name="duration">duration</param>
        /// <param name="contentLength">content length</param>
        /// <param name="statusCode">status code</param>
        /// <returns>PerfLog</returns>
        public PerfLog CreatePerfLog(string server, Request request, ValidationResult validationResult, double duration, long contentLength, int statusCode)
        {
            if (validationResult == null)
            {
                throw new ArgumentNullException(nameof(validationResult));
            }

            // map the parameters
            PerfLog log = new PerfLog(validationResult.ValidationErrors)
            {
                Server = server,
                Tag = config.Tag,
                Path = request?.Path ?? string.Empty,
                StatusCode = statusCode,
                Category = request?.PerfTarget?.Category ?? string.Empty,
                Validated = !validationResult.Failed && validationResult.ValidationErrors.Count == 0,
                Duration = duration,
                ContentLength = contentLength,
                Failed = validationResult.Failed,
            };

            // determine the Performance Level based on category
            if (targets.ContainsKey(log.Category))
            {
                // lookup the target
                PerfTarget target = targets[log.Category];

                if (target != null &&
                    !string.IsNullOrWhiteSpace(target.Category) &&
                    target.Quartiles != null &&
                    target.Quartiles.Count == 3)
                {
                    // set to max
                    log.Quartile = target.Quartiles.Count + 1;

                    for (int i = 0; i < target.Quartiles.Count; i++)
                    {
                        // find the lowest Perf Target achieved
                        if (duration <= target.Quartiles[i])
                        {
                            log.Quartile = i + 1;
                            break;
                        }
                    }
                }
            }

            return log;
        }

        /// <summary>
        /// Display the startup message for RunLoop
        /// </summary>
        private static void DisplayStartupMessage(Config config)
        {
            Dictionary<string, object> msg = new Dictionary<string, object>
            {
                { "Date", DateTime.UtcNow },
                { "EventType", "Startup" },
                { "Version", Version.AssemblyVersion },
                { "Host", string.Join(' ', config.Server) },
                { "BaseUrl", config.BaseUrl },
                { "Files", string.Join(' ', config.Files) },
                { "Sleep", config.Sleep },
                { "MaxConcurrent", config.MaxConcurrent },
                { "Duration", config.Duration },
                { "Random", config.Random },
                { "Verbose", config.Verbose },
                { "Tag", config.Tag },
            };

            Console.WriteLine(JsonSerializer.Serialize(msg));
        }

        /// <summary>
        /// Open an http client
        /// </summary>
        /// <param name="index">index of base URL</param>
        private HttpClient OpenClient(int index)
        {
            if (index < 0 || index >= config.Server.Count)
            {
                throw new ArgumentException($"Index out of range: {index}", nameof(index));
            }

            return OpenHttpClient(config.Server[index]);
        }

        /// <summary>
        /// Opens and configures the shared HttpClient
        ///
        /// Disposed in IDispose
        /// </summary>
        /// <returns>HttpClient</returns>
        private HttpClient OpenHttpClient(string host)
        {
            HttpClient client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
            {
                Timeout = new TimeSpan(0, 0, config.Timeout),
                BaseAddress = new Uri(host),
            };
            client.DefaultRequestHeaders.Add("User-Agent", $"l8r/{Version.ShortVersion}");

            return client;
        }

        /// <summary>
        /// Summarize the requests for the hour
        /// </summary>
        /// <param name="timerState">TimerState</param>
        private void SummaryLogTask(object timerState)
        {
            if (config.SummaryMinutes < 1)
            {
                return;
            }

            if (timerState is TimerRequestState state)
            {
                // exit if cancelled
                if (state.Token.IsCancellationRequested)
                {
                    return;
                }

                // build the log entry
                string log = "{ \"logType\": \"summary\", " + $"\"logDate\": \"{state.CurrentLogTime.ToString("o", CultureInfo.InvariantCulture)}Z\", \"tag\": \"{config.Tag}\", ";

                // get the summary values
                lock (state.Lock)
                {
                    log += $"\"requestCount\": {state.Count}, ";
                    log += $"\"averageDuration\": {(state.Count > 0 ? Math.Round(state.Duration / state.Count, 2) : 0)}, ";
                    log += $"\"errorCount\": {state.ErrorCount} " + "}";

                    // reset counters
                    state.Count = 0;
                    state.Duration = 0;
                    state.ErrorCount = 0;

                    // set next log time
                    state.CurrentLogTime = state.CurrentLogTime.AddMinutes(config.SummaryMinutes);
                }

                // log the summary
                Console.WriteLine(log);
            }
        }

        /// <summary>
        /// Log the test
        /// </summary>
        /// <param name="request">Request</param>
        /// <param name="perfLog">PerfLog</param>
        private void LogToConsole(Request request, ValidationResult valid, PerfLog perfLog)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            if (valid == null)
            {
                throw new ArgumentNullException(nameof(valid));
            }

            if (perfLog == null)
            {
                throw new ArgumentNullException(nameof(perfLog));
            }

            // don't log ignore requests
            if (request.PerfTarget?.Category != "Ignore")
            {
                Dictionary<string, object> logDict = new Dictionary<string, object>
                {
                    { "Date", perfLog.Date },
                    { "Server", perfLog.Server },
                    { "StatusCode", perfLog.StatusCode },
                    { "Verb", request.Verb },
                    { "Path", perfLog.Path },
                    { "Errors", perfLog.ErrorCount },
                    { "Duration", Math.Round(perfLog.Duration, 2) },
                    { "ContentLength", perfLog.ContentLength },
                    { "CVector", perfLog.CorrelationVector },
                    { "CVectorBase", perfLog.CorrelationVectorBase },
                    { "Tag", perfLog.Tag },
                    { "Quartile", perfLog.Quartile },
                    { "Category", perfLog.Category },
                };

                // log error details
                if (config.VerboseErrors && valid.ValidationErrors.Count > 0)
                {
                    string errors = string.Empty;

                    // add up to 5 detailed errors
                    int max = valid.ValidationErrors.Count > 5 ? 5 : valid.ValidationErrors.Count;

                    for (int i = 0; i < max; i++)
                    {
                        errors += valid.ValidationErrors[i].Trim() + "\t";
                    }

                    logDict.Add("ErrorDetails", errors.Trim());
                }

                Console.WriteLine(JsonSerializer.Serialize(logDict, JsonOptions));
            }
        }
    }
}
