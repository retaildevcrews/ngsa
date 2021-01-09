// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using System.Threading.Tasks;
using Imdb.Model;
using Microsoft.Azure.Cosmos;
using Ngsa.Middleware;

namespace Ngsa.DataService.DataAccessLayer
{
    /// <summary>
    /// Data Access Layer for CosmosDB
    /// </summary>
    public partial class CosmosDal
    {
        /// <summary>
        /// Retrieve a single Movie from CosmosDB by movieId
        ///
        /// Uses the CosmosDB single document read API which is 1 RU if less than 1K doc size
        ///
        /// Throws an exception if not found
        /// </summary>
        /// <param name="movieId">Movie ID</param>
        /// <returns>Movie object</returns>
        public async Task<Movie> GetMovieAsync(string movieId)
        {
            if (string.IsNullOrWhiteSpace(movieId))
            {
                throw new ArgumentNullException(nameof(movieId));
            }

            string key = $"/api/movies/{movieId.ToLowerInvariant().Trim()}";

            if (App.UseCache && cache.Contains(key) && cache.Get(key) is Movie mc)
            {
                return mc;
            }

            // get the partition key for the movie ID
            // note: if the key cannot be determined from the ID, ReadDocumentAsync cannot be used.
            // ComputePartitionKey will throw an ArgumentException if the movieId isn't valid
            // get a movie by ID

            Movie m = await cosmosDetails.Container.ReadItemAsync<Movie>(movieId, new PartitionKey(Movie.ComputePartitionKey(movieId))).ConfigureAwait(false);

            cache.Add(new CacheItem(key, m), cachePolicy);

            return m;
        }

        public async Task<IEnumerable<Movie>> GetMoviesAsync(MovieQueryParameters movieQueryParameters)
        {
            if (movieQueryParameters == null)
            {
                throw new ArgumentNullException(nameof(movieQueryParameters));
            }

            string key = movieQueryParameters.GetKey();

            if (App.UseCache && cache.Contains(key) && cache.Get(key) is List<Movie> m)
            {
                return m;
            }

            string sql = App.SearchService.GetMovieIds(movieQueryParameters);

            List<Movie> movies = new List<Movie>();

            // retrieve the items
            if (!string.IsNullOrWhiteSpace(sql))
            {
                movies = (List<Movie>)await InternalCosmosDBSqlQuery<Movie>(sql).ConfigureAwait(false);
            }

            // add to cache
            cache.Add(new CacheItem(key, movies), cachePolicy);

            return movies;
        }

        /// <summary>
        /// Get the featured movie list from Cosmos
        /// </summary>
        /// <returns>List</returns>
        public async Task<List<string>> GetFeaturedMovieListAsync()
        {
            return await App.CacheDal.GetFeaturedMovieListAsync().ConfigureAwait(false);
        }
    }
}
