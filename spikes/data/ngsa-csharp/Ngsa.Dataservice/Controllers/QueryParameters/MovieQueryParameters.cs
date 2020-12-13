// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using Ngsa.DataService.Validation;

namespace Ngsa.DataService
{
    /// <summary>
    /// Query sting parameters for Movies controller
    /// </summary>
    public sealed class MovieQueryParameters : QueryParameters
    {
        [IdValidation(startingCharacters: "nm", minimumCharacters: 7, maximumCharacters: 11, true)]
        public string ActorId { get; set; }

        [StringLength(maximumLength: 20, MinimumLength = 3)]
        public string Genre { get; set; }

        [YearValidation]
        public int Year { get; set; }

        [Range(minimum: 0.0, maximum: 10.0)]
        public double Rating { get; set; }

        public new string GetKey()
        {
            string key = "/api/movies";
            key += $"/{PageNumber}/{PageSize}/{Year}/{Rating}";
            key += $"/{(string.IsNullOrWhiteSpace(Q) ? string.Empty : Q.Trim().ToUpperInvariant())}";
            key += $"/{(string.IsNullOrWhiteSpace(Genre) ? string.Empty : Genre.Trim().ToUpperInvariant())}";
            key += $"/{(string.IsNullOrWhiteSpace(ActorId) ? string.Empty : ActorId.Trim().ToUpperInvariant())}";

            return key;
        }
    }
}
