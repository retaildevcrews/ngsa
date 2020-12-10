// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CSE.NextGenSymmetricApp;
using Microsoft.AspNetCore.Http;
using Microsoft.CorrelationVector;
using Microsoft.Extensions.Options;

namespace CSE.Middleware
{
    /// <summary>
    /// Simple aspnet core middleware that logs requests to the console
    /// </summary>
    public class Logger
    {
        private const string IpHeader = "X-Client-IP";

        private static readonly List<int> RPS = new List<int>();
        private static int counter;
        private static Timer statsTimer;

        // next action to Invoke
        private readonly RequestDelegate next;
        private readonly LoggerOptions options;

        /// <summary>
        /// Initializes a new instance of the <see cref="Logger"/> class.
        /// </summary>
        /// <param name="next">RequestDelegate</param>
        /// <param name="options">LoggerOptions</param>
        public Logger(RequestDelegate next, IOptions<LoggerOptions> options)
        {
            // save for later
            this.next = next;
            this.options = options?.Value;

            if (this.options == null)
            {
                // use default
                this.options = new LoggerOptions();
            }
        }

        public static int RequestsPerSecond => RPS.Count > 0 ? RPS[0] : counter;

        /// <summary>
        /// Start a timer that summarizes the requests every period
        /// </summary>
        /// <param name="delay">initial delay (ms - min 5000)</param>
        /// <param name="period">delay per run (ms - min 1000)</param>
        public static void StartCounterTime(int delay, int period)
        {
            delay = delay < 5000 ? 5000 : delay;
            period = period < 1000 ? 1000 : period;

            statsTimer = new Timer(RollCounter, null, delay - DateTime.UtcNow.Millisecond, period);
        }

        /// <summary>
        /// Called by aspnet pipeline
        /// </summary>
        /// <param name="context">HttpContext</param>
        /// <returns>Task (void)</returns>
        public async Task Invoke(HttpContext context)
        {
            if (context == null)
            {
                return;
            }

            CorrelationVector cv;
            DateTime dtStart = DateTime.Now;
            double duration = 0;
            double ttfb = 0;

            cv = ExtendCVector(context);

            // Invoke next handler
            if (next != null)
            {
                await next.Invoke(context).ConfigureAwait(false);
            }

            // compute request duration
            duration = Math.Round(DateTime.Now.Subtract(dtStart).TotalMilliseconds, 2);
            ttfb = ttfb == 0 ? duration : ttfb;

            // don't log favicon.ico 404s
            if (context.Request.Path.StartsWithSegments("/favicon.ico", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            LogRequest(context, cv, ttfb, duration);
        }

        // roll request counter into list
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1801:Review unused parameters", Justification = "required by delegate")]
        private static void RollCounter(object state)
        {
            RPS.Insert(0, Interlocked.Exchange(ref counter, 0));

            while (RPS.Count > 600)
            {
                RPS.RemoveAt(RPS.Count - 1);
            }
        }

        // extend correlation vector
        private static CorrelationVector ExtendCVector(HttpContext context)
        {
            CorrelationVector cv;

            if (context.Request.Headers.ContainsKey(CorrelationVector.HeaderName))
            {
                try
                {
                    // extend the correlation vector
                    cv = CorrelationVector.Extend(context.Request.Headers[CorrelationVector.HeaderName].ToString());
                }
                catch
                {
                    // create a new correlation vector
                    cv = new CorrelationVector(CorrelationVectorVersion.V2);
                }
            }
            else
            {
                // create a new correlation vector
                cv = new CorrelationVector(CorrelationVectorVersion.V2);
            }

            return cv;
        }

        // log the request
        private static void LogRequest(HttpContext context, CorrelationVector cv, double ttfb, double duration)
        {
            DateTime dt = DateTime.UtcNow;

            Dictionary<string, object> log = new Dictionary<string, object>
            {
                { "Date", dt },
                { "StatusCode", context.Response.StatusCode },
                { "TTFB", ttfb },
                { "Duration", duration },
                { "Verb", context.Request.Method },
                { "Path", GetPathAndQuerystring(context.Request) },
                { "Host", context.Request.Headers["Host"].ToString() },
                { "ClientIP", GetClientIp(context) },
                { "UserAgent", context.Request.Headers["User-Agent"].ToString() },
                { "CVector", cv.Value },
                { "CosmosName", App.CosmosName },
                { "CosmosQueryId", "todo" },
                { "CosmosRUs", 1.23 },
                { "Region", App.Region },
                { "Zone", App.Zone },
                { "PodType", App.PodType },
            };

            Interlocked.Increment(ref counter);

            // write the results to the console
            Console.WriteLine(JsonSerializer.Serialize(log));
        }

        // get the client IP address from the request / headers
        private static string GetClientIp(HttpContext context)
        {
            string clientIp = context.Connection.RemoteIpAddress.ToString();

            // check for the forwarded header
            if (context.Request.Headers.ContainsKey(IpHeader))
            {
                clientIp = context.Request.Headers[IpHeader].ToString();
            }

            // remove IP6 local address
            return clientIp.Replace("::ffff:", string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Return the path and query string if it exists
        /// </summary>
        /// <param name="request">HttpRequest</param>
        /// <returns>string</returns>
        private static string GetPathAndQuerystring(HttpRequest request)
        {
            return request?.Path.ToString() + request?.QueryString.ToString();
        }
    }
}
