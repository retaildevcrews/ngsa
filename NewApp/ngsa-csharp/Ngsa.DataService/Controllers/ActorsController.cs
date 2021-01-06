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
        private static readonly NgsaLog Logger = new NgsaLog
        {
            Name = typeof(ActorsController).FullName,
            LogLevel = App.AppLogLevel,
            ErrorMessage = "ActorControllerException",
            NotFoundError = "Actor Not Found",
        };

        private readonly IDAL dal;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorsController"/> class.
        /// </summary>
        public ActorsController()
        {
            // save to local for use in handlers
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

            NgsaLog myLogger = Logger.GetLogger(nameof(GetActorsAsync), HttpContext);

            return await ResultHandler.Handle(dal.GetActorsAsync(actorQueryParameters), myLogger).ConfigureAwait(false);
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

            NgsaLog myLogger = Logger.GetLogger(nameof(GetActorByIdAsync), HttpContext);

            // return result
            return await ResultHandler.Handle(dal.GetActorAsync(actorIdParameter.ActorId), myLogger).ConfigureAwait(false);
        }
    }
}
