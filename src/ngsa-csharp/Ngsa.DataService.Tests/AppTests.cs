using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
            if (!string.IsNullOrWhiteSpace(System.Environment.GetEnvironmentVariable("RUN_TEST_COVERAGE")))
            {
                Task t = App.Main(new string[] { "--log-level", "Information" });

                await Task.Delay(10000);

                // test in memory DAL
                if (App.InMemory)
                {
                    InMemoryDal dal = new InMemoryDal();

                    dal.GetActorIds(null);
                    dal.GetMovieIds(null);
                    await dal.Reconnect(null, string.Empty, string.Empty, string.Empty, false);

                    List<Actor> actors = dal.GetActors(null);
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
                }

                Stopwatch sw = new Stopwatch();
                sw.Start();

                // wait up to 45 seconds for the file semaphore
                while (sw.ElapsedMilliseconds < 45000)
                {
                    if (File.Exists("../../../../tests-complete"))
                    {
                        break;
                    }

                    await Task.Delay(1000);
                }

                // end the app
                t.Wait(1);
            }
        }
    }
}
