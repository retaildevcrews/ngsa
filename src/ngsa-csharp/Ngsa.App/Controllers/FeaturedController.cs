// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading.Tasks;
using Imdb.Model;
using Microsoft.AspNetCore.Mvc;
using Ngsa.Middleware;

namespace Ngsa.App.Controllers
{
    /// <summary>
    /// Handle /api/featured/movie requests
    /// </summary>
    [Route("api/[controller]")]
    public class FeaturedController : Controller
    {
        private static readonly NgsaLog Logger = new NgsaLog
        {
            Name = typeof(FeaturedController).FullName,
            LogLevel = App.AppLogLevel,
            ErrorMessage = "FeaturedControllerException",
            NotFoundError = "Movie Not Found",
            Method = nameof(GetFeaturedMovieAsync),
        };

        /// <summary>
        /// Returns a random movie from the featured movie list as a JSON Movie
        /// </summary>
        /// <response code="200">OK</response>
        /// <returns>IActionResult</returns>
        [HttpGet("movie")]
        public async Task<IActionResult> GetFeaturedMovieAsync()
        {
            NgsaLog nLogger = Logger.GetLogger(nameof(GetFeaturedMovieAsync), HttpContext);

            nLogger.LogInformation("Web Request");

            return await DataService.Read<Movie>(Request).ConfigureAwait(false);
        }
    }
}
