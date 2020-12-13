// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Ngsa.DataService.Validation;

namespace Ngsa.DataService
{
    /// <summary>
    /// Movie ID URI validation class
    /// </summary>
    public sealed class MovieIdParameter
    {
        /// <summary>
        /// gets or sets a valid movie ID
        /// </summary>
        [IdValidation(startingCharacters: "tt", minimumCharacters: 7, maximumCharacters: 11, false)]
        public string MovieId { get; set; }
    }
}
