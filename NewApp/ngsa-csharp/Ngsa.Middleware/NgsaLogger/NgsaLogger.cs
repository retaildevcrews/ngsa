// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.CorrelationVector;
using Microsoft.Extensions.Logging;

namespace Ngsa.Middleware
{
    /// <summary>
    /// Simple aspnet core middleware that logs requests to the console
    /// </summary>
    public class NgsaLogger : ILogger
    {
        private readonly ConsoleColor origColor = Console.ForegroundColor;
        private readonly string name;
        private readonly NgsaLoggerConfiguration config;

        /// <summary>
        /// Initializes a new instance of the <see cref="NgsaLogger"/> class.
        /// </summary>
        /// <param name="name">Logger Name</param>
        /// <param name="config">Logger Config</param>
        public NgsaLogger(string name, NgsaLoggerConfiguration config)
        {
            this.name = name;
            this.config = config;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return default;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel >= config.LogLevel;
        }

        public void LogError(EventId eventId, string message, Exception ex = null, HttpContext context = null, Dictionary<string, string> data = null)
        {
            const LogLevel logLevel = LogLevel.Error;

            if (!IsEnabled(logLevel))
            {
                return;
            }

            Dictionary<string, object> d = new Dictionary<string, object>
            {
                { "logName", name },
                { "logLevel", logLevel.ToString() },
                { "eventId", eventId.Id },
                { "eventName", eventId.Name },
                { "message", message },
            };

            if (ex != null)
            {
                d.Add("exceptionType", ex.GetType());
                d.Add("exceptionMessage", ex.Message);
            }

            if (context != null && context.Items != null)
            {
                CorrelationVector cv = CorrelationVectorExtensions.GetCorrelationVectorFromContext(context);

                if (cv != null)
                {
                    d.Add("CVector", cv.Value);
                }
            }

            if (data != null)
            {
                foreach (var kvp in data)
                {
                    d.Add(kvp.Key, kvp.Value);
                }
            }

            // display the error
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(JsonSerializer.Serialize(d));
            Console.ForegroundColor = origColor;

            // free the memory for GC
            d.Clear();
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            Dictionary<string, object> d = new Dictionary<string, object>
            {
                { "logName", name },
                { "logLevel", logLevel.ToString() },
                { "eventId", eventId.Id },
                { "eventName", eventId.Name },
            };

            // convert state to list
            if (state is IReadOnlyList<KeyValuePair<string, object>> roList)
            {
                List<KeyValuePair<string, object>> list = roList.ToList();

                switch (list.Count)
                {
                    case 0:
                        break;
                    case 1:
                        // clean up name
                        list.Add(new KeyValuePair<string, object>("message", list[0].Value));
                        list.RemoveAt(0);
                        break;
                    default:
                        // remove formatting key-value
                        list.RemoveAt(list.Count - 1);
                        break;
                }

                for (int i = list.Count - 1; i >= 0; i--)
                {
                    // get correlation vector from HttpContext.Items
                    if (list[i].Value is HttpContext c)
                    {
                        list.RemoveAt(i);

                        if (c != null && c.Items != null)
                        {
                            CorrelationVector cv = CorrelationVectorExtensions.GetCorrelationVectorFromContext(c);

                            if (cv != null)
                            {
                                d.Add("CVector", cv.Value);
                            }
                        }

                        break;
                    }
                }

                // add remaining state
                foreach (var kvp in list)
                {
                    d.Add(kvp.Key.ToString(), kvp.Value.ToString());
                }
            }

            // add exception
            if (exception != null)
            {
                d.Add("Exception", exception.Message);
            }

            if (logLevel >= LogLevel.Error)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(JsonSerializer.Serialize(d));
            }
            else
            {
                Console.ForegroundColor = logLevel == LogLevel.Warning ? ConsoleColor.Yellow : ConsoleColor.Green;
                Console.WriteLine(JsonSerializer.Serialize(d));
            }

            // free the memory for GC
            d.Clear();

            Console.ForegroundColor = origColor;
        }
    }
}
