// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Ngsa.Middleware;

namespace Ngsa.App
{
    public static class NgsaLogExtensions
    {
        public static NgsaLog AddPodType(this NgsaLog log)
        {
            log.Data.Remove("PodType");

            log.Data.Add("PodType", App.PodType);

            return log;
        }
    }
}
