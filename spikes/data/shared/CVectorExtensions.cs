// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Microsoft.CorrelationVector;

namespace CSE.NextGenSymmetricApp.Extensions
{
    /// <summary>
    /// Correlation Vector extensions
    /// </summary>
    public static partial class CVectorExtensions
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
    }
}
