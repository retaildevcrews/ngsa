using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Ngsa.LodeRunner;
using Xunit;

namespace CSE.WebValidate.Tests.Unit
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
                    "http://localhost:4120",
                    "-f",
                    "baseline.json",
                };

                Assert.Equal(0, await App.Main(args).ConfigureAwait(false));
            }
        }
    }
}
