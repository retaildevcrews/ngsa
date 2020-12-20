// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Ngsa.DataService.DataAccessLayer;

namespace Ngsa.DataService.Controllers
{
    /// <summary>
    /// Handle the single /api/genres requests
    /// </summary>
    [Route("api/[controller]")]
    public class GenresController : Controller
    {
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
            string method = nameof(GetGenresAsync);

            // TODO - remove after testing
            logger.LogWarning("TestWarning {method}", method);
            logger.LogError(1, new ArgumentException("Argument Exception"), "TestError {method}", method);

            // get list of genres as list of string
            return await ResultHandler.Handle(dal.GetGenresAsync(), nameof(GetGenresAsync), Constants.GenresControllerException, logger).ConfigureAwait(false);
        }
    }
}
