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
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            IgnoreNullValues = true,
        };

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

            return logger;
        }

        public void LogInformation(string method, string message, HttpContext context = null)
        {
            if (LogLevel >= LogLevel.Information)
            {
                Dictionary<string, object> d = GetDictionary(method, message, LogLevel.Information, context);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(JsonSerializer.Serialize(d, Options));
                Console.ResetColor();
            }
        }

        public void LogWarning(string method, string message, HttpContext context = null)
        {
            if (LogLevel >= LogLevel.Warning)
            {
                Dictionary<string, object> d = GetDictionary(method, message, LogLevel.Warning, context);

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(JsonSerializer.Serialize(d, Options));
                Console.ResetColor();
            }
        }

        public void LogError(string message, Exception ex = null)
        {
            if (LogLevel >= LogLevel.Error)
            {
                return;
            }

            Dictionary<string, object> d = GetDictionary(message, LogLevel.Error);

            if (ex != null)
            {
                d.Add("ExceptionType", ex.GetType().FullName);
                d.Add("ExceptionMessage", ex.Message);
            }
            else if (Exception != null)
            {
                d.Add("ExceptionType", Exception.GetType().FullName);
                d.Add("ExceptionMessage", Exception.Message);
            }

            // display the error
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(JsonSerializer.Serialize(d, Options));
            Console.ResetColor();
        }

        private Dictionary<string, object> GetDictionary(string method, string message, LogLevel logLevel, HttpContext context = null)
        {
            Dictionary<string, object> data = new Dictionary<string, object>
            {
                { "Date", DateTime.UtcNow },
                { "LogName", Name },
                { "Method", method },
                { "Message", message },
                { "LogLevel", logLevel.ToString() },
            };

            if (EventId.Id > 0)
            {
                data.Add("EventId", EventId.Id);
            }

            if (!string.IsNullOrWhiteSpace(EventId.Name))
            {
                data.Add("EventName", EventId.Name);
            }

            if (context != null && context.Items != null)
            {
                data.Add("Path", context.Request.Path + (string.IsNullOrWhiteSpace(context.Request.QueryString.Value) ? string.Empty : context.Request.QueryString.Value));

                if (context.Items != null)
                {
                    CorrelationVector cv = CorrelationVectorExtensions.GetCorrelationVectorFromContext(context);

                    if (cv != null)
                    {
                        data.Add("CVector", cv.Value);
                    }
                }
            }

            foreach (KeyValuePair<string, string> kvp in Data)
            {
                data.Add(kvp.Key, kvp.Value);
            }

            return data;
        }

        private Dictionary<string, object> GetDictionary(string message, LogLevel logLevel)
        {
            Dictionary<string, object> data = new Dictionary<string, object>
            {
                { "Date", DateTime.UtcNow },
                { "LogName", Name },
                { "Method", Method },
                { "Message", message },
                { "LogLevel", logLevel.ToString() },
            };

            if (EventId.Id > 0)
            {
                data.Add("EventId", EventId.Id);
            }

            if (!string.IsNullOrWhiteSpace(EventId.Name))
            {
                data.Add("EventName", EventId.Name);
            }

            if (Context != null && Context.Items != null)
            {
                data.Add("Path", Context.Request.Path + (string.IsNullOrWhiteSpace(Context.Request.QueryString.Value) ? string.Empty : Context.Request.QueryString.Value));

                if (Context.Items != null)
                {
                    CorrelationVector cv = CorrelationVectorExtensions.GetCorrelationVectorFromContext(Context);

                    if (cv != null)
                    {
                        data.Add("CVector", cv.Value);
                    }
                }
            }

            foreach (KeyValuePair<string, string> kvp in Data)
            {
                data.Add(kvp.Key, kvp.Value);
            }

            return data;
        }
    }
}
