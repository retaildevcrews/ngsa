﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;

namespace CSE.NextGenSymmetricApp.Extensions
{
    public static class MovieExtensions
    {
        /// <summary>
        /// Add parameters to the method name if specified in the query string
        /// </summary>
        /// <param name="movieQueryParameters">movie query parameters</param>
        /// <param name="httpContext">HttpContext</param>
        /// <returns>method name</returns>
        public static string GetMethodText(this MovieQueryParameters movieQueryParameters, HttpContext httpContext)
        {
            if (movieQueryParameters == null)
            {
                throw new ArgumentNullException(nameof(movieQueryParameters));
            }

            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            string method = "GetMovies";

            if (httpContext?.Request?.Query == null)
            {
                return method;
            }

            // add the query parameters to the method name if exists
            if (httpContext.Request.Query.ContainsKey("q"))
            {
                method = $"{method}:q:{movieQueryParameters.Q}";
            }

            if (httpContext.Request.Query.ContainsKey("genre"))
            {
                method = $"{method}:genre:{movieQueryParameters.Genre}";
            }

            if (httpContext.Request.Query.ContainsKey("year"))
            {
                method = $"{method}:year:{movieQueryParameters.Year}";
            }

            if (httpContext.Request.Query.ContainsKey("rating"))
            {
                method = $"{method}:rating:{movieQueryParameters.Rating}";
            }

            if (httpContext.Request.Query.ContainsKey("actorId"))
            {
                method = $"{method}:actorId:{movieQueryParameters.ActorId}";
            }

            if (httpContext.Request.Query.ContainsKey("pageNumber"))
            {
                method = $"{method}:pageNumber:{movieQueryParameters.PageNumber}";
            }

            if (httpContext.Request.Query.ContainsKey("pageSize"))
            {
                method = $"{method}:pageSize:{movieQueryParameters.PageSize}";
            }

            return method;
        }
    }
}
