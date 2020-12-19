using System.Collections.Generic;
using Ngsa.LodeRunner.Model;
using Ngsa.LodeRunner.Validators;
using Xunit;

namespace CSE.WebValidate.Tests.Unit
{
    public class TestArrayValidator
    {
        [Fact]
        public void JsonArrayTest()
        {
            if (string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("RUN_TEST_COVERAGE")))
            {
                ValidationResult res;
                JsonArray a;

                // validate empty array
                a = new JsonArray();
                res = ParameterValidator.Validate(a);
                Assert.False(res.Failed);

                // validate bad count
                a = new JsonArray
                {
                    Count = -1
                };
                res = ParameterValidator.Validate(a);
                Assert.True(res.Failed);

                // validate bad count
                a = new JsonArray
                {
                    Count = 1,
                    MinCount = 1
                };
                res = ParameterValidator.Validate(a);
                Assert.True(res.Failed);

                // validate bad count
                a = new JsonArray
                {
                    MaxCount = 1,
                    MinCount = 1
                };
                res = ParameterValidator.Validate(a);
                Assert.True(res.Failed);
            }
        }

        [Fact]
        public void ByIndexTest()
        {
            if (string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("RUN_TEST_COVERAGE")))
            {
                List<JsonPropertyByIndex> list = new List<JsonPropertyByIndex>();
                JsonPropertyByIndex f;

                // empty list is valid
                Assert.False(ParameterValidator.Validate(list).Failed);

                // validate index < 0 fails
                f = new JsonPropertyByIndex
                {
                    Index = -1,
                    Value = null,
                    Validation = null
                };
                list.Add(f);
                Assert.True(ParameterValidator.Validate(list).Failed);

                // validate field, value, validation
                f = new JsonPropertyByIndex
                {
                    Index = 0,
                    Field = null,
                    Value = null,
                    Validation = null
                };
                list.Clear();
                list.Add(f);
                Assert.True(ParameterValidator.Validate(list).Failed);
            }
        }
    }
}
