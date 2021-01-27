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
        public Dictionary<string, string> Data { get; } = new Dictionary<string, string>();

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

        public void LogWarning(EventId eventId, string method, string message, HttpContext context = null)
        {
            if (LogLevel >= LogLevel.Warning)
            {
                Dictionary<string, object> d = GetDictionary(eventId, method, message, LogLevel.Warning, context);

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(JsonSerializer.Serialize(d, Options));
                Console.ResetColor();
            }
        }

        public void LogError(EventId eventId, string method, string message, HttpContext context = null, Exception ex = null)
        {
            if (LogLevel >= LogLevel.Error)
            {
                Dictionary<string, object> d = GetDictionary(eventId, method, message, LogLevel.Error, context);

                if (ex != null)
                {
                    d.Add("ExceptionType", ex.GetType().FullName);
                    d.Add("ExceptionMessage", ex.Message);
                }

                // display the error
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(JsonSerializer.Serialize(d, Options));
                Console.ResetColor();
            }
        }

        public void LogError(string method, string message, HttpContext context = null, Exception ex = null)
        {
            if (LogLevel >= LogLevel.Error)
            {
                Dictionary<string, object> d = GetDictionary(method, message, LogLevel.Error, context);

                if (ex != null)
                {
                    d.Add("ExceptionType", ex.GetType().FullName);
                    d.Add("ExceptionMessage", ex.Message);
                }

                // display the error
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Error.WriteLine(JsonSerializer.Serialize(d, Options));
                Console.ResetColor();
            }
        }

        private Dictionary<string, object> GetDictionary(EventId eventId, string method, string message, LogLevel logLevel, HttpContext context = null)
        {
            Dictionary<string, object> data = GetDictionary(method, message, logLevel, context);

            if (eventId.Id > 0)
            {
                data.Add("EventId", eventId.Id);
            }

            if (!string.IsNullOrWhiteSpace(eventId.Name))
            {
                data.Add("EventName", eventId.Name);
            }

            return data;
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
    }
}
