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
    public class CommandLineTests
    {
        [Fact]
        public async Task CommandTests()
        {
            if (string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("RUN_TEST_COVERAGE")))
            {
                // test command line
                Assert.Equal(0, await App.Main(new string[] { "-d", "-l", "Error", "--secrets-volume", "secrets", "--cache-duration", "60", "--perf-cache", "100" }).ConfigureAwait(false));
                Assert.Equal(0, await App.Main(new string[] { "--version", "--log-level", "Error",  }).ConfigureAwait(false));
                Assert.Equal(0, await App.Main(new string[] { "--help", }).ConfigureAwait(false));

                // test command line parser errors
                RootCommand root = App.BuildRootCommand();

                Assert.Equal(1, root.Parse("-l foo").Errors.Count);
                Assert.Equal(1, root.Parse("--secrets-volume foo").Errors.Count);

                Assert.Equal(1, root.Parse("--foo").Errors.Count);
                Assert.Equal(2, root.Parse("-f bar").Errors.Count);

                Assert.Equal(1, root.Parse("--in-memory --no-cache --perf-cache 0 --cache-duration 0 --secrets-volume notfound -l Error").Errors.Count);
            }
        }
    }
}
