// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Imdb.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Ngsa.Middleware;

namespace Ngsa.App.Controllers
{
    /// <summary>
    /// Handle all of the /api/movies requests
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class MoviesController : Controller
    {
        private static readonly NgsaLog Logger = new NgsaLog
        {
            Name = typeof(MoviesController).FullName,
            LogLevel = App.AppLogLevel,
            ErrorMessage = "MoviesControllerException",
            NotFoundError = "Movie Not Found",
        };

        /// <summary>
        /// Returns a JSON array of Movie objects
        /// </summary>
        /// <param name="movieQueryParameters">query parameters</param>
        /// <returns>IActionResult</returns>
        [HttpGet]
        public async Task<IActionResult> GetMoviesAsync([FromQuery] MovieQueryParameters movieQueryParameters)
        {
            NgsaLog nLogger = Logger.GetLogger(nameof(GetMoviesAsync), HttpContext);
            nLogger.LogInformation("Web Request");

            if (movieQueryParameters == null)
            {
                throw new ArgumentNullException(nameof(movieQueryParameters));
            }

            List<Middleware.Validation.ValidationError> list = movieQueryParameters.Validate();

            if (list.Count > 0)
            {
                nLogger.Data.Clear();
                nLogger.EventId = new EventId((int)HttpStatusCode.BadRequest, HttpStatusCode.BadRequest.ToString());
                nLogger.LogWarning($"Invalid query string");

                return ResultHandler.CreateResult(list, Request.Path.ToString() + (Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty));
            }

            return await DataService.Read<List<Movie>>(Request).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns a single JSON Movie by movieIdParameter
        /// </summary>
        /// <param name="movieId">Movie ID</param>
        /// <returns>IActionResult</returns>
        [HttpGet("{movieId}")]
        public async Task<IActionResult> GetMovieByIdAsync([FromRoute] string movieId)
        {
            NgsaLog nLogger = Logger.GetLogger(nameof(GetMovieByIdAsync), HttpContext);
            nLogger.LogInformation("Web Request");

            if (string.IsNullOrWhiteSpace(movieId))
            {
                throw new ArgumentNullException(nameof(movieId));
            }

            List<Middleware.Validation.ValidationError> list = MovieQueryParameters.ValidateMovieId(movieId);

            if (list.Count > 0)
            {
                nLogger.Data.Clear();
                nLogger.EventId = new EventId((int)HttpStatusCode.BadRequest, HttpStatusCode.BadRequest.ToString());
                nLogger.LogWarning($"Invalid Movie Id");

                return ResultHandler.CreateResult(list, Request.Path.ToString() + (Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty));
            }

            return await DataService.Read<Movie>(Request).ConfigureAwait(false);
        }
    }
}
