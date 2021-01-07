// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;

namespace Ngsa.Middleware.Validation
{
    /// <summary>
    /// Validation Error Class
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// Gets or sets error Code
        /// </summary>
        public string Code { get; set; } = "InvalidValue";

        /// <summary>
        /// Gets or sets error Target
        /// </summary>
        public string Target { get; set; }

        /// <summary>
        /// Gets or sets error Message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Create standard error messages
        /// </summary>
        /// <param name="key">validation error key</param>
        /// <returns>string</returns>
        public static string GetErrorMessage(string key)
        {
            return key.ToUpperInvariant() switch
            {
                "ACTORID" => "The parameter 'actorId' should start with 'nm' and be between 7 and 11 characters in total.",
                "GENRE" => "The parameter 'genre' should be between 3 and 20 characters.",
                "MOVIEID" => "The parameter 'movieId' should start with 'tt' and be between 7 and 11 characters in total.",
                "PAGENUMBER" => "The parameter 'pageNumber' should be between 1 and 10000.",
                "PAGESIZE" => "The parameter 'pageSize' should be between 1 and 1000.",
                "Q" => "The parameter 'q' should be between 2 and 20 characters.",
                "RATING" => "The parameter 'rating' should be between 0.0 and 10.0.",
                "YEAR" => $"The parameter 'year' should be between 1874 and {DateTime.UtcNow.Year + 5}.",
                _ => $"Unknown parameter: {key}",
            };
        }
    }
}
