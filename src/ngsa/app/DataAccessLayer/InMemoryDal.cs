// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using CSE.NextGenSymmetricApp.Model;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CSE.NextGenSymmetricApp.DataAccessLayer
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "log params")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "json serialization")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "key")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2234:Pass system uri objects instead of strings", Justification = "Cosmos")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA1822:does not access instance data", Justification = "simplicity")]

    public class InMemoryDal : IDAL
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "use List<> for performance")]
        public InMemoryDal()
        {
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() },
            };

            using HttpClient client = new HttpClient { BaseAddress = new Uri("https://raw.githubusercontent.com/retaildevcrews/imdb/main/data/") };

            // load the data from the json files
            Actors = JsonConvert.DeserializeObject<List<Actor>>(client.GetStringAsync("actors.json").Result, settings);
            Actors.Sort((x, y) =>
            {
                if (x.Name == null && y.Name == null)
                {
                    return 0;
                }
                else if (x.Name == null)
                {
                    return -1;
                }
                else if (y.Name == null)
                {
                    return 1;
                }
                else
                {
                    if (x.TextSearch == y.TextSearch)
                    {
                        return string.Compare(x.TextSearch + x.Id, y.TextSearch + y.Id, StringComparison.OrdinalIgnoreCase);
                    }
                    else
                    {
                        return string.Compare(x.TextSearch, y.TextSearch, StringComparison.OrdinalIgnoreCase);
                    }
                }
            });

            Movies = JsonConvert.DeserializeObject<List<Movie>>(client.GetStringAsync("movies.json").Result, settings);
            Movies.Sort((x, y) =>
            {
                if (x.Title == null && y.Title == null)
                {
                    return 0;
                }
                else if (x.Title == null)
                {
                    return -1;
                }
                else if (y.Title == null)
                {
                    return 1;
                }
                else
                {
                    if (x.TextSearch == y.TextSearch)
                    {
                        return string.Compare(x.TextSearch + x.Id, y.TextSearch + y.Id, StringComparison.OrdinalIgnoreCase);
                    }
                    else
                    {
                        return string.Compare(x.TextSearch, y.TextSearch, StringComparison.OrdinalIgnoreCase);
                    }
                }
            });

            List<dynamic> list = JsonConvert.DeserializeObject<List<dynamic>>(client.GetStringAsync("genres.json").Result, settings);

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

        public async Task<Actor> GetActorAsync(string actorId)
        {
            return await Task.Run(() =>
            {
                Actor.ComputePartitionKey(actorId);

                foreach (Actor a in Actors)
                {
                    if (a.ActorId == actorId)
                    {
                        return a;
                    }
                }

                throw new CosmosException("Not Found", System.Net.HttpStatusCode.NotFound, 404, string.Empty, 0);
            }).ConfigureAwait(false);
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

        public Task<IEnumerable<Movie>> GetMoviesAsync(string q, string genre, int year = 0, double rating = 0.0, string actorId = "", int offset = 0, int limit = 100)
        {
            List<Movie> res = new List<Movie>();
            int skip = 0;
            bool add = false;

            foreach (Movie m in Movies)
            {
                if ((string.IsNullOrEmpty(q) || m.TextSearch.Contains(q, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrEmpty(genre) || m.Genres.Contains(genre, StringComparer.OrdinalIgnoreCase)) &&
                    (year < 1 || m.Year == year) &&
                    (rating <= 0 || m.Rating >= rating))
                {
                    add = true;

                    if (!string.IsNullOrEmpty(actorId))
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

            return Task<IEnumerable<Movie>>.Factory.StartNew(() => { return res; });
        }

        public Task<IEnumerable<string>> GetGenresAsync()
        {
            return Task<IEnumerable<string>>.Factory.StartNew(() => { return Genres; });
        }

        public async Task<Movie> GetMovieAsync(string movieId)
        {
            return await Task.Run(() =>
            {
                Movie.ComputePartitionKey(movieId);

                foreach (Movie m in Movies)
                {
                    if (m.MovieId == movieId)
                    {
                        return m;
                    }
                }

                throw new CosmosException("Not Found", System.Net.HttpStatusCode.NotFound, 404, string.Empty, 0);
            })
                .ConfigureAwait(false);
        }

        public Task<IEnumerable<Movie>> GetMoviesAsync(int offset = 0, int limit = 100)
        {
            return GetMoviesAsync(string.Empty, string.Empty, offset: offset, limit: limit);
        }

        public Task<List<string>> GetFeaturedMovieListAsync()
        {
            return Task<List<string>>.Factory.StartNew(() =>
            {
                return new List<string> { "tt0133093", "tt0120737", "tt0167260", "tt0167261", "tt0372784", "tt0172495", "tt0317705" };
            });
        }

        public Task Reconnect(Uri cosmosUrl, string cosmosKey, string cosmosDatabase, string cosmosCollection, bool force = false)
        {
            // do nothing
            return Task.CompletedTask;
        }

        public Task<IEnumerable<Actor>> GetActorsAsync(ActorQueryParameters qp)
        {
            if (qp == null)
            {
                return GetActorsAsync(string.Empty, 0, 100);
            }

            return GetActorsAsync(qp.Q, qp.GetOffset(), qp.PageSize);
        }

        public Task<IEnumerable<Movie>> GetMoviesAsync(MovieQueryParameters qp)
        {
            if (qp == null)
            {
                return GetMoviesAsync(string.Empty, string.Empty, offset: 0, limit: 100);
            }

            return GetMoviesAsync(qp.Q, qp.Genre, qp.Year, qp.Rating, qp.ActorId, qp.GetOffset(), qp.PageSize);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "simplicity")]
    public class GenreDoc
    {
        public string Genre { get; set; }
    }

    /// <summary>
    /// Extension to allow services.AddInMemoryDal()
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "simplicity")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:Static elements should appear before instance elements", Justification = "code readability")]
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
