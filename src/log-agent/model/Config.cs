// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;

namespace LogAgent
{
    /// <summary>
    /// Config class
    /// </summary>
    public class Config
    {
        public string WorkspaceId { get; set; }
        public string SharedKey { get; set; }
        public string LogName { get; set; }
        public int Delay { get; set; }
        public string NodeName { get; set; }
        public string PodName { get; set; }
        public string PodNamespace { get; set; }
        public string PodIP { get; set; }
        public string Region { get; set; }
        public string Zone { get; set; }

        public Config()
        {
            // default value
            Delay = 10;

            // read env vars
            NodeName = Environment.GetEnvironmentVariable("NodeName");
            PodName = Environment.GetEnvironmentVariable("PodName");
            PodNamespace = Environment.GetEnvironmentVariable("PodNamespace");
            PodIP = Environment.GetEnvironmentVariable("PodIP");
            Region = Environment.GetEnvironmentVariable("Region");
            Zone = Environment.GetEnvironmentVariable("Zone");
        }
    }
}
