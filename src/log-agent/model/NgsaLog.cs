// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;

namespace LogAgent
{
    /// <summary>
    /// Log class
    /// </summary>
    public class NgsaLog
    {
        public string NodeName { get; set; }
        public string PodName { get; set; }
        public string PodType { get; set; }
        public string PodNamespace { get; set; }
        public string Category { get; set; } = string.Empty;
        public long ContentLength { get; set; }
        public string ClientIP { get; set; } = string.Empty;
        public string CorrelationVector { get; set; }
        public string CosmosName { get; set; } = string.Empty;
        public string CosmosQueryId { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public double Duration { get; set; }
        public int ErrorCount { get; set; }
        public bool Failed { get; set; }
        public string Host { get; set; } = string.Empty;
        public string Path { get; set; }
        public int Quartile { get; set; }
        public string Region { get; set; } = string.Empty;

        public string Server { get; set; }
        public int StatusCode { get; set; }
        public string Tag { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public bool Validated { get; set; } = true;
        public string Verb { get; set; } = "GET";
        public string Zone { get; set; } = string.Empty;
    }
}
