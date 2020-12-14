// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Ngsa.Middleware;

namespace Ngsa.App.Controllers
{
    /// <summary>
    /// Handles query requests from the controllers
    /// </summary>
    public static class DataService
    {
        // json serialization options
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            IgnoreNullValues = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        // http client used to call data layer
        private static readonly HttpClient Client = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:4122"),
        };

        /// <summary>
        /// Call the data access layer proxy using a path and query string
        /// </summary>
        /// <typeparam name="T">Result Type</typeparam>
        /// <param name="path">path</param>
        /// <param name="queryString">query string</param>
        /// <returns>IActionResult</returns>
        public static async Task<IActionResult> Read<T>(string path, string queryString = "")
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            string fullPath = path.Trim();

            if (!string.IsNullOrWhiteSpace(queryString))
            {
                fullPath += $"?{queryString.Trim()}";
            }

            try
            {
                string res = await Client.GetStringAsync(fullPath).ConfigureAwait(false);

                T obj = System.Text.Json.JsonSerializer.Deserialize<T>(res, Options);

                return new JsonResult(obj, Options);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return new BadRequestResult();
            }
        }

        /// <summary>
        /// Call the data access layer based on the path in the request
        /// </summary>
        /// <typeparam name="T">return type</typeparam>
        /// <param name="request">http request</param>
        /// <returns>IActionResult</returns>
        public static async Task<IActionResult> Read<T>(HttpRequest request)
        {
            try
            {
                string res = await Client.GetStringAsync(request?.Path.ToString() + request?.QueryString.ToString()).ConfigureAwait(false);

                T obj = System.Text.Json.JsonSerializer.Deserialize<T>(res, Options);

                return new JsonResult(obj, Options);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return CreateResult(ex.Message, HttpStatusCode.BadRequest);
            }
        }

        /// <summary>
        /// ContentResult factory
        /// </summary>
        /// <param name="message">string</param>
        /// <param name="statusCode">int</param>
        /// <returns>JsonResult</returns>
        public static JsonResult CreateResult(string message, HttpStatusCode statusCode)
        {
            return new JsonResult(new ErrorResult { Error = statusCode, Message = message })
            {
                StatusCode = (int)statusCode,
            };
        }
    }
}
