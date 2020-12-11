// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CSE.NextGenSymmetricApp.Model;
using Microsoft.Azure.Cosmos;

/// <summary>
/// This code is used to support performance testing
///
/// This loads the IMDb data into memory which removes the roundtrip to Cosmos
/// This provides higher performance and less variability which allows us to establish
/// baseline performance metrics
/// </summary>
namespace CSE.NextGenSymmetricApp.DataAccessLayer
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "log params aren't localized")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "json serialization requires read/write")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "key is lower case")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2234:Pass system uri objects instead of strings", Justification = "Cosmos requirement")]
    public class InMemoryDal : IDAL
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryDal"/> class.
        /// </summary>
        public InMemoryDal()
        {
            JsonSerializerOptions settings = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            // load the data from the json file
            Actors = JsonSerializer.Deserialize<List<Actor>>(File.ReadAllText("data/actors.json"), settings);

            // sort by Name
            Actors.Sort(Actor.NameCompare);

            // Loads an O(1) dictionary for retrieving by ID
            // Could also use a binary search to reduce memory usage
            foreach (Actor a in Actors)
            {
                ActorsIndex.Add(a.ActorId, a);
            }

            // load the data from the json file
            Movies = JsonSerializer.Deserialize<List<Movie>>(File.ReadAllText("data/movies.json"), settings);

            // sort by Title
            Movies.Sort(Movie.TitleCompare);

            string ge;

            foreach (Movie m in Movies)
            {
                // Loads an O(1) dictionary for retrieving by ID
                // Could also use a binary search to reduce memory usage
                MoviesIndex.Add(m.MovieId, m);

                // Add to by year dictionary
                if (!YearIndex.ContainsKey(m.Year))
                {
                    YearIndex.Add(m.Year, new List<Movie>());
                }

                YearIndex[m.Year].Add(m);

                // Add to by Genre dictionary
                foreach (string g in m.Genres)
                {
                    ge = g.ToLowerInvariant().Trim();

                    if (!GenreIndex.ContainsKey(ge))
                    {
                        GenreIndex.Add(ge, new List<Movie>());
                    }

                    GenreIndex[ge].Add(m);
                }
            }

            // load the data from the json file
            List<dynamic> list = JsonSerializer.Deserialize<List<dynamic>>(File.ReadAllText("data/genres.json"), settings);

            // Convert Genre object to List<string> per API spec
            foreach (dynamic g in list)
            {
                Genres.Add(g["genre"].Value);
            }

            Genres.Sort();
        }

        public static List<Actor> Actors { get; set; }
        public static List<Movie> Movies { get; set; }
        public static List<string> Genres { get; set; } = new List<string>();

        // O(1) dictionary for retrieving by ID
        public static Dictionary<string, Actor> ActorsIndex { get; set; } = new Dictionary<string, Actor>();
        public static Dictionary<string, Movie> MoviesIndex { get; set; } = new Dictionary<string, Movie>();
        public static Dictionary<int, List<Movie>> YearIndex { get; set; } = new Dictionary<int, List<Movie>>();

        // List subsets to improve search speed
        public static Dictionary<string, List<Movie>> GenreIndex { get; set; } = new Dictionary<string, List<Movie>>();

        /// <summary>
        /// Get a single actor by ID
        /// </summary>
        /// <param name="actorId">ID</param>
        /// <returns>Actor object</returns>
        public async Task<Actor> GetActorAsync(string actorId)
        {
            return await Task.Run(() => { return GetActor(actorId); }).ConfigureAwait(false);
        }

        /// <summary>
        /// Get a single actor by ID
        /// </summary>
        /// <param name="actorId">ID</param>
        /// <returns>Actor object</returns>
        public Actor GetActor(string actorId)
        {
            if (ActorsIndex.ContainsKey(actorId))
            {
                return ActorsIndex[actorId];
            }

            throw new CosmosException("Not Found", System.Net.HttpStatusCode.NotFound, 404, string.Empty, 0);
        }

        /// <summary>
        /// Get actors by search criteria
        /// </summary>
        /// <param name="actorQueryParameters">search criteria</param>
        /// <returns>List of Actor</returns>
        public Task<IEnumerable<Actor>> GetActorsAsync(ActorQueryParameters actorQueryParameters)
        {
            return Task<IEnumerable<Actor>>.Factory.StartNew(() =>
            {
                return GetActors(actorQueryParameters);
            });
        }

        /// <summary>
        /// Get actors by search criteria
        /// </summary>
        /// <param name="actorQueryParameters">search criteria</param>
        /// <returns>List of Actor</returns>
        public List<Actor> GetActors(ActorQueryParameters actorQueryParameters)
        {
            if (actorQueryParameters == null)
            {
                return GetActors(string.Empty, 0, 100);
            }

            return GetActors(actorQueryParameters.Q, actorQueryParameters.GetOffset(), actorQueryParameters.PageSize);
        }

        /// <summary>
        /// Worker function
        /// </summary>
        /// <param name="q">search query (optional)</param>
        /// <param name="offset">result offset</param>
        /// <param name="limit">page size</param>
        /// <returns>List of Actor</returns>
        public List<Actor> GetActors(string q, int offset = 0, int limit = 100)
        {
            List<Actor> res = new List<Actor>();
            int skip = 0;

            foreach (Actor a in Actors)
            {
                if (string.IsNullOrWhiteSpace(q) || a.TextSearch.Contains(q.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    if (skip < offset)
                    {
                        skip++;
                    }
                    else
                    {
                        res.Add(a);
                    }

                    if (res.Count >= limit)
                    {
                        break;
                    }
                }
            }

            return res;
        }

        /// <summary>
        /// Get list of featured Movie IDs
        /// </summary>
        /// <returns>List of IDs</returns>
        public Task<List<string>> GetFeaturedMovieListAsync()
        {
            return Task<List<string>>.Factory.StartNew(() =>
            {
                return GetFeaturedMovieList();
            });
        }

        /// <summary>
        /// Get list of featured Movie IDs
        /// </summary>
        /// <returns>List of IDs</returns>
        public List<string> GetFeaturedMovieList()
        {
            return new List<string>
            {
                "tt0133093",
                "tt0120737",
                "tt0167260",
                "tt0167261",
                "tt0372784",
                "tt0172495",
                "tt0317705",
            };
        }

        /// <summary>
        /// Get list of Genres
        /// </summary>
        /// <returns>List of Genres</returns>
        public async Task<IEnumerable<string>> GetGenresAsync()
        {
            return await Task<List<string>>.Factory.StartNew(() =>
            {
                return Genres;
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Get Movie by ID
        /// </summary>
        /// <param name="movieId">ID</param>
        /// <returns>Movie</returns>
        public async Task<Movie> GetMovieAsync(string movieId)
        {
            return await Task<Movie>.Factory.StartNew(() => { return GetMovie(movieId); }).ConfigureAwait(false);
        }

        /// <summary>
        /// Get Movie by ID
        /// </summary>
        /// <param name="movieId">ID</param>
        /// <returns>Movie</returns>
        public Movie GetMovie(string movieId)
        {
            if (MoviesIndex.ContainsKey(movieId))
            {
                return MoviesIndex[movieId];
            }

            throw new CosmosException("Not Found", System.Net.HttpStatusCode.NotFound, 404, string.Empty, 0);
        }

        /// <summary>
        /// Get Cosmos query string based on query parameters
        /// </summary>
        /// <param name="movieQueryParameters">query params</param>
        /// <returns>Cosmos query string</returns>
        public string GetMovieIds(MovieQueryParameters movieQueryParameters)
        {
            List<Movie> cache;
            string ids = string.Empty;

            if (movieQueryParameters == null)
            {
                cache = GetMovies(string.Empty, string.Empty, offset: 0, limit: 100);
            }
            else
            {
                cache = GetMovies(movieQueryParameters.Q, movieQueryParameters.Genre, movieQueryParameters.Year, movieQueryParameters.Rating, movieQueryParameters.ActorId, movieQueryParameters.GetOffset(), movieQueryParameters.PageSize);
            }

            foreach (Movie m in cache)
            {
                ids += $"'{m.Id}',";
            }

            // nothing found
            if (string.IsNullOrEmpty(ids))
            {
                return string.Empty;
            }

            string sql = "select m.id, m.partitionKey, m.movieId, m.type, m.textSearch, m.title, m.year, m.runtime, m.rating, m.votes, m.totalScore, m.genres, m.roles from m where m.id in ({0}) order by m.textSearch ASC, m.movieId ASC";
            return sql.Replace("{0}", ids[0..^1], StringComparison.Ordinal);
        }

        /// <summary>
        /// Get list of Movies based on query parameters
        /// </summary>
        /// <param name="movieQueryParameters">query params</param>
        /// <returns>List of Movie</returns>
        public List<Movie> GetMovies(MovieQueryParameters movieQueryParameters)
        {
            if (movieQueryParameters == null)
            {
                return GetMovies(string.Empty, string.Empty, offset: 0, limit: 100);
            }

            return GetMovies(movieQueryParameters.Q, movieQueryParameters.Genre, movieQueryParameters.Year, movieQueryParameters.Rating, movieQueryParameters.ActorId, movieQueryParameters.GetOffset(), movieQueryParameters.PageSize);
        }

        /// <summary>
        /// Get List of Movie by search params
        /// </summary>
        /// <param name="q">match title</param>
        /// <param name="genre">match genre</param>
        /// <param name="year">match year</param>
        /// <param name="rating">match rating</param>
        /// <param name="actorId">match Actor ID</param>
        /// <param name="offset">page offset</param>
        /// <param name="limit">page size</param>
        /// <returns>List of Movie</returns>
        public List<Movie> GetMovies(string q, string genre, int year = 0, double rating = 0.0, string actorId = "", int offset = 0, int limit = 100)
        {
            List<Movie> res = new List<Movie>();
            int skip = 0;
            bool add;

            if ((year > 0 || !string.IsNullOrWhiteSpace(genre)) &&
                !(year > 0 && !string.IsNullOrWhiteSpace(genre)) &&
                string.IsNullOrWhiteSpace(q) &&
                rating == 0 &&
                string.IsNullOrWhiteSpace(actorId) &&
                offset == 0)
            {
                if (year > 0)
                {
                    if (YearIndex.ContainsKey(year))
                    {
                        foreach (Movie m in YearIndex[year])
                        {
                            res.Add(m);

                            if (res.Count >= limit)
                            {
                                break;
                            }
                        }
                    }
                }
                else if (!string.IsNullOrWhiteSpace(genre))
                {
                    genre = genre.ToLowerInvariant().Trim();

                    if (GenreIndex.ContainsKey(genre))
                    {
                        foreach (Movie m in GenreIndex[genre])
                        {
                            if (res.Count < limit)
                            {
                                res.Add(m);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (Movie m in Movies)
                {
                    if ((string.IsNullOrWhiteSpace(q) || m.TextSearch.Contains(q, StringComparison.OrdinalIgnoreCase)) &&
                        (string.IsNullOrWhiteSpace(genre) || m.Genres.Contains(genre, StringComparer.OrdinalIgnoreCase)) &&
                        (year < 1 || m.Year == year) &&
                        (rating <= 0 || m.Rating >= rating))
                    {
                        add = true;

                        if (!string.IsNullOrWhiteSpace(actorId))
                        {
                            add = false;

                            actorId = actorId.Trim().ToLowerInvariant();

                            foreach (Role a in m.Roles)
                            {
                                if (a.ActorId == actorId)
                                {
                                    add = true;
                                    break;
                                }
                            }
                        }

                        if (add)
                        {
                            if (skip >= offset)
                            {
                                res.Add(m);

                                if (res.Count >= limit)
                                {
                                    break;
                                }
                            }
                            else
                            {
                                skip++;
                            }
                        }
                    }
                }
            }

            return res;
        }

        /// <summary>
        /// Get list of Movies based on query parameters
        /// </summary>
        /// <param name="movieQueryParameters">query params</param>
        /// <returns>List of Movie</returns>
        public Task<IEnumerable<Movie>> GetMoviesAsync(MovieQueryParameters movieQueryParameters)
        {
            return Task<IEnumerable<Movie>>.Factory.StartNew(() =>
            {
                return GetMovies(movieQueryParameters);
            });
        }

        // Part of IDal Interface - not used
        public Task Reconnect(Uri cosmosUrl, string cosmosKey, string cosmosDatabase, string cosmosCollection, bool force = false)
        {
            // do nothing
            return Task.CompletedTask;
        }
    }
}
