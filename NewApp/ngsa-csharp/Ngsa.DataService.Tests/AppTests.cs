using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Imdb.Model;
using Microsoft.Azure.Cosmos;
using Ngsa.DataService;
using Ngsa.DataService.DataAccessLayer;
using Ngsa.Middleware;
using Xunit;

namespace Tests
{
    public class AppTest
    {
        [Fact]
        public async Task RunApp()
        {
            // run the web server for integration test
            if (!string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("RUN_TEST_COVERAGE")))
            {
                Console.WriteLine("Starting web server");

                string[] args = new string[] { "--log-level", "Error" };

                Task t = App.Main(args);

                await Task.Delay(10000);

                if (App.InMemory)
                {
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

                // test Cosmos DAL
                if (App.CosmosDal is CosmosDal d)
                {
                    Assert.Equal(21, (await d.GetGenresAsync()).ToList().Count);

                    try
                    {
                        await d.GetActorsAsync(null);
                    }
                    catch (ArgumentNullException)
                    {
                    }

                    try
                    {
                        await d.GetActorAsync(null);
                    }
                    catch (ArgumentNullException)
                    {
                    }

                    try
                    {
                        await d.Reconnect(null, string.Empty, string.Empty, string.Empty, true);
                    }
                    catch (ArgumentNullException)
                    {
                    }

                    d.Dispose();
                }

                // end the app
                t.Wait(App.InMemory ? 5000 : 35000);

                Console.WriteLine("Web server stopped");
            }
        }
    }
}
