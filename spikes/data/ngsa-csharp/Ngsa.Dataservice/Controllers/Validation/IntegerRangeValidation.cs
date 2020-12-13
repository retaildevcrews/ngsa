// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;

namespace Ngsa.DataService.Validation
{
    /// <summary>
    /// Paramameter validation for integer ranges
    /// </summary>
    public class IntegerRangeValidation : ValidationAttribute
    {
        private readonly int minValue;
        private readonly int maxValue;

        public IntegerRangeValidation(int minValue, int maxValue)
        {
            this.minValue = minValue;
            this.maxValue = maxValue;
        }

        protected override System.ComponentModel.DataAnnotations.ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (validationContext == null)
            {
                return System.ComponentModel.DataAnnotations.ValidationResult.Success;
            }

            string errorMessage = $"The parameter '{validationContext.MemberName}' should be between {minValue} and {maxValue}.";

            bool isValid = (int)value >= minValue && (int)value <= maxValue;

            return isValid ? System.ComponentModel.DataAnnotations.ValidationResult.Success : new System.ComponentModel.DataAnnotations.ValidationResult(errorMessage);
        }
    }
}
