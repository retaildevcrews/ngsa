// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Ngsa.DataService.DataAccessLayer;
using Ngsa.Middleware;

namespace Ngsa.DataService.Controllers
{
    /// <summary>
    /// Handle the single /api/genres requests
    /// </summary>
    [Route("api/[controller]")]
    public class GenresController : Controller
    {
        private static readonly NgsaLogger Logger = new NgsaLogger(typeof(GenresController).FullName, new NgsaLoggerConfiguration { LogLevel = LogLevel.Warning });

        private readonly ILogger logger;
        private readonly IDAL dal;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenresController"/> class.
        /// </summary>
        /// <param name="logger">log instance</param>
        /// <param name="dal">data access layer instance</param>
        public GenresController(ILogger<GenresController> logger)
        {
            this.logger = logger;
            dal = App.CacheDal;
        }

        /// <summary>
        /// Returns a JSON string array of Genre
        /// </summary>
        /// <response code="200">JSON array of strings or empty array if not found</response>
        /// <returns>IActionResult</returns>
        [HttpGet]
        public async Task<IActionResult> GetGenresAsync()
        {
            // TODO - ILogger
            Microsoft.AspNetCore.Http.HttpContext context = HttpContext;
            string message = "Test Error";
            string key1 = "value1";
            string key2 = "value2";

            logger.LogError(
                new EventId(123, nameof(GetGenresAsync)),
                "{message} {context} {key1} {key2}",
                message,
                context,
                key1,
                key2);

            // TODO - custom logger
            Logger.LogError(
                new EventId(123, nameof(GetGenresAsync)),
                "Test Error",
                null,
                HttpContext,
                new Dictionary<string, string>
                {
                    { "key1", "value1" },
                    { "key2", "value2" },
                });

            // get list of genres as list of string
            return await ResultHandler.Handle2(HttpContext, dal.GetGenresAsync(), nameof(GetGenresAsync), Constants.GenresControllerException, logger).ConfigureAwait(false);
        }
    }
}
