// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Ngsa.DataService.DataAccessLayer;
using Ngsa.Middleware;

namespace Ngsa.DataService.Controllers
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
            ErrorMessage = "MovieControllerException",
            NotFoundError = "Movie Not Found",
        };

        private readonly IDAL dal;

        /// <summary>
        /// Initializes a new instance of the <see cref="MoviesController"/> class.
        /// </summary>
        public MoviesController()
        {
            dal = App.CosmosDal;
        }

        /// <summary>
        /// Returns a JSON array of Movie objects
        /// </summary>
        /// <param name="movieQueryParameters">query parameters</param>
        /// <returns>IActionResult</returns>
        [HttpGet]
        public async Task<IActionResult> GetMoviesAsync([FromQuery] MovieQueryParameters movieQueryParameters)
        {
            if (movieQueryParameters == null)
            {
                throw new ArgumentNullException(nameof(movieQueryParameters));
            }

            NgsaLog myLogger = Logger.GetLogger(nameof(GetMoviesAsync), HttpContext).EnrichLog();

            var list = movieQueryParameters.Validate();

            if (list.Count > 0)
            {
                myLogger.Data.Clear();
                myLogger.EventId = new EventId((int)HttpStatusCode.BadRequest, HttpStatusCode.BadRequest.ToString());
                myLogger.LogWarning($"Invalid query string");

                return ResultHandler.CreateResult(list, Request.Path.ToString() + (Request.QueryString.HasValue ? Request.QueryString.ToString() : string.Empty));
            }

            // get the result
            IActionResult res = await ResultHandler.Handle(dal.GetMoviesAsync(movieQueryParameters), myLogger).ConfigureAwait(false);

            // use cache dal on Cosmos 429 errors
            if (res is JsonResult jres && jres.StatusCode == 429)
            {
                res = await ResultHandler.Handle(App.CacheDal.GetMoviesAsync(movieQueryParameters), myLogger).ConfigureAwait(false);
            }

            return res;
        }

        /// <summary>
        /// Returns a single JSON Movie by movieIdParameter
        /// </summary>
        /// <param name="movieId">Movie ID</param>
        /// <returns>IActionResult</returns>
        [HttpGet("{movieId}")]
        public async Task<IActionResult> GetMovieByIdAsync([FromRoute] string movieId)
        {
            if (string.IsNullOrWhiteSpace(movieId))
            {
                throw new ArgumentNullException(nameof(movieId));
            }

            NgsaLog myLogger = Logger.GetLogger(nameof(GetMovieByIdAsync), HttpContext).EnrichLog();

            var list = MovieQueryParameters.ValidateMovieId(movieId);

            if (list.Count > 0)
            {
                myLogger.Data.Clear();
                myLogger.EventId = new EventId((int)HttpStatusCode.BadRequest, HttpStatusCode.BadRequest.ToString());
                myLogger.LogWarning($"Invalid Movie Id");

                return ResultHandler.CreateResult(list, Request.Path.ToString() + (Request.QueryString.HasValue ? Request.QueryString.ToString() : string.Empty));
            }

            IActionResult res = await ResultHandler.Handle(dal.GetMovieAsync(movieId), myLogger).ConfigureAwait(false);

            // use cache dal on Cosmos 429 errors
            if (res is JsonResult jres && jres.StatusCode == 429)
            {
                res = await ResultHandler.Handle(App.CacheDal.GetMovieAsync(movieId), myLogger).ConfigureAwait(false);
            }

            return res;
        }
    }
}
