﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using CSE.NextGenApp;
using Microsoft.AspNetCore.Http;

namespace CSE.NextGenApp.Extensions
{
    public static class ActorExtensions
    {
        /// <summary>
        /// Add parameters to the method name if specified in the query string
        /// </summary>
        /// <param name="actorQueryParameters">Actor query parameters</param>
        /// <param name="httpContext">HttpContext</param>
        /// <returns>method name</returns>
        public static string GetMethodText(this ActorQueryParameters actorQueryParameters, HttpContext httpContext)
        {
            _ = actorQueryParameters ?? throw new ArgumentNullException(nameof(actorQueryParameters));
            _ = httpContext ?? throw new ArgumentNullException(nameof(httpContext));

            string method = "GetActors";

            // add the query parameters to the method name if exists
            if (httpContext.Request.Query.ContainsKey("q"))
            {
                method = $"{method}:q:{actorQueryParameters.Q}";
            }

            if (httpContext.Request.Query.ContainsKey("pageNumber"))
            {
                method = $"{method}:pageNumber:{actorQueryParameters.PageNumber}";
            }

            if (httpContext.Request.Query.ContainsKey("pageSize"))
            {
                method = $"{method}:pageSize:{actorQueryParameters.PageSize}";
            }

            return method;
        }
    }
}
