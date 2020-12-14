// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.CorrelationVector;

namespace Ngsa.Middleware
{
    /// <summary>
    /// Correlation Vector extensions
    /// </summary>
    public static class CorrelationVectorExtensions
    {
        /// <summary>
        /// Get the Correlation Vector base
        /// </summary>
        /// <param name="correlationVector">Correlation Vector</param>
        /// <returns>string</returns>
        public static string GetBase(this CorrelationVector correlationVector)
        {
            if (correlationVector == null)
            {
                throw new ArgumentNullException(nameof(correlationVector));
            }

            return correlationVector.Version switch
            {
                CorrelationVectorVersion.V1 => correlationVector.Value.Substring(0, 16),
                _ => correlationVector.Value.Substring(0, 22),
            };
        }

        /// <summary>
        /// Extend correlation vector
        /// </summary>
        /// <param name="context">http context</param>
        /// <returns>extended CV</returns>
        public static CorrelationVector Extend(HttpContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            CorrelationVector cv;

            // get the cv from the header
            if (context.Request.Headers.ContainsKey(CorrelationVector.HeaderName))
            {
                try
                {
                    // extend the correlation vector
                    cv = CorrelationVector.Extend(context.Request.Headers[CorrelationVector.HeaderName].ToString());
                }
                catch
                {
                    // create a new correlation vector
                    cv = new CorrelationVector(CorrelationVectorVersion.V2);
                }
            }
            else
            {
                // create a new correlation vector
                cv = new CorrelationVector(CorrelationVectorVersion.V2);
            }

            return cv;
        }
    }
}
