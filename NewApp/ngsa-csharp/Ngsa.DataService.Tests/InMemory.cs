using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Imdb.Model;
using Microsoft.Azure.Cosmos;
using Ngsa.DataService;
using Ngsa.DataService.DataAccessLayer;
using Ngsa.Middleware;
using Xunit;

namespace Tests
{
    public class InMemoryTest
    {
        [Fact]
        public async Task InMemoryTests()
        {
            if (string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("RUN_TEST_COVERAGE")))
            {
                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                };

                // test --dry-run
                Assert.Equal(0, await App.Main(new string[] { "-d" }).ConfigureAwait(false));

                Task t = App.Main(new string[] { "--in-memory", "--log-level", "Error" });

                using HttpClient client = new HttpClient
                {
                    BaseAddress = new Uri("http://localhost:4122/"),
                    Timeout = TimeSpan.FromSeconds(2),
                };

                string json = await client.GetStringAsync("/version").ConfigureAwait(false);

                json = await client.GetStringAsync("/api/movies/tt0133093").ConfigureAwait(false);
                Movie m = JsonSerializer.Deserialize<Movie>(json, options);
                Assert.Equal("tt0133093", m.MovieId);

                json = await client.GetStringAsync("/api/actors/nm0000031").ConfigureAwait(false);
                Actor a = JsonSerializer.Deserialize<Actor>(json, options);
                Assert.Equal("nm0000031", a.ActorId);

                json = await client.GetStringAsync("/api/movies?genre=action").ConfigureAwait(false);
                List<Movie> movies = JsonSerializer.Deserialize<List<Movie>>(json, options);
                Assert.Equal(100, movies.Count);

                json = await client.GetStringAsync("/api/movies?year=1999").ConfigureAwait(false);
                movies = movies = JsonSerializer.Deserialize<List<Movie>>(json, options);
                Assert.Equal(39, movies.Count);

                json = await client.GetStringAsync("/api/movies?year=1999&pageSize=10").ConfigureAwait(false);
                movies = movies = JsonSerializer.Deserialize<List<Movie>>(json, options);
                Assert.Equal(10, movies.Count);

                json = await client.GetStringAsync("/api/movies?rating=8.5").ConfigureAwait(false);
                movies = movies = JsonSerializer.Deserialize<List<Movie>>(json, options);
                Assert.Equal(20, movies.Count);

                json = await client.GetStringAsync("/api/movies?actorId=nm0000206").ConfigureAwait(false);
                movies = movies = JsonSerializer.Deserialize<List<Movie>>(json, options);
                Assert.Equal(45, movies.Count);

                json = await client.GetStringAsync("/api/movies?q=ring").ConfigureAwait(false);
                movies = movies = JsonSerializer.Deserialize<List<Movie>>(json, options);
                Assert.Equal(7, movies.Count);

                json = await client.GetStringAsync("/api/movies?year=2000&genre=Action&rating=7&q=Gladiator&actorId=nm0000128").ConfigureAwait(false);
                movies = movies = JsonSerializer.Deserialize<List<Movie>>(json, options);
                Assert.Single(movies);

                // stop the service
                t.Wait(10);

                InMemoryDal dal = new InMemoryDal();

                dal.GetActorIds(null);
                dal.GetMovieIds(null);
                await dal.Reconnect(null, string.Empty, string.Empty, string.Empty, false);

                var actors = dal.GetActors(null);
                Assert.Equal(100, actors.Count);

                actors = dal.GetActors(new ActorQueryParameters { Q = "Nicole" });
                Assert.Equal(5, actors.Count);

                Assert.Equal(100, dal.GetMovies(null).Count);

                try
                {
                    dal.GetActor("notfound");
                }
                catch (CosmosException ex)
                {
                    Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
                }

                try
                {
                    dal.GetMovie("notfound");
                }
                catch (CosmosException ex)
                {
                    Assert.Equal(HttpStatusCode.NotFound, ex.StatusCode);
                }
            }
        }
    }
}
