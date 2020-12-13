// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Ngsa.DataService.Validation;

namespace Ngsa.DataService
{
    /// <summary>
    /// Actor ID URI validation class
    /// </summary>
    public sealed class ActorIdParameter
    {
        /// <summary>
        /// gets or sets a valid actor ID
        /// </summary>
        [IdValidation(startingCharacters: "nm", minimumCharacters: 7, maximumCharacters: 11, false)]
        public string ActorId { get; set; }
    }
}
