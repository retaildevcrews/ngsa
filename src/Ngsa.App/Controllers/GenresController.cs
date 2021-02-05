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
            ErrorMessage = "GenresControllerException",
            NotFoundError = "Genre Not Found",
        };

        /// <summary>
        /// Returns a JSON string array of Genre
        /// </summary>
        /// <response code="200">JSON array of strings or empty array if not found</response>
        /// <returns>IActionResult</returns>
        [HttpGet]
        public async Task<IActionResult> GetGenresAsync()
        {
            Logger.LogInformation(nameof(GetGenresAsync), "Web Request", HttpContext);

            return await DataService.Read<List<string>>(Request).ConfigureAwait(false);
        }
    }
}
