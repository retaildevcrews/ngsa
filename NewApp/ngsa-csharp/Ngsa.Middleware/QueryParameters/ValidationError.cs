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
        ///     default is InvalidValue per spec
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
        /// Get standard error message
        ///     changing these will require changes to the json validation tests
        /// </summary>
        /// <param name="fieldName">field name</param>
        /// <returns>string</returns>
        public static string GetErrorMessage(string fieldName)
        {
            return fieldName.ToUpperInvariant() switch
            {
                "ACTORID" => "The parameter 'actorId' should start with 'nm' and be between 7 and 11 characters in total.",
                "GENRE" => "The parameter 'genre' should be between 3 and 20 characters.",
                "MOVIEID" => "The parameter 'movieId' should start with 'tt' and be between 7 and 11 characters in total.",
                "PAGENUMBER" => "The parameter 'pageNumber' should be between 1 and 10000.",
                "PAGESIZE" => "The parameter 'pageSize' should be between 1 and 1000.",
                "Q" => "The parameter 'q' should be between 2 and 20 characters.",
                "RATING" => "The parameter 'rating' should be between 0.0 and 10.0.",
                "YEAR" => $"The parameter 'year' should be between 1874 and 2025.",
                _ => $"Unknown parameter: {fieldName}",
            };
        }

        /// <summary>
        /// Get the doc link based on request URL
        /// </summary>
        /// <param name="path">full request path</param>
        /// <returns>link to doc</returns>
        public static string GetErrorLink(string path)
        {
            string s = "https://github.com/retaildevcrews/ngsa/blob/main/docs/ParameterValidation.md";

            if (path.StartsWith("/api/movies?") || path.StartsWith("/api/movies/?"))
            {
                s += "#movies-api";
            }
            else if (path.StartsWith("/api/movies"))
            {
                s += "#movies-direct-read";
            }
            else if (path.StartsWith("/api/actors?") || path.StartsWith("/api/actors/?"))
            {
                s += "#actors-api";
            }
            else if (path.StartsWith("/api/actors"))
            {
                s += "#actors-direct-read";
            }

            return s;
        }
    }
}
