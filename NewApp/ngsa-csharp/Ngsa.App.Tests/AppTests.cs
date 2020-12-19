using System;
using System.CommandLine;
using System.Threading.Tasks;
using Ngsa.App;
using Xunit;

namespace Tests
{
    public class AppTest
    {
        [Fact]
        public async Task RunApp()
        {
            // run the web server for 30 seconds for integration test
            if (!string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("RUN_TEST_COVERAGE")))
            {
                Console.WriteLine("Starting web server");
                Task t = App.Main(null);

                // let the service run for 30 seconds
                await Task.Delay(30000);

                // stop the service
                t.Wait(10);
                Console.WriteLine("Web server stopped");
            }
        }

        [Fact]
        public async Task CommandLineTests()
        {
            if (string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("RUN_TEST_COVERAGE")))
            {
                // test dry run, help and version
                Assert.Equal(0, await App.Main(new string[] { "-l", "Error", "--data-service", "http://localhost:4122/", "--help" }));
                Assert.Equal(0, await App.Main(new string[] { "--log-level", "Error", "-s", "http://localhost:4122/", "-d" }));
                Assert.Equal(0, await App.Main(new string[] { "-l", "Error", "--version" }));

                // test invalid command line options
                RootCommand root = App.BuildRootCommand();

                Assert.Equal(1, root.Parse("-l foo").Errors.Count);
                Assert.Equal(1, root.Parse("--foo").Errors.Count);
                Assert.Equal(2, root.Parse("--foo bar").Errors.Count);
            }
        }
    }
}
