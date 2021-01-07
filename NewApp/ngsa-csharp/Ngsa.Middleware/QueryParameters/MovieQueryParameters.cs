// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using Ngsa.Middleware.Validation;

namespace Ngsa.Middleware
{
    /// <summary>
    /// Query sting parameters for Movies controller
    /// </summary>
    public sealed class MovieQueryParameters
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 100;
        public string Q { get; set; }
        public string ActorId { get; set; }
        public string Genre { get; set; }
        public int Year { get; set; }
        public double Rating { get; set; }

        public static List<ValidationError> ValidateMovieId(string movieId)
        {
            List<ValidationError> errors = new List<ValidationError>();

            if (!string.IsNullOrWhiteSpace(movieId) &&
                    (movieId != movieId.ToLowerInvariant().Trim() ||
                    movieId.Length < 7 ||
                    movieId.Length > 11 ||
                    !movieId.StartsWith("tt") ||
                    !int.TryParse(movieId[2..], out int v) ||
                    v <= 0))
            {
                errors.Add(new ValidationError { Target = "movieId", Message = ValidationError.GetErrorMessage("movieId") });
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

            if (!string.IsNullOrEmpty(ActorId) && ActorQueryParameters.ValidateActorId(ActorId).Count > 0)
            {
                errors.Add(new ValidationError { Target = "actorId", Message = ValidationError.GetErrorMessage("ActorId") });
            }

            if (!string.IsNullOrEmpty(Genre) &&
                (Genre != Genre.Trim() ||
                 Genre.Length < 3 ||
                 Genre.Length > 20))
            {
                errors.Add(new ValidationError { Target = "genre", Message = ValidationError.GetErrorMessage("Genre") });
            }

            if (!(Rating >= 0 && Rating <= 10))
            {
                errors.Add(new ValidationError { Target = "rating", Message = ValidationError.GetErrorMessage("Rating") });
            }

            if (Year != 0 && !(Year >= 1874 && Year <= DateTime.UtcNow.Year + 5))
            {
                errors.Add(new ValidationError { Target = "year", Message = ValidationError.GetErrorMessage("Year") });
            }

            return errors;
        }

        public string GetKey()
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
