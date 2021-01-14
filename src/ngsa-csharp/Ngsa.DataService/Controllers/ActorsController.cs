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

            NgsaLog nLogger = Logger.GetLogger(nameof(GetActorsAsync), HttpContext);

            System.Collections.Generic.List<Middleware.Validation.ValidationError> list = actorQueryParameters.Validate();

            if (list.Count > 0)
            {
                nLogger.Data.Clear();
                nLogger.EventId = new EventId((int)HttpStatusCode.BadRequest, HttpStatusCode.BadRequest.ToString());
                nLogger.LogWarning($"Invalid query string");

                return ResultHandler.CreateResult(list, Request.Path.ToString() + (Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty));
            }

            IActionResult res = await ResultHandler.Handle(dal.GetActorsAsync(actorQueryParameters), nLogger).ConfigureAwait(false);

            // use cache dal on Cosmos 429 errors
            if (res is JsonResult jres && jres.StatusCode == 429)
            {
                res = await ResultHandler.Handle(App.CacheDal.GetActorsAsync(actorQueryParameters), nLogger).ConfigureAwait(false);
            }

            return res;
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
            if (actorId == null)
            {
                throw new ArgumentNullException(nameof(actorId));
            }

            NgsaLog nLogger = Logger.GetLogger(nameof(GetActorByIdAsync), HttpContext);

            System.Collections.Generic.List<Middleware.Validation.ValidationError> list = ActorQueryParameters.ValidateActorId(actorId);

            if (list.Count > 0)
            {
                nLogger.Data.Clear();
                nLogger.EventId = new EventId((int)HttpStatusCode.BadRequest, HttpStatusCode.BadRequest.ToString());
                nLogger.LogWarning($"Invalid Actor Id");

                return ResultHandler.CreateResult(list, Request.Path.ToString() + (Request.QueryString.HasValue ? Request.QueryString.Value : string.Empty));
            }

            // return result
            IActionResult res = await ResultHandler.Handle(dal.GetActorAsync(actorId), nLogger).ConfigureAwait(false);

            // use cache dal on Cosmos 429 errors
            if (res is JsonResult jres && jres.StatusCode == 429)
            {
                res = await ResultHandler.Handle(App.CacheDal.GetActorAsync(actorId), nLogger).ConfigureAwait(false);
            }

            return res;
        }
    }
}
