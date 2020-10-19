// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace CSE.NextGenSymmetricApp
{
    /// <summary>
    /// Application constants
    /// </summary>
    public sealed class Constants
    {
        public const string SwaggerTitle = "Next Gen Symmetric Apps";
        public const string SwaggerPath = "/swagger.json";

        // if port is changed, also update value in the Dockerfiles
        public const string Port = "4120";

        public const string ActorsControllerException = "ActorsControllerException";
        public const string GenresControllerException = "GenresControllerException";
        public const string HealthzControllerException = "HealthzControllerException";
        public const string MoviesControllerException = "MoviesControllerException";
        public const string FeaturedControllerException = "FeaturedControllerException";

        public const int DefaultPageSize = 100;
        public const int MaxPageSize = 1000;

        public const int HealthzCacheDuration = 60;
        public const int GracefulShutdownTimeout = 10;
    }
}
