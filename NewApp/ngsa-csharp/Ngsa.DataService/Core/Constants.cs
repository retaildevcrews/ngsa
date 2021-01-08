// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace Ngsa.DataService
{
    /// <summary>
    /// Application constants
    /// </summary>
    public sealed class Constants
    {
        // if port is changed, also update value in the Dockerfiles
        public const string Port = "4122";

        public const int DefaultPageSize = 100;
        public const int MaxPageSize = 1000;
        public const int MaxReqSecBeforeCache = 50;
        public const int CacheDuration = 300;

        public const int GracefulShutdownTimeout = 10;
    }
}
