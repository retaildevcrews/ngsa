using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using Imdb.Model;
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
            // run the web server for 40 seconds for integration test
            if (!string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("RUN_TEST_COVERAGE")))
            {
                Console.WriteLine("Starting web server");
                Task t = App.Main(null);

                await Task.Delay(45000);

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
                t.Wait(10);

                Console.WriteLine("Web server stopped");
            }
        }
    }
}
