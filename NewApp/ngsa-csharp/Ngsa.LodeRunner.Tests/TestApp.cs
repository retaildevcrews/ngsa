using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Ngsa.LodeRunner;
using Xunit;

namespace CSE.LodeRunner.Tests
{
    public class TestApp
    {
        [Fact]
        public async Task IntegrationTest()
        {
            if (!string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("RUN_TEST_COVERAGE")))
            {
                string[] args = new string[]
                {
                    "-s",
                    "localhost:4120",
                    "-f",
                    "baseline.json",
                    "-l",
                    "1",
                };

                Assert.Equal(0, await App.Main(args).ConfigureAwait(false));

                args = new string[]
                {
                    "-s",
                    "localhost:4120",
                    "-f",
                    "baseline.json",
                    "-l",
                    "1",
                    "-r",
                    "--duration",
                    "5",
                };

                Assert.Equal(0, await App.Main(args).ConfigureAwait(false));

            }
        }
    }
}
