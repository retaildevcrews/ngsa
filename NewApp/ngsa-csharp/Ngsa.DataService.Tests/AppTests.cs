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
            string[] args;

            args = new string[] { "-l", "Warning", "--help", "-d", "--version" };
            Assert.Equal(0, await App.Main(args));

            args = new string[] { "--secrets-volume", "foo" };
            Assert.Equal(1, await App.Main(args));

            // run the web server for 40 seconds for integration test
            if (!string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("RUN_TEST_COVERAGE")))
            {
                Console.WriteLine("Starting web server");
                Task t = App.Main(null);

                await Task.Delay(30000);

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

        [Fact]
        public void CommandLineTests()
        {
            // test command line parser
            RootCommand root = App.BuildRootCommand();

            Assert.Equal(0, root.Parse("-d").Errors.Count);
            Assert.Equal(0, root.Parse("-l Error").Errors.Count);
            Assert.Equal(0, root.Parse("--secrets-volume secrets").Errors.Count);

            Assert.Equal(1, root.Parse("-l foo").Errors.Count);
            Assert.Equal(1, root.Parse("--secrets-volume foo").Errors.Count);

            Assert.Equal(1, root.Parse("--foo").Errors.Count);
            Assert.Equal(2, root.Parse("-f bar").Errors.Count);

            Assert.Equal(1, root.Parse("--in-memory --no-cache --perf-cache 0 --cache-duration 0 --secrets-volume notfound -l Error").Errors.Count);
        }
    }
}
