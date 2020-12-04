// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CSE.NextGenSymmetricApp.Model;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

/// <summary>
/// This code is used to support performance testing
/// </summary>
namespace CSE.NextGenSymmetricApp.DataAccessLayer
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "log params")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "json serialization")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "key")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2234:Pass system uri objects instead of strings", Justification = "Cosmos")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1822:does not access instance data", Justification = "simplicity")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "use List<> for performance")]
    public class InMemoryDal : IDAL
    {
        public InMemoryDal()
        {
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() },
            };

            // load the data from the json files
            Actors = JsonConvert.DeserializeObject<List<Actor>>(client.GetStringAsync("actors.json").Result, settings);
            Actors.Sort();

            foreach (Actor a in Actors)
            {
                ActorsIndex.Add(a.ActorId, a);
            }

            Movies = JsonConvert.DeserializeObject<List<Movie>>(client.GetStringAsync("movies.json").Result, settings);
            Movies.Sort();

            string ge;

            foreach (Movie m in Movies)
            {
                MoviesIndex.Add(m.MovieId, m);

                if (!YearIndex.ContainsKey(m.Year))
                {
                    YearIndex.Add(m.Year, new List<Movie>());
                }

                YearIndex[m.Year].Add(m);

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

            List<dynamic> list = JsonConvert.DeserializeObject<List<dynamic>>(File.ReadAllText("data/genres.json"), settings);
            Genres = new List<string>();

            foreach (dynamic g in list)
            {
                Genres.Add(g["genre"].Value);
            }

            Genres.Sort();
        }

        public static List<Actor> Actors { get; set; }
        public static List<Movie> Movies { get; set; }
        public static List<string> Genres { get; set; }
        public static Dictionary<string, Actor> ActorsIndex { get; set; } = new Dictionary<string, Actor>();
        public static Dictionary<string, Movie> MoviesIndex { get; set; } = new Dictionary<string, Movie>();
        public static Dictionary<int, List<Movie>> YearIndex { get; set; } = new Dictionary<int, List<Movie>>();
        public static Dictionary<string, List<Movie>> GenreIndex { get; set; } = new Dictionary<string, List<Movie>>();

        public async Task<Actor> GetActorAsync(string actorId)
        {
            return await Task.Run(() =>
            {
                if (ActorsIndex.ContainsKey(actorId))
                {
                    return ActorsIndex[actorId];
                }

                throw new CosmosException("Not Found", System.Net.HttpStatusCode.NotFound, 404, string.Empty, 0);
            }).ConfigureAwait(false);
        }

        public Task<IEnumerable<Actor>> GetActorsAsync(ActorQueryParameters actorQueryParameters)
        {
            if (actorQueryParameters == null)
            {
                return GetActorsAsync(string.Empty, 0, 100);
            }

            return GetActorsAsync(actorQueryParameters.Q, actorQueryParameters.GetOffset(), actorQueryParameters.PageSize);
        }

        public Task<IEnumerable<Actor>> GetActorsAsync(string q, int offset = 0, int limit = 100)
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

            return Task<IEnumerable<Actor>>.Factory.StartNew(() => { return res; });
        }

        public Task<List<string>> GetFeaturedMovieListAsync()
        {
            return Task<List<string>>.Factory.StartNew(() =>
            {
                return new List<string> { "tt0133093", "tt0120737", "tt0167260", "tt0167261", "tt0372784", "tt0172495", "tt0317705" };
            });
        }

        public async Task<IEnumerable<string>> GetGenresAsync()
        {
            return await Task.Run(() =>
            {
                return Genres.AsEnumerable<string>();
            }).ConfigureAwait(false);
        }

        public async Task<Movie> GetMovieAsync(string movieId)
        {
            return await Task.Run(() =>
            {
                if (MoviesIndex.ContainsKey(movieId))
                {
                    return MoviesIndex[movieId];
                }

                throw new CosmosException("Not Found", System.Net.HttpStatusCode.NotFound, 404, string.Empty, 0);
            }).ConfigureAwait(false);
        }

        public Task<IEnumerable<Movie>> GetMoviesAsync(MovieQueryParameters movieQueryParameters)
        {
            if (movieQueryParameters == null)
            {
                return GetMoviesAsync(string.Empty, string.Empty, offset: 0, limit: 100);
            }

            return GetMoviesAsync(movieQueryParameters.Q, movieQueryParameters.Genre, movieQueryParameters.Year, movieQueryParameters.Rating, movieQueryParameters.ActorId, movieQueryParameters.GetOffset(), movieQueryParameters.PageSize);
        }

        public Task<IEnumerable<Movie>> GetMoviesAsync(string q, string genre, int year = 0, double rating = 0.0, string actorId = "", int offset = 0, int limit = 100)
        {
            List<Movie> res = new List<Movie>();
            int skip = 0;
            bool add = false;

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

            return Task<IEnumerable<Movie>>.Factory.StartNew(() => { return res; });
        }

        public Task Reconnect(Uri cosmosUrl, string cosmosKey, string cosmosDatabase, string cosmosCollection, bool force = false)
        {
            // do nothing
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Extension to allow services.AddInMemoryDal()
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "simplicity")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:Static elements should appear before instance elements", Justification = "simplicity")]
    public static class InMemoryDataAccessLayerExtension
    {
        public static IServiceCollection AddInMemoryDal(this IServiceCollection services)
        {
            // add the data access layer as a singleton
            services.AddSingleton<IDAL>(new InMemoryDal());

            return services;
        }
    }
}
