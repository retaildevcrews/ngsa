// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;

namespace Ngsa.Middleware.Validation
{
    /// <summary>
    /// Paramameter validation for Year
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class YearValidation : ValidationAttribute
    {
        private const int StartYear = 1874;
        private static readonly int EndYear = DateTime.UtcNow.AddYears(5).Year;

        protected override System.ComponentModel.DataAnnotations.ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (validationContext == null)
            {
                throw new ArgumentNullException(nameof(validationContext));
            }

            bool isValid = ((int)value >= StartYear && (int)value <= EndYear) || (int)value == 0;

            string errorMessage = $"The parameter '{validationContext.MemberName}' should be between {StartYear} and {EndYear}.";

            return !isValid ? new System.ComponentModel.DataAnnotations.ValidationResult(errorMessage) : System.ComponentModel.DataAnnotations.ValidationResult.Success;
        }
    }
}
