// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Ngsa.Middleware;

namespace Ngsa.App
{
    public static class NgsaLogExtensions
    {
        public static NgsaLog EnrichLog(this NgsaLog log)
        {
            log.Data.Remove("PodType");
            log.Data.Remove("Region");
            log.Data.Remove("Zone");

            log.Data.Add("PodType", App.PodType);
            log.Data.Add("Region", App.Region);
            log.Data.Add("Zone", App.Zone);

            return log;
        }
    }
}
