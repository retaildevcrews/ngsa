// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Ngsa.DataService.DataAccessLayer;
using Ngsa.Middleware;

namespace Ngsa.DataService.Controllers
{
    /// <summary>
    /// Handle all of the /api/actors requests
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ActorsController : Controller
    {
        private readonly ILogger logger;
        private readonly IDAL dal;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorsController"/> class.
        /// </summary>
        /// <param name="logger">log instance</param>
        public ActorsController(ILogger<ActorsController> logger)
        {
            // save to local for use in handlers
            this.logger = logger;
            dal = App.CosmosDal;
        }

        /// <summary>
        /// Returns a JSON array of Actor objects based on query parameters
        /// </summary>
        /// <param name="actorQueryParameters">query parameters</param>
        /// <returns>IActionResult</returns>
        [HttpGet]
        public async Task<IActionResult> GetActorsAsync([FromQuery] ActorQueryParameters actorQueryParameters)
        {
            if (actorQueryParameters == null)
            {
                throw new ArgumentNullException(nameof(actorQueryParameters));
            }

            return await ResultHandler.Handle(
                    dal.GetActorsAsync(actorQueryParameters), actorQueryParameters.GetMethodText(HttpContext), Constants.ActorsControllerException, logger)
                .ConfigureAwait(false);
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
            if (actorIdParameter == null)
            {
                throw new ArgumentNullException(nameof(actorIdParameter));
            }

            string method = nameof(GetActorByIdAsync) + actorIdParameter.ActorId;

            // return result
            return await ResultHandler.Handle(
                dal.GetActorAsync(actorIdParameter.ActorId), method, "Actor Not Found", logger)
                .ConfigureAwait(false);
        }
    }
}
