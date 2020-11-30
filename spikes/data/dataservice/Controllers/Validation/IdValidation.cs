﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;

namespace CSE.NextGenSymmetricApp.Validation
{
    /// <summary>
    /// Paramameter validation for Movie ID and Actor ID
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class IdValidation : ValidationAttribute
    {
        private readonly int minimumCharacters;
        private readonly int maximumCharacters;
        private readonly bool allowNulls;
        private readonly string startingCharacters;

        public IdValidation(string startingCharacters, int minimumCharacters, int maximumCharacters, bool allowNulls)
        {
            this.startingCharacters = startingCharacters;
            this.minimumCharacters = minimumCharacters;
            this.maximumCharacters = maximumCharacters;
            this.allowNulls = allowNulls;
        }

        protected override System.ComponentModel.DataAnnotations.ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (validationContext == null || (allowNulls && value == null))
            {
                return System.ComponentModel.DataAnnotations.ValidationResult.Success;
            }

            string errorMessage = $"The parameter '{validationContext.MemberName}' should start with '{startingCharacters}' and be between {minimumCharacters} and {maximumCharacters} characters in total";

            if (!allowNulls && value == null)
            {
                return new System.ComponentModel.DataAnnotations.ValidationResult(errorMessage);
            }

            // cast value to string
            string id = (string)value;

            // check id has correct starting characters and is between min/max values specified
            bool isInvalid = id == null ||
                          id.Length < minimumCharacters ||
                          id.Length > maximumCharacters ||
                          id.Substring(0, 2) != startingCharacters ||
                          !int.TryParse(id.Substring(2), out int val) ||
                          val <= 0;

            return isInvalid ? new System.ComponentModel.DataAnnotations.ValidationResult(errorMessage) : System.ComponentModel.DataAnnotations.ValidationResult.Success;
        }
    }
}
