using System.Collections;
using System.Collections.Generic;
using CSE.NextGenSymmetricApp;
using CSE.NextGenSymmetricApp.Controllers;
using CSE.NextGenSymmetricApp.DataAccessLayer;
using CSE.NextGenSymmetricApp.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace tests
{
    public class ExtensionTests
    {
        [Theory]
        [ClassData(typeof(MovieQueryParameterData))]
        public void GivenMovieParameter_ValidateString_ReturnsValidMethodName(string queryProperty, string queryValue, MovieQueryParameters parameterObject)
        {
            // Arrange
            Mock<ILogger<MoviesController>> logger = new Mock<ILogger<MoviesController>>();
            Mock<IDAL> mockIDAL = new Mock<IDAL>();

            MoviesController controller = new MoviesController(logger.Object, mockIDAL.Object)
            {
                ControllerContext = new ControllerContext()
            };

            Dictionary<string, StringValues> request = new Dictionary<string, StringValues>
            {
                { queryProperty, queryValue }
            };

            QueryCollection queryCollection = new QueryCollection(request);
            QueryFeature query = new QueryFeature(queryCollection);
            FeatureCollection features = new FeatureCollection();
            features.Set<IQueryFeature>(query);
            controller.ControllerContext.HttpContext = new DefaultHttpContext(features);

            // Act
            string expectedResult = $"GetMovies:{queryProperty}:{queryValue}";
            string actualResult = parameterObject.GetMethodText(controller.HttpContext);

            // Assert
            Assert.Equal(expectedResult, actualResult);
        }

        private class MovieQueryParameterData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { "q", "ring", new MovieQueryParameters { Q = "ring" } };
                yield return new object[] { "genre", "Action", new MovieQueryParameters { Genre = "Action" } };
                yield return new object[] { "year", "1999", new MovieQueryParameters { Year = 1999 } };
                yield return new object[] { "rating", "1.1", new MovieQueryParameters { Rating = 1.1 } };
                yield return new object[] { "actorId", "nm123456", new MovieQueryParameters { ActorId = "nm123456" } };
                yield return new object[] { "pageNumber", "1", new MovieQueryParameters { PageNumber = 1 } };
                yield return new object[] { "pageSize", "1", new MovieQueryParameters { PageSize = 1 } };
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        [Theory]
        [ClassData(typeof(ActorQueryParameterData))]
        public void GivenActorParameter_ValidateString_ReturnsValidMethodName(string queryProperty, string queryValue, ActorQueryParameters parameterObject)
        {
            // Arrange
            Mock<ILogger<ActorsController>> logger = new Mock<ILogger<ActorsController>>();
            Mock<IDAL> mockIDAL = new Mock<IDAL>();

            ActorsController controller = new ActorsController(logger.Object, mockIDAL.Object)
            {
                ControllerContext = new ControllerContext()
            };

            Dictionary<string, StringValues> request = new Dictionary<string, StringValues>
            {
                { queryProperty, queryValue }
            };

            QueryCollection queryCollection = new QueryCollection(request);
            QueryFeature query = new QueryFeature(queryCollection);
            FeatureCollection features = new FeatureCollection();
            features.Set<IQueryFeature>(query);

            controller.ControllerContext.HttpContext = new DefaultHttpContext(features);

            // Act
            string expectedResult = $"GetActors:{queryProperty}:{queryValue}";
            string actualResult = parameterObject.GetMethodText(controller.HttpContext);

            // Assert
            Assert.Equal(expectedResult, actualResult);
        }

        private class ActorQueryParameterData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                yield return new object[] { "q", "ring", new ActorQueryParameters { Q = "ring" } };
                yield return new object[] { "pageNumber", "1", new ActorQueryParameters { PageNumber = 1 } };
                yield return new object[] { "pageSize", "1", new ActorQueryParameters { PageSize = 1 } };
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}
