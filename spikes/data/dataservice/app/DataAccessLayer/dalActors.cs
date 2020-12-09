// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using CSE.NextGenSymmetricApp.Model;
using Microsoft.Azure.Cosmos;

namespace CSE.NextGenSymmetricApp.DataAccessLayer
{
    /// <summary>
    /// Data Access Layer for CosmosDB
    /// </summary>
    public partial class CosmosDal
    {
        // select template for Actors
        private const string ActorSelect = "select m.id, m.partitionKey, m.actorId, m.type, m.name, m.birthYear, m.deathYear, m.profession, m.textSearch, m.movies from m where m.type = 'Actor' ";
        private const string ActorOrderBy = " order by m.textSearch ASC, m.actorId ASC";
        private const string ActorOffset = " offset {0} limit {1}";

        /// <summary>
        /// Retrieve a single Actor from CosmosDB by actorId
        ///
        /// Uses the CosmosDB single document read API which is 1 RU if less than 1K doc size
        ///
        /// Throws an exception if not found
        /// </summary>
        /// <param name="actorId">Actor ID</param>
        /// <returns>Actor object</returns>
        public async Task<Actor> GetActorAsync(string actorId)
        {
            if (string.IsNullOrWhiteSpace(actorId))
            {
                throw new ArgumentNullException(nameof(actorId));
            }

            // get the partition key for the actor ID
            // note: if the key cannot be determined from the ID, ReadDocumentAsync cannot be used.
            // ComputePartitionKey will throw an ArgumentException if the actorId isn't valid
            // get an actor by ID

            string key = $"/api/actors/{actorId.ToUpperInvariant().Trim()}";

            if (App.UseCache && cache.Contains(key) && cache.Get(key) is Actor ac)
            {
                return ac;
            }

            Actor res = await cosmosDetails.Container.ReadItemAsync<Actor>(actorId, new PartitionKey(Actor.ComputePartitionKey(actorId))).ConfigureAwait(false);

            cache.Add(new System.Runtime.Caching.CacheItem(key, res), cachePolicy);

            return res;
        }

        /// <summary>
        /// Get a list of Actors by search string
        ///
        /// The search is a "contains" search on actor name
        /// If q is empty, all actors are returned
        /// </summary>
        /// <param name="actorQueryParameters">search parameters</param>
        /// <returns>List of Actors or an empty list</returns>
        public async Task<IEnumerable<Actor>> GetActorsAsync(ActorQueryParameters actorQueryParameters)
        {
            if (actorQueryParameters == null)
            {
                throw new ArgumentNullException(nameof(actorQueryParameters));
            }

            string key = actorQueryParameters.GetKey();

            if (App.UseCache && cache.Contains(key) && cache.Get(key) is List<Actor> ac)
            {
                return ac;
            }

            string sql = ActorSelect;

            int offset = actorQueryParameters.GetOffset();
            int limit = actorQueryParameters.PageSize;

            string offsetLimit = string.Format(CultureInfo.InvariantCulture, ActorOffset, offset, limit);

            if (!string.IsNullOrEmpty(actorQueryParameters.Q))
            {
                // convert to lower and escape embedded '
                actorQueryParameters.Q = actorQueryParameters.Q.Trim().ToLowerInvariant().Replace("'", "''", System.StringComparison.OrdinalIgnoreCase);

                if (!string.IsNullOrEmpty(actorQueryParameters.Q))
                {
                    // get actors by a "like" search on name
                    sql += string.Format(CultureInfo.InvariantCulture, $" and contains(m.textSearch, @q) ");
                }
            }

            sql += ActorOrderBy + offsetLimit;

            QueryDefinition queryDefinition = new QueryDefinition(sql);

            if (!string.IsNullOrEmpty(actorQueryParameters.Q))
            {
                queryDefinition.WithParameter("@q", actorQueryParameters.Q);
            }

            List<Actor> res = (List<Actor>)await InternalCosmosDBSqlQuery<Actor>(queryDefinition).ConfigureAwait(false);

            cache.Add(new System.Runtime.Caching.CacheItem(key, res), cachePolicy);

            return res;
        }
    }
}
