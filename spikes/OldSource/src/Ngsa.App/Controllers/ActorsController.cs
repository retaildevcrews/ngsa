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
    /// Handle all of the /api/actors requests
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ActorsController : Controller
    {
        private static readonly NgsaLog Logger = new NgsaLog
        {
            Name = typeof(ActorsController).FullName,
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
            Logger.LogInformation(nameof(GetActorsAsync), "Web Request", HttpContext);

            if (actorQueryParameters == null)
            {
                throw new ArgumentNullException(nameof(actorQueryParameters));
            }

            List<Middleware.Validation.ValidationError> list = actorQueryParameters.Validate();

            if (list.Count > 0)
            {
                Logger.LogWarning(new EventId((int)HttpStatusCode.BadRequest, HttpStatusCode.BadRequest.ToString()), nameof(GetActorsAsync), "Invalid query string", HttpContext);

                return ResultHandler.CreateResult(list, Request.Path.ToString() + (Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty));
            }

            return await DataService.Read<List<Actor>>(Request).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns a single JSON Actor by actorId
        /// </summary>
        /// <param name="actorId">The actorId</param>
        /// <response code="404">actorId not found</response>
        /// <returns>IActionResult</returns>
        [HttpGet("{actorId}")]
        public async Task<IActionResult> GetActorByIdAsync([FromRoute] string actorId)
        {
            Logger.LogInformation(nameof(GetActorByIdAsync), "Web Request", HttpContext);

            if (string.IsNullOrWhiteSpace(actorId))
            {
                throw new ArgumentNullException(nameof(actorId));
            }

            List<Middleware.Validation.ValidationError> list = ActorQueryParameters.ValidateActorId(actorId);

            if (list.Count > 0)
            {
                Logger.LogWarning(new EventId((int)HttpStatusCode.BadRequest, HttpStatusCode.BadRequest.ToString()), nameof(GetActorByIdAsync), "Invalid Actor Id", HttpContext);

                return ResultHandler.CreateResult(list, Request.Path.ToString() + (Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty));
            }

            // return result
            return await DataService.Read<Actor>(Request).ConfigureAwait(false);
        }
    }
}
