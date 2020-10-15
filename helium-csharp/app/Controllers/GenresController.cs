﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Threading.Tasks;
using CSE.Helium.DataAccessLayer;
using CSE.KeyRotation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace CSE.Helium.Controllers
{
    /// <summary>
    /// Handle the single /api/genres requests
    /// </summary>
    [Route("api/[controller]")]
    public class GenresController : Controller
    {
        private readonly ILogger logger;
        private readonly IDAL dal;
        private readonly IKeyRotation keyRotation;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="logger">log instance</param>
        /// <param name="dal">data access layer instance</param>
        /// <param name="keyRotation">KeyRotationHelper instance</param>
        public GenresController(ILogger<GenresController> logger, IDAL dal, IKeyRotation keyRotation)
        {
            this.logger = logger;
            this.dal = dal;
            this.keyRotation = keyRotation;
        }

        /// <summary>
        /// Returns a JSON string array of Genre
        /// </summary>
        /// <response code="200">JSON array of strings or empty array if not found</response>
        /// <returns>IActionResult</returns>
        [HttpGet]
        public async Task<IActionResult> GetGenresAsync()
        {
            // get list of genres as list of string
            return await ResultHandler.Handle(keyRotation.RetryCosmosPolicy.ExecuteAsync(() => dal.GetGenresAsync()), nameof(GetGenresAsync), Constants.GenresControllerException, logger).ConfigureAwait(false);
        }
    }
}
