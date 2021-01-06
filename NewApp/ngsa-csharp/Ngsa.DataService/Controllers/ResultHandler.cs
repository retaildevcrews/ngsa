// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Ngsa.Middleware;

namespace Ngsa.DataService.Controllers
{
    /// <summary>
    /// Handles query requests from the controllers
    /// </summary>
    public static class ResultHandler
    {
        // todo - remove once logging decision is final
        public static async Task<IActionResult> HandleOld<T>(Task<T> task, string method, string errorMessage, ILogger logger)
        {
            // log the request
            logger.LogInformation(method);

            // return exception if task is null
            if (task == null)
            {
                logger.LogError($"Exception:{method} task is null");

                return CreateResult(errorMessage, HttpStatusCode.InternalServerError);
            }

            try
            {
                // return an OK object result
                return new OkObjectResult(await task.ConfigureAwait(false));
            }
            catch (CosmosException ce)
            {
                // log and return Cosmos status code
                if (ce.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    logger.LogWarning($"CosmosNotFound:{method}");
                }
                else
                {
                    logger.LogError($"{ce}\nCosmosException:{method}:{ce.StatusCode}:{ce.ActivityId}:{ce.Message}");
                }

                return CreateResult(errorMessage, ce.StatusCode);
            }
            catch (Exception ex)
            {
                // log and return exception
                logger.LogError($"{ex}\nException:{method}:{ex.Message}");

                // return 500 error
                return CreateResult("Internal Server Error", HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// Handle an IActionResult request from a controller
        /// </summary>
        /// <typeparam name="T">type of result</typeparam>
        /// <param name="task">async task (usually the Cosmos query)</param>
        /// <param name="logger">NgsaLog</param>
        /// <returns>IActionResult</returns>
        public static async Task<IActionResult> Handle<T>(Task<T> task, NgsaLog logger)
        {
            // log the request
            logger.LogInformation("Web request");

            // return exception if task is null
            if (task == null)
            {
                logger.EventId = new EventId((int)HttpStatusCode.InternalServerError, "Exception");
                logger.Exception = new ArgumentNullException(nameof(task));
                logger.LogError("Exception: task is null");

                return CreateResult(logger.ErrorMessage, HttpStatusCode.InternalServerError);
            }

            try
            {
                // return an OK object result
                return new OkObjectResult(await task.ConfigureAwait(false));
            }
            catch (CosmosException ce)
            {
                // log and return Cosmos status code
                if (ce.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    logger.EventId = new EventId((int)ce.StatusCode, string.Empty);
                    logger.LogWarning($"CosmosNotFound: {ce.StatusCode}");
                    return CreateResult(logger.NotFoundError, ce.StatusCode);
                }

                logger.Exception = ce;
                logger.EventId = new EventId((int)ce.StatusCode, "CosmosException");
                logger.Data.Add("cosmosActivityId", ce.ActivityId);
                logger.LogError($"CosmosException: {ce.Message}");

                return CreateResult(logger.ErrorMessage, ce.StatusCode);
            }
            catch (Exception ex)
            {
                // log and return exception
                logger.Exception = ex;
                logger.EventId = new EventId((int)HttpStatusCode.InternalServerError, "Exception");
                logger.LogError($"Exception: {ex.Message}");

                // return 500 error
                return CreateResult("Internal Server Error", HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// ContentResult factory
        /// </summary>
        /// <param name="message">string</param>
        /// <param name="statusCode">int</param>
        /// <returns>JsonResult</returns>
        public static JsonResult CreateResult(string message, HttpStatusCode statusCode)
        {
            return new JsonResult(new ErrorResult { Error = statusCode, Message = message })
            {
                StatusCode = (int)statusCode,
            };
        }
    }
}
