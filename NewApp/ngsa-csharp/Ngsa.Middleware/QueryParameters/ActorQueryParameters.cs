// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Collections.Generic;
using Ngsa.Middleware.Validation;

namespace Ngsa.Middleware
{
    /// <summary>
    /// Query sting parameters for Actors controller
    /// </summary>
    public sealed class ActorQueryParameters
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 100;
        public string Q { get; set; }

        public static List<ValidationError> ValidateActorId(string actorId)
        {
            List<ValidationError> errors = new List<ValidationError>();

            if (!string.IsNullOrWhiteSpace(actorId) &&
                    (actorId != actorId.ToLowerInvariant().Trim() ||
                    actorId.Length < 7 ||
                    actorId.Length > 11 ||
                    !actorId.StartsWith("nm") ||
                    !int.TryParse(actorId[2..], out int v) ||
                    v <= 0))
            {
                errors.Add(new ValidationError { Target = "actorId", Message = ValidationError.GetErrorMessage("actorId") });
            }

            return errors;
        }

        public int GetOffset()
        {
            return PageSize * (PageNumber > 1 ? PageNumber - 1 : 0);
        }

        public List<ValidationError> Validate()
        {
            List<ValidationError> errors = new List<ValidationError>();

            if (!string.IsNullOrEmpty(Q) &&
                (Q.Length < 2 || Q.Length > 20))
            {
                errors.Add(new ValidationError { Target = "q", Message = ValidationError.GetErrorMessage("Q") });
            }

            if (PageNumber < 1 || PageNumber > 10000)
            {
                errors.Add(new ValidationError { Target = "pageNumber", Message = ValidationError.GetErrorMessage("PageNumber") });
        }

            if (PageSize < 1 || PageSize > 1000)
            {
                errors.Add(new ValidationError { Target = "pageSize", Message = ValidationError.GetErrorMessage("PageSize") });
            }

            return errors;
        }

        public string GetKey()
        {
            return $"/api/actors/{PageNumber}/{PageNumber}/{(string.IsNullOrWhiteSpace(Q) ? string.Empty : Q.ToUpperInvariant().Trim())}";
        }
    }
}
