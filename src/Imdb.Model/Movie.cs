﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Imdb.Model
{
    public class Movie
    {
        public string Id { get; set; }
        public string PartitionKey { get; set; }
        public string MovieId { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public int Year { get; set; }
        public int Runtime { get; set; }
        public double Rating { get; set; }
        public long Votes { get; set; }
        public long TotalScore { get; set; }
        public string TextSearch { get; set; }

        public List<string> Genres { get; set; }
        public List<Role> Roles { get; set; }

        /// <summary>
        /// Compute the partition key based on the movieId or actorId
        ///
        /// For this sample, the partitionkey is the id mod 10
        ///
        /// In a full implementation, you would update the logic to determine the partition key
        /// </summary>
        /// <param name="id">document id</param>
        /// <returns>the partition key</returns>
        public static string ComputePartitionKey(string id)
        {
            // validate id
            if (!string.IsNullOrWhiteSpace(id) &&
                id.Length > 5 &&
                id.StartsWith("tt", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(id.Substring(2), out int idInt))
            {
                return (idInt % 10).ToString(CultureInfo.InvariantCulture);
            }

            throw new ArgumentException("Invalid Partition Key");
        }

        public static int TitleCompare(Movie x, Movie y)
        {
            int result;

            result = string.Compare(x?.Title, y?.Title, StringComparison.OrdinalIgnoreCase);

            if (result == 0)
            {
                return string.Compare(y.Id, y.Id, StringComparison.OrdinalIgnoreCase);
            }

            return result;
        }
    }
}
