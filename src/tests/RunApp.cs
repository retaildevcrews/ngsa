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

            // test command line parser
            RootCommand root = App.BuildRootCommand();

            Assert.Equal(1, root.Parse("-foo").Errors.Count);
            Assert.Equal(2, root.Parse("-foo bar").Errors.Count);

            args = new string[] { "--help" };
            Assert.Equal(0, await App.Main(args));
        }
    }
}
