// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Ngsa.Middleware;

namespace Ngsa.App
{
    public static class NgsaLogExtensions
    {
        public static NgsaLog EnrichLog(this NgsaLog log)
        {
            log.Data.Remove("podType");
            log.Data.Remove("region");
            log.Data.Remove("zone");

            log.Data.Add("podType", App.PodType);
            log.Data.Add("region", App.Region);
            log.Data.Add("zone", App.Zone);

            return log;
        }
    }
}
