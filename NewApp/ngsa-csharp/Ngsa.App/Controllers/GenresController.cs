// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Ngsa.Middleware;

namespace Ngsa.App.Controllers
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
            ErrorMessage = "GenresControllerException",
            NotFoundError = "Movie Not Found",
            Method = nameof(GetGenresAsync),
        };

        /// <summary>
        /// Returns a JSON string array of Genre
        /// </summary>
        /// <response code="200">JSON array of strings or empty array if not found</response>
        /// <returns>IActionResult</returns>
        [HttpGet]
        public async Task<IActionResult> GetGenresAsync()
        {
            NgsaLog nLogger = Logger.GetLogger(nameof(GetGenresAsync), HttpContext).EnrichLog();

            nLogger.LogInformation("Web Request");

            return await DataService.Read<List<string>>(Request).ConfigureAwait(false);
        }
    }
}
