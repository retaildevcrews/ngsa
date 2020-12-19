using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using System.Threading.Tasks;
using Ngsa.LodeRunner;
using Ngsa.LodeRunner.Model;
using Ngsa.LodeRunner.Validators;
using Xunit;

namespace CSE.WebValidate.Tests.Unit
{
    public class TestCommonTarget
    {
        [Fact]
        public void PathTest()
        {
            if (string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("RUN_TEST_COVERAGE")))
            {

                ValidationResult res;

                // empty path
                res = ParameterValidator.ValidatePath(string.Empty);
                Assert.True(res.Failed);

                // path must start with /
                res = ParameterValidator.ValidatePath("testpath");
                Assert.True(res.Failed);
            }
        }

        [Fact]
        public void CommonBoundariesTest()
        {
            if (string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("RUN_TEST_COVERAGE")))
            {
                ValidationResult res;

                // verb must be GET POST PUT DELETE ...
                // path must start with /
                Request r = new Request
                {
                    Verb = "badverb",
                    Path = "badpath",
                    Validation = null
                };
                res = ParameterValidator.Validate(r);
                Assert.True(res.Failed);

                Validation v = new Validation();

                // null is valid
                res = ParameterValidator.ValidateLength(null);
                Assert.False(res.Failed);

                // edge values
                // >= 0
                v.Length = -1;
                v.MinLength = -1;
                v.MaxLength = -1;

                // 200 - 599
                v.StatusCode = 10;

                // > 0
                v.MaxMilliseconds = 0;

                // ! isnullorempty
                v.ExactMatch = string.Empty;
                v.ContentType = string.Empty;

                // each element ! isnullempty
                v.Contains = new List<string> { string.Empty };
                v.NotContains = new List<string> { string.Empty };

                res = ParameterValidator.Validate(v);
                Assert.True(res.Failed);
            }
        }

        [Fact]
        public void PerfTargetTest()
        {
            if (string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("RUN_TEST_COVERAGE")))
            {
                ValidationResult res;

                // category can't be blank
                PerfTarget t = new PerfTarget();
                res = ParameterValidator.Validate(t);
                Assert.True(res.Failed);

                // quartiles can't be null
                t.Category = "Tests";
                res = ParameterValidator.Validate(t);
                Assert.True(res.Failed);

                // valid
                t.Quartiles = new List<double> { 100, 200, 400 };
                res = ParameterValidator.Validate(t);
                Assert.False(res.Failed);
            }
        }

        [Fact]
        public void ResponseNullTest()
        {
            if (string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("RUN_TEST_COVERAGE")))
            {
                Request r = new Request();

                //Assert.False(ResponseValidator.Validate(r, null, string.Empty).Failed);

                r.Validation = new Validation();

                Assert.True(ResponseValidator.Validate(r, null, "this is a test").Failed);

                //using System.Net.Http.HttpResponseMessage resp = new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
                //Assert.True(ResponseValidator.Validate(r, resp, "this is a test").Failed);

                //Assert.True(ResponseValidator.ValidateStatusCode(400, 200).Failed);
            }
        }

        [Fact]
        public async Task CommandArgsTest()
        {
            if (string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("RUN_TEST_COVERAGE")))
            {
                // no params displays usage
                Assert.Equal(1, await App.Main(null).ConfigureAwait(false));

                // test remaining valid parameters
                string[] args = new string[] { "--random", "--verbose" };
                Assert.Equal(1, await App.Main(args).ConfigureAwait(false));

                // test bad param
                args = new string[] { "test" };
                Assert.Equal(1, await App.Main(args).ConfigureAwait(false));

                // test bad param with good param
                args = new string[] { "-s", "test", "test" };
                Assert.Equal(1, await App.Main(args).ConfigureAwait(false));
            }
        }

        [Fact]
        public async Task ValidateAllJsonFilesTest()
        {
            if (string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("RUN_TEST_COVERAGE")))
            {
                // test all files
                Config cfg = new Config
                {
                    Server = new List<string> { "http://localhost" },
                    Timeout = 30,
                    MaxConcurrent = 100,
                };
                cfg.Files.Add("baseline.json");

                // load and validate all of our test files
                WebV wv = new WebV(cfg);

                // file not found test
                Assert.Null(wv.ReadJson("bad-file-name"));

                // test with null config
                Assert.NotEqual(0, await wv.RunOnce(null, new System.Threading.CancellationToken()).ConfigureAwait(false));
            }
        }

        [Fact]
        public void EnvironmentVariableTest()
        {
            if (string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("RUN_TEST_COVERAGE")))
            {
                RootCommand root = App.BuildRootCommand();
                ParseResult parse;

                // set all env vars
                System.Environment.SetEnvironmentVariable(EnvKeys.Files, "msft.json");
                System.Environment.SetEnvironmentVariable(EnvKeys.Server, "test");
                System.Environment.SetEnvironmentVariable(EnvKeys.MaxConcurrent, "100");
                System.Environment.SetEnvironmentVariable(EnvKeys.Random, "false");
                System.Environment.SetEnvironmentVariable(EnvKeys.RequestTimeout, "30");
                System.Environment.SetEnvironmentVariable(EnvKeys.RunLoop, "false");
                System.Environment.SetEnvironmentVariable(EnvKeys.Sleep, "1000");
                System.Environment.SetEnvironmentVariable(EnvKeys.Verbose, "false");
                System.Environment.SetEnvironmentVariable(EnvKeys.VerboseErrors, "false");
                System.Environment.SetEnvironmentVariable(EnvKeys.DelayStart, "1");

                // test env vars
                parse = root.Parse(string.Empty);
                Assert.Equal(0, parse.Errors.Count);
                Assert.Equal(16, parse.CommandResult.Children.Count);

                // override the files env var
                parse = root.Parse("-f file1 file2");
                Assert.Equal(0, parse.Errors.Count);
                Assert.Equal(16, parse.CommandResult.Children.Count);
                Assert.Equal(2, parse.CommandResult.Children.First(c => c.Symbol.Name == "files").Tokens.Count);

                // test run-loop
                System.Environment.SetEnvironmentVariable(EnvKeys.Duration, "30");
                parse = root.Parse(string.Empty);
                Assert.Equal(1, parse.Errors.Count);

                // test run-loop
                System.Environment.SetEnvironmentVariable(EnvKeys.Random, "true");
                parse = root.Parse(string.Empty);
                Assert.Equal(1, parse.Errors.Count);

                // clear env vars
                System.Environment.SetEnvironmentVariable(EnvKeys.Duration, null);
                System.Environment.SetEnvironmentVariable(EnvKeys.Files, null);
                System.Environment.SetEnvironmentVariable(EnvKeys.Server, null);
                System.Environment.SetEnvironmentVariable(EnvKeys.MaxConcurrent, null);
                System.Environment.SetEnvironmentVariable(EnvKeys.Random, null);
                System.Environment.SetEnvironmentVariable(EnvKeys.RequestTimeout, null);
                System.Environment.SetEnvironmentVariable(EnvKeys.RunLoop, null);
                System.Environment.SetEnvironmentVariable(EnvKeys.Sleep, null);
                System.Environment.SetEnvironmentVariable(EnvKeys.Verbose, null);
                System.Environment.SetEnvironmentVariable(EnvKeys.VerboseErrors, null);
                System.Environment.SetEnvironmentVariable(EnvKeys.DelayStart, null);

                // isnullempty fails
                Assert.False(App.CheckFileExists(string.Empty));

                // isnullempty fails
                Assert.False(App.CheckFileExists("testFileNotFound"));
            }
        }

        [Fact]
        public void FlagTest()
        {
            if (string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("RUN_TEST_COVERAGE")))
            {
                RootCommand root = App.BuildRootCommand();
                ParseResult parse;

                // bool flags can be specified with just the flag name (-r) or with a value (-v false)
                string args = "-s test -f test.json -r -v false --random true";

                parse = root.Parse(args);

                Assert.Equal(0, parse.Errors.Count);

                SymbolResult result = parse.CommandResult.Children.FirstOrDefault(c => c.Symbol.Name == "run-loop");
                Assert.NotNull(result);
                Assert.Equal(0, result.Tokens.Count);

                result = parse.CommandResult.Children.FirstOrDefault(c => c.Symbol.Name == "random");
                Assert.NotNull(result);
                Assert.Equal(1, result.Tokens.Count);
                Assert.Equal("true", result.Tokens[0].Value);

                result = parse.CommandResult.Children.FirstOrDefault(c => c.Symbol.Name == "verbose");
                Assert.NotNull(result);
                Assert.Equal(1, result.Tokens.Count);
                Assert.Equal("false", result.Tokens[0].Value);

                args = "-s test -f test.json -r -v false --random badvalue";

                parse = root.Parse(args);

                Assert.Equal(1, parse.Errors.Count);
            }
        }
    }
}
