using System.CommandLine;
using System.Diagnostics;
using System.IO;
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
                Task t = App.Main(null);

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

                // stop the service
                t.Wait(1);
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
