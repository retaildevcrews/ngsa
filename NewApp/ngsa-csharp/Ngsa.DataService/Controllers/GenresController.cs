// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CorrelationVector;
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
        private static readonly NgsaLog Logger = new NgsaLog
        {
            Name = typeof(GenresController).FullName,
            LogLevel = App.AppLogLevel,
            ErrorMessage = "GenreControllerException",
        };
        private readonly IDAL dal;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenresController"/> class.
        /// </summary>
        /// <param name="dal">data access layer instance</param>
        public GenresController()
        {
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
            NgsaLog myLogger = Logger.GetLogger(nameof(GetGenresAsync), HttpContext);

            // todo - modify handle once logging decision is made
            return await ResultHandler.Handle(dal.GetGenresAsync(), myLogger).ConfigureAwait(false);

            // TODO - ILogger - Leave this for now as an example
            //Microsoft.AspNetCore.Http.HttpContext context = HttpContext;
            //string message = "Test Error";
            //string key1 = "value1";
            //string key2 = "value2";

            //logger.LogError(
            //    new EventId(123, nameof(GetGenresAsync)),
            //    "{message} {context} {key1} {key2}",
            //    message,
            //    context,
            //    key1,
            //    key2);

            //// get list of genres as list of string
            //return await ResultHandler.Handle(HttpContext, dal.GetGenresAsync(), nameof(GetGenresAsync), Constants.GenresControllerException, logger).ConfigureAwait(false);
        }
    }
}
