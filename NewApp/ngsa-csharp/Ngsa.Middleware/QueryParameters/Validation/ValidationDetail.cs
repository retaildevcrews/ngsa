// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;

namespace Ngsa.Middleware.Validation
{
    /// <summary>
    /// Model class for Validation Problem Details
    /// </summary>
    public class ValidationDetail
    {
        private readonly List<ValidationError> validationErrors = new List<ValidationError>();

        public string Type { get; set; }

        public string Title { get; set; }

        public string Detail { get; set; }

        public int Status { get; set; }

        public string Instance { get; set; }

        public ICollection<ValidationError> ValidationErrors => validationErrors;
    }
}
