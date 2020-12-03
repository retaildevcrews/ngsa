// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CSE.NextGenSymmetricApp.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CSE.NextGenSymmetricApp.Controllers
{
    /// <summary>
    /// Handle all of the /api/movies requests
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class MoviesController : Controller
    {
        private readonly ILogger logger;

        /// <summary>
        ///  Constructor
        /// </summary>
        /// <param name="logger">log instance</param>
        /// <param name="dal">data access layer instance</param>
        public MoviesController(ILogger<MoviesController> logger)
        {
            this.logger = logger;
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

            return await DataService.Read<List<Movie>>(Request).ConfigureAwait(false);

            //return await ResultHandler.Handle(
            //    dal.GetMoviesAsync(movieQueryParameters), movieQueryParameters.GetMethodText(HttpContext), Constants.MoviesControllerException, logger)
            //    .ConfigureAwait(false);
        }

        /// <summary>
        /// Returns a single JSON Movie by movieIdParameter
        /// </summary>
        /// <param name="movieIdParameter">Movie ID</param>
        /// <returns>IActionResult</returns>
        [HttpGet("{movieId}")]
        public async Task<IActionResult> GetMovieByIdAsync([FromRoute] MovieIdParameter movieIdParameter)
        {
            if (movieIdParameter == null)
            {
                throw new ArgumentNullException(nameof(movieIdParameter));
            }

            string method = nameof(GetMovieByIdAsync) + movieIdParameter.MovieId;

            return await DataService.Read<Movie>(Request).ConfigureAwait(false);

            //return await ResultHandler.Handle(
            //    dal.GetMovieAsync(movieIdParameter.MovieId), method, "Movie Not Found", logger)
            //    .ConfigureAwait(false);
        }
    }
}
