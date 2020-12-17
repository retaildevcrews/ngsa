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
            string[] args;

            args = new string[] { "-l", "Warning", "--help", "-d", "--version" };
            Assert.Equal(0, await App.Main(args));

            // run the web server for 30 seconds for integration test
            if (!string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("RUN_TEST_COVERAGE")))
            {
                Console.WriteLine("Starting web server");
                App.Main(Array.Empty<string>()).Wait(30000);
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
            Assert.Equal(1, root.Parse("-l foo").Errors.Count);

            Assert.Equal(1, root.Parse("-foo").Errors.Count);
            Assert.Equal(2, root.Parse("-foo bar").Errors.Count);
        }
    }
}
