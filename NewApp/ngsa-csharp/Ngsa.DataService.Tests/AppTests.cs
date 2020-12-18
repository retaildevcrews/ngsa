using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using Imdb.Model;
using Ngsa.DataService;
using Ngsa.DataService.DataAccessLayer;
using Ngsa.Middleware;
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

            args = new string[] { "--secrets-volume", "foo" };
            Assert.Equal(1, await App.Main(args));

            // run the web server for 40 seconds for integration test
            if (!string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("RUN_TEST_COVERAGE")))
            {
                Console.WriteLine("Starting web server");
                App.Main(Array.Empty<string>()).Wait(40000);
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
            Assert.Equal(0, root.Parse("--secrets-volume secrets").Errors.Count);

            Assert.Equal(1, root.Parse("-l foo").Errors.Count);
            Assert.Equal(1, root.Parse("--secrets-volume foo").Errors.Count);

            Assert.Equal(1, root.Parse("--foo").Errors.Count);
            Assert.Equal(2, root.Parse("-f bar").Errors.Count);
        }

        [Fact]
        public async Task InMemoryTests()
        {
            InMemoryDal dal = new InMemoryDal();

            Movie m = await dal.GetMovieAsync("tt0133093").ConfigureAwait(false);
            Assert.Equal("tt0133093", m.MovieId);

            Actor a = await dal.GetActorAsync("nm0000031").ConfigureAwait(false);
            Assert.Equal("nm0000031", a.ActorId);

            IEnumerable<Movie> movies = await dal.GetMoviesAsync(new MovieQueryParameters { Genre = "Action" }).ConfigureAwait(false);
            Assert.Equal(100, movies.ToList().Count);

            movies = await dal.GetMoviesAsync(new MovieQueryParameters { Genre = "Action" }).ConfigureAwait(false);
            Assert.Equal(100, movies.ToList().Count);

            movies = await dal.GetMoviesAsync(new MovieQueryParameters { Year = 1999 }).ConfigureAwait(false);
            Assert.Equal(39, movies.ToList().Count);

            movies = await dal.GetMoviesAsync(new MovieQueryParameters { Rating = 8.5 }).ConfigureAwait(false);
            Assert.Equal(20, movies.ToList().Count);

            movies = await dal.GetMoviesAsync(new MovieQueryParameters { ActorId = "nm0000206" }).ConfigureAwait(false);
            Assert.Equal(45, movies.ToList().Count);

            movies = await dal.GetMoviesAsync(new MovieQueryParameters { Q = "ring" }).ConfigureAwait(false);
            Assert.Equal(7, movies.ToList().Count);


            MovieQueryParameters mp = new MovieQueryParameters
            {
                Year = 2000,
                Genre = "Action",
                Rating = 7,
                Q = "Gladiator",
                ActorId = "nm0000128",
            };

            movies = await dal.GetMoviesAsync(mp).ConfigureAwait(false);
            Assert.Single(movies.ToList());
        }
    }
}
