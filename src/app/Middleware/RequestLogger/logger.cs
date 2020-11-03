// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Globalization;
using System.Threading.Tasks;
using CSE.NextGenSymmetricApp;
using CSE.NextGenSymmetricApp.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.CorrelationVector;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace CSE.Middleware
{
    /// <summary>
    /// Simple aspnet core middleware that logs requests to the console
    /// </summary>
    public class Logger
    {
        private const string IpHeader = "X-Client-IP";
        private const string CVHeader = "X-Correlation-Vector";

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

            // set start time
            DateTime dtStart = DateTime.Now;

            CorrelationVector cv;

            if (context.Request.Headers.ContainsKey(CVHeader))
            {
                try
                {
                    // extend the correlation vector
                    cv = CorrelationVector.Extend(context.Request.Headers[CVHeader].ToString());
                }
                catch
                {
                    // create a new correlation vector
                    cv = new CorrelationVector();
                }
            }
            else
            {
                // create a new correlation vector
                cv = new CorrelationVector();
            }

            // Invoke next handler
            if (next != null)
            {
                await next.Invoke(context).ConfigureAwait(false);
            }

            // compute request duration
            double duration = DateTime.Now.Subtract(dtStart).TotalMilliseconds;

            // don't log favicon.ico 404s
            if (context.Request.Path.StartsWithSegments("/favicon.ico", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            // handle healthz composite logging
            if (LogHealthzHandled(context, duration))
            {
                return;
            }

            string clientIp = context.Connection.RemoteIpAddress.ToString().Replace("::ffff:", string.Empty, StringComparison.OrdinalIgnoreCase);

            if (context.Request.Headers.ContainsKey(IpHeader))
            {
                clientIp = context.Request.Headers[IpHeader];
            }

            // write the results to the console
            Console.WriteLine($"{DateTime.UtcNow:u}\t{context.Response.StatusCode}\t{Math.Round(duration, 1)}\t{context.Request.Method}\t{GetPathAndQuerystring(context.Request)}\t{cv.Value}\t{context.Request.Headers["Host"]}\t{clientIp}\t{App.CosmosName}\t{App.CosmosQueryId}\t{context.Request.Headers["User-Agent"]}");
        }

        /// <summary>
        /// Log the healthz results for degraded and unhealthy
        /// </summary>
        /// <param name="context">HttpContext</param>
        /// <param name="duration">double</param>
        /// <returns>bool</returns>
        private static bool LogHealthzHandled(HttpContext context, double duration)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // check if there is a HealthCheckResult item
            if (context.Items.Count > 0 && context.Items.ContainsKey(typeof(HealthCheckResult).ToString()))
            {
                HealthCheckResult hcr = (HealthCheckResult)context.Items[typeof(HealthCheckResult).ToString()];

                // log not healthy requests
                if (hcr.Status != HealthStatus.Healthy)
                {
                    string log = string.Empty;

                    // build the log message
                    log += string.Format(CultureInfo.InvariantCulture, $"{IetfCheck.ToIetfStatus(hcr.Status)}\t{duration,6:0}\t{context.Request.Headers[IpHeader]}\t{GetPathAndQuerystring(context.Request)}\n");

                    // add each not healthy check to the log message
                    foreach (object d in hcr.Data.Values)
                    {
                        if (d is HealthzCheck h && h.Status != HealthStatus.Healthy)
                        {
                            log += string.Format(CultureInfo.InvariantCulture, $"{IetfCheck.ToIetfStatus(h.Status)}\t{(long)h.Duration.TotalMilliseconds,6}\t{context.Request.Headers[IpHeader]}\t{h.Endpoint}\t({h.TargetDuration.TotalMilliseconds,1:0})\n");
                        }
                    }

                    if (hcr.Exception != null)
                    {
                        log += "HealthCheckException\n";
                        log += hcr.Exception.ToString() + "\n";
                    }

                    // write the log message
                    Console.Write(log);

                    // done logging this request
                    return true;
                }
            }

            // keep processing
            return false;
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
