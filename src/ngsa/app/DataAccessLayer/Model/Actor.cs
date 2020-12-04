﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace CSE.NextGenSymmetricApp.Model
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "performance")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1036:Override methods on comparable types", Justification = "todo")]
    public class Actor : IComparable
    {
        public string Id { get; set; }
        public string ActorId { get; set; }
        public string PartitionKey { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public int? BirthYear { get; set; }
        public int? DeathYear { get; set; }
        public string TextSearch { get; set; }
        public List<string> Profession { get; set; }
        public List<ActorMovie> Movies { get; set; }

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
            if (!string.IsNullOrEmpty(id) &&
                id.Length > 5 &&
                id.StartsWith("nm", StringComparison.OrdinalIgnoreCase) &&
                int.TryParse(id.Substring(2), out int idInt))
            {
                return (idInt % 10).ToString(CultureInfo.InvariantCulture);
            }

            throw new ArgumentException("Invalid Partition Key");
        }

        public int CompareTo(object obj)
        {
            if (obj is Actor y)
            {
                return string.Compare(Name, y.Name, StringComparison.OrdinalIgnoreCase);
            }

            return 1;
        }
    }
}
