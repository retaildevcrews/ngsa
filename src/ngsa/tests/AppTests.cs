using System;
using System.CommandLine;
using System.Threading.Tasks;
using CSE.NextGenSymmetricApp;
using Xunit;

namespace Tests
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable")]
    public class AppTest
    {
        [Fact]
        public async Task RunApp()
        {
            string[] args;

            args = new string[] { "-l", "Warning", "--help", "-d", "--version" };
            Assert.Equal(0, await App.Main(args));

            args = new string[] { "--secrets-volume", "foo" };
            Assert.Equal(-1, await App.Main(args));

            // run the web server for 15 seconds for integration test
            if (!string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("RUN_TEST_COVERAGE")))
            {
                Console.WriteLine("Starting web server");
                App.Main(Array.Empty<string>()).Wait(15000);
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
            Assert.Equal(0, root.Parse("--secrets-volume foo").Errors.Count);

            Assert.Equal(1, root.Parse("-foo").Errors.Count);
            Assert.Equal(2, root.Parse("-foo bar").Errors.Count);
        }
    }
}
