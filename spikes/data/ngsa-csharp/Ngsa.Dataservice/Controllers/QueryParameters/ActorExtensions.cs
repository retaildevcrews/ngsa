﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Ngsa.Middleware;

namespace Ngsa.Middleware
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
            if (actorQueryParameters == null)
            {
                throw new ArgumentNullException(nameof(actorQueryParameters));
            }

            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

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
