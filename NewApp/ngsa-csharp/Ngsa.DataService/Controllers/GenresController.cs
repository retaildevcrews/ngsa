// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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

        /// <summary>
        /// Returns a JSON string array of Genre
        /// </summary>
        /// <response code="200">JSON array of strings or empty array if not found</response>
        /// <returns>IActionResult</returns>
        [HttpGet]
        public async Task<IActionResult> GetGenresAsync()
        {
            NgsaLog myLogger = Logger.GetLogger(nameof(GetGenresAsync), HttpContext).EnrichLog();

            IActionResult res = await ResultHandler.Handle(App.CosmosDal.GetGenresAsync(), myLogger).ConfigureAwait(false);

            // use cache dal on Cosmos 429 errors
            if (res is JsonResult jres && jres.StatusCode == 429)
            {
                res = await ResultHandler.Handle(App.CacheDal.GetGenresAsync(), myLogger).ConfigureAwait(false);
            }

            return res;

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
