// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Microsoft.AspNetCore.Http;
using Microsoft.CorrelationVector;

namespace CSE.NextGenSymmetricApp.Extensions
{
    /// <summary>
    /// Correlation Vector extensions
    /// </summary>
    public static partial class CVectorExtensions
    {
        /// <summary>
        /// Extend correlation vector
        /// </summary>
        /// <param name="context">http context</param>
        /// <returns>extended CV</returns>
        public static CorrelationVector ExtendCVector(HttpContext context)
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
