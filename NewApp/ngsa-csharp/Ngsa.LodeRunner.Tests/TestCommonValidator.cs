using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Ngsa.LodeRunner;
using Ngsa.LodeRunner.Model;
using Ngsa.LodeRunner.Validators;
using Xunit;

namespace CSE.LodeRunner.Tests
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

                Assert.Empty(ResponseValidator.Validate(r, null, string.Empty).ValidationErrors);

                r.Validation = new Validation();

                Assert.NotEmpty(ResponseValidator.Validate(r, null, "this is a test").ValidationErrors);

                using HttpResponseMessage resp = new System.Net.Http.HttpResponseMessage(HttpStatusCode.NotFound);
                Assert.NotEmpty(ResponseValidator.Validate(r, resp, "this is a test").ValidationErrors);

                resp.StatusCode = HttpStatusCode.Moved;
                r.Validation = new Validation { StatusCode = 301 };
                Assert.Empty(ResponseValidator.Validate(r, resp, "this is a test").ValidationErrors);

                resp.StatusCode = HttpStatusCode.OK;
                r.Validation.StatusCode = 200;
                resp.Content = new StringContent("this is a test");
                Assert.NotEmpty(ResponseValidator.Validate(r, resp, "this is a test").ValidationErrors);

                r.Validation.ContentType = "text/plain";
                Assert.Empty(ResponseValidator.Validate(r, resp, null).ValidationErrors);
            }
        }

        [Fact]
        public void ParameterValidatorTest()
        {
            if (string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("RUN_TEST_COVERAGE")))
            {
                Assert.NotEmpty(ParameterValidator.Validate((Request)null).ValidationErrors);

                Assert.Empty(ParameterValidator.ValidateNotContains(null).ValidationErrors);
                Assert.Empty(ParameterValidator.ValidateNotContains(new List<string>()).ValidationErrors);
                Assert.NotEmpty(ParameterValidator.ValidateNotContains(new List<string> { "test", string.Empty }).ValidationErrors);
                Assert.Empty(ParameterValidator.ValidateNotContains(new List<string> { "test" }).ValidationErrors);

                Assert.Empty(ParameterValidator.ValidateContains(null).ValidationErrors);
                Assert.Empty(ParameterValidator.ValidateContains(new List<string>()).ValidationErrors);
                Assert.NotEmpty(ParameterValidator.ValidateContains(new List<string> { "test", string.Empty }).ValidationErrors);
                Assert.Empty(ParameterValidator.ValidateContains(new List<string> { "test" }).ValidationErrors);
            }
        }

        [Fact]
        public void ResponseValidatorTest()
        {
            if (string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("RUN_TEST_COVERAGE")))
            {
                Assert.NotEmpty(ResponseValidator.ValidateStatusCode(400, 200).ValidationErrors);

                Assert.Empty(ResponseValidator.ValidateNotContains(null, null).ValidationErrors);
                Assert.Empty(ResponseValidator.ValidateNotContains(new List<string>(), null).ValidationErrors);
                Assert.Empty(ResponseValidator.ValidateNotContains(new List<string> { "test" }, null).ValidationErrors);
                Assert.Empty(ResponseValidator.ValidateNotContains(new List<string> { "good" }, "bad").ValidationErrors);
                Assert.NotEmpty(ResponseValidator.ValidateNotContains(new List<string> { "good" }, "good").ValidationErrors);

                Assert.Empty(ResponseValidator.ValidateContains(null, null).ValidationErrors);
                Assert.Empty(ResponseValidator.ValidateContains(new List<string>(), null).ValidationErrors);
                Assert.NotEmpty(ResponseValidator.ValidateContains(new List<string> { "test" }, null).ValidationErrors);
                Assert.NotEmpty(ResponseValidator.ValidateContains(new List<string> { "good" }, "bad").ValidationErrors);
                Assert.Empty(ResponseValidator.ValidateContains(new List<string> { "good" }, "good").ValidationErrors);

                Assert.Empty(ResponseValidator.Validate(new Validation(), null).ValidationErrors);
                Assert.Empty(ResponseValidator.Validate(new List<JsonItem>(), null).ValidationErrors);
                Assert.NotEmpty(ResponseValidator.Validate(new List<JsonItem> { new JsonItem() }, null).ValidationErrors);

                Assert.NotEmpty(ResponseValidator.Validate(new JsonArray(), null).ValidationErrors);
                Assert.Empty(ResponseValidator.ValidateContentType(null, "bad").ValidationErrors);
                Assert.NotEmpty(ResponseValidator.ValidateContentType("good", "bad").ValidationErrors);
                Assert.Empty(ResponseValidator.ValidateContentType("good", "good").ValidationErrors);

                Assert.Empty(ResponseValidator.ValidateLength(1, null).ValidationErrors);
                Assert.NotEmpty(ResponseValidator.ValidateLength(1, new Validation { MinLength = 2, MaxLength = 10 }).ValidationErrors);
                Assert.NotEmpty(ResponseValidator.ValidateLength(11, new Validation { MinLength = 2, MaxLength = 10 }).ValidationErrors);
                Assert.Empty(ResponseValidator.ValidateLength(5, new Validation { MinLength = 2, MaxLength = 10 }).ValidationErrors);
                Assert.NotEmpty(ResponseValidator.ValidateLength(5, new Validation { Length = 10 }).ValidationErrors);
                Assert.Empty(ResponseValidator.ValidateLength(10, new Validation { Length = 10 }).ValidationErrors);

                Assert.Empty(ResponseValidator.ValidateExactMatch(null, null).ValidationErrors);
                Assert.NotEmpty(ResponseValidator.ValidateExactMatch("good", null).ValidationErrors);
                Assert.NotEmpty(ResponseValidator.ValidateExactMatch("good", "bad").ValidationErrors);
                Assert.Empty(ResponseValidator.ValidateExactMatch("good", "good").ValidationErrors);

                // TODO validate json array length
            }
        }

        [Fact]
        public void ConfigTest()
        {
            if (string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("RUN_TEST_COVERAGE")))
            {
                Config cfg = null;

                try
                { _ = new ValidationTest(cfg); }
                catch (ArgumentNullException) { }
                cfg = new Config();
                try
                { _ = new ValidationTest(cfg); }
                catch (ArgumentNullException) { }
                cfg.Files = new List<string>();
                try
                { _ = new ValidationTest(cfg); }
                catch (ArgumentNullException) { }
                cfg.Server = new List<string>();
                try
                { _ = new ValidationTest(cfg); }
                catch (ArgumentNullException) { }
                cfg.Server = new List<string> { "localhost", "bluebell" };
                try
                { _ = new ValidationTest(cfg); }
                catch (ArgumentException) { }

                cfg.Files = new List<string> { "baseline.json" };
                _ = new ValidationTest(cfg);

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
                ValidationTest wv = new ValidationTest(cfg);

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
