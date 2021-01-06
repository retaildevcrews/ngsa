// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Imdb.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Ngsa.Middleware;

namespace Ngsa.App.Controllers
{
    /// <summary>
    /// Handle all of the /api/actors requests
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ActorsController : Controller
    {
        private static readonly NgsaLog Logger = new NgsaLog
        {
            Name = typeof(ActorsController).FullName,
            LogLevel = App.AppLogLevel,
            ErrorMessage = "ActorControllerException",
            NotFoundError = "Actor Not Found",
        };

        /// <summary>
        /// Returns a JSON array of Actor objects based on query parameters
        /// </summary>
        /// <param name="actorQueryParameters">query parameters</param>
        /// <returns>IActionResult</returns>
        [HttpGet]
        public async Task<IActionResult> GetActorsAsync([FromQuery] ActorQueryParameters actorQueryParameters)
        {
            NgsaLog myLogger = Logger.GetLogger(nameof(GetActorsAsync), HttpContext);

            myLogger.LogInformation("Web Request");

            if (actorQueryParameters == null)
            {
                throw new ArgumentNullException(nameof(actorQueryParameters));
            }

            return await DataService.Read<List<Actor>>(Request).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns a single JSON Actor by actorId
        /// </summary>
        /// <param name="actorIdParameter">The actorId</param>
        /// <response code="404">actorId not found</response>
        /// <returns>IActionResult</returns>
        [HttpGet("{actorId}")]
        public async Task<IActionResult> GetActorByIdAsync([FromRoute] ActorIdParameter actorIdParameter)
        {
            NgsaLog myLogger = Logger.GetLogger(nameof(GetActorByIdAsync), HttpContext);

            myLogger.LogInformation("Web Request");

            if (actorIdParameter == null)
            {
                throw new ArgumentNullException(nameof(actorIdParameter));
            }

            // return result
            return await DataService.Read<Actor>(Request).ConfigureAwait(false);
        }
    }
}
