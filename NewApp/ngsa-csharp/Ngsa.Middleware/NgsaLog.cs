// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.CorrelationVector;
using Microsoft.Extensions.Logging;

namespace Ngsa.Middleware
{
    public class NgsaLog
    {
        private static readonly Dictionary<int, NgsaLog> Loggers = new Dictionary<int, NgsaLog>();
        private static int counter = 0;

        public string Name { get; set; } = string.Empty;
        public LogLevel LogLevel { get; set; } = LogLevel.Information;
        public string ErrorMessage { get; set; } = string.Empty;
        public string NotFoundError { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public Exception Exception { get; set; } = null;
        public EventId EventId { get; set; } = new EventId(-1, string.Empty);
        public HttpContext Context { get; set; } = null;
        public Dictionary<string, string> Data { get; } = new Dictionary<string, string>();

        public NgsaLog GetLogger(string method, HttpContext context = null)
        {
            NgsaLog logger = new NgsaLog
            {
                Name = Name,
                ErrorMessage = ErrorMessage,
                NotFoundError = NotFoundError,
                LogLevel = LogLevel,

                Method = method,
                Context = context,
            };

            // get the next key
            while (Loggers.ContainsKey(counter))
            {
                if (counter == int.MaxValue)
                {
                    counter = 0;
                }

                counter++;
            }

            // todo - recycle loggers
            // use iDisposable?
            //Loggers.Add(counter, logger);

            return logger;
        }

        public void LogInformation(string message)
        {
            const LogLevel logLevel = LogLevel.Information;

            if (LogLevel <= logLevel)
            {
                Dictionary<string, object> d = GetDictionary(message, logLevel);

                Console.WriteLine(JsonSerializer.Serialize(d));
            }
        }

        public void LogWarning(string message)
        {
            const LogLevel logLevel = LogLevel.Warning;

            if (LogLevel > logLevel)
            {
                return;
            }

            Dictionary<string, object> d = GetDictionary(message, logLevel);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(JsonSerializer.Serialize(d));
            Console.ResetColor();
        }

        public void LogError(string message, Exception ex = null)
        {
            const LogLevel logLevel = LogLevel.Error;

            if (LogLevel > logLevel)
            {
                return;
            }

            Dictionary<string, object> d = GetDictionary(message, logLevel);

            if (ex != null)
            {
                d.Add("exceptionType", ex.GetType());
                d.Add("exceptionMessage", ex.Message);

                //d.Add("exceptionText", ex.ToString());
            }
            else if (Exception != null)
            {
                d.Add("exceptionType", Exception.GetType().FullName);
                d.Add("exceptionMessage", Exception.Message);

                //d.Add("exceptionText", Exception.ToString());
            }

            // display the error
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(JsonSerializer.Serialize(d));
            Console.ResetColor();
        }

        private Dictionary<string, object> GetDictionary(string message, LogLevel logLevel)
        {
            Dictionary<string, object> data = new Dictionary<string, object>
            {
                { "logName", Name },
                { "method", Method },
                { "message", message },
                { "logLevel", logLevel.ToString() },
            };

            if (EventId.Id > 0)
            {
                data.Add("eventId", EventId.Id);
            }

            if (!string.IsNullOrEmpty(EventId.Name))
            {
                data.Add("eventName", EventId.Name);
            }

            if (Context != null && Context.Items != null)
            {
                data.Add("path", Context.Request.Path.ToString());

                if (!string.IsNullOrEmpty(Context.Request.QueryString.ToString()))
                {
                    data.Add("queryString", Context.Request.QueryString.ToString()[1..]);
                }

                if (Context.Items != null)
                {
                    CorrelationVector cv = Middleware.CorrelationVectorExtensions.GetCorrelationVectorFromContext(Context);

                    if (cv != null)
                    {
                        data.Add("CVector", cv.Value);
                    }
                }
            }

            if (Data != null)
            {
                foreach (KeyValuePair<string, string> kvp in Data)
                {
                    data.Add(kvp.Key, kvp.Value);
                }
            }

            return data;
        }
    }
}
