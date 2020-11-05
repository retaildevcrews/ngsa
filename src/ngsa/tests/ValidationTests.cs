﻿using System.ComponentModel.DataAnnotations;
using CSE.NextGenSymmetricApp;
using Xunit;

namespace Tests
{
    public class ValidationTests
    {
        [Theory]
        [InlineData(12, true)]
        [InlineData(1200, true)]
        [InlineData(10000, true)]
        [InlineData(10001, false)]
        [InlineData(10006, false)]
        [InlineData(10007, false)]
        [InlineData(10002, false)]
        [InlineData(0, false)]
        public void PageNumberInput_ValidateModel_ReturnsExpectedResult(int input, bool expectedResult)
        {
            // Arrange
            ActorQueryParameters queryParameters = new ActorQueryParameters { PageNumber = input };

            // Act
            bool actualValue = IsValidProperty(queryParameters, input, "PageNumber");

            // Assert
            Assert.Equal(expectedResult, actualValue);
        }

        [Theory]
        [InlineData(12, true)]
        [InlineData(1000, true)]
        [InlineData(1001, false)]
        [InlineData(0, false)]
        public void PageSizeInput_ValidateModel_ReturnsExpectedResult(int input, bool expectedResult)
        {
            // Arrange
            ActorQueryParameters queryParameters = new ActorQueryParameters();

            // Act
            bool actualValue = IsValidProperty(queryParameters, input, "PageSize");

            // Assert
            Assert.Equal(expectedResult, actualValue);
        }

        [Theory]
        [InlineData("aaa", true)]
        [InlineData("AAA", true)]
        [InlineData("12345678901234567890", true)]
        [InlineData("123456789012345678901", false)]
        [InlineData("aa", false)]
        public void GenreInput_ValidateModel_ReturnsExpectedResult(string input, bool expectedResult)
        {
            // Arrange
            MovieQueryParameters queryParameters = new MovieQueryParameters();

            // Act
            bool actualValue = IsValidProperty(queryParameters, input, "Genre");

            // Assert
            Assert.Equal(expectedResult, actualValue);
        }

        [Theory]
        [InlineData(9.9, true)]
        [InlineData(0.1, true)]
        [InlineData(5, true)]
        [InlineData(999, false)]
        [InlineData(10.00001, false)]
        [InlineData(100.1, false)]
        [InlineData(-5, false)]
        public void RatingInput_ValidateModel_ReturnsExpectedResult(double input, bool expectedResult)
        {
            // Arrange
            MovieQueryParameters queryParameters = new MovieQueryParameters();

            // Act
            bool actualValue = IsValidProperty(queryParameters, input, "Rating");

            // Assert
            Assert.Equal(expectedResult, actualValue);
        }

        [Theory]
        [InlineData(1874, true)]
        [InlineData(2025, true)]
        [InlineData(2001, true)]
        [InlineData(1870, false)]
        [InlineData(123, false)]
        public void YearInput_ValidateRegularExpression_ReturnsExpectedResult(int input, bool expectedResult)
        {
            // Arrange
            MovieQueryParameters yearValidation = new MovieQueryParameters();

            // Act
            bool actualResult = IsValidProperty(yearValidation, input, "Year");

            // Assert
            Assert.Equal(expectedResult, actualResult);
        }

        [Theory]
        [InlineData("nm123456789", true)]
        [InlineData("nm12345678", true)]
        [InlineData("nm1234567", true)]
        [InlineData("nm0000000", false)]
        [InlineData("tt0000001", false)]
        [InlineData("nm1234", false)]
        [InlineData("nM12345", false)]
        [InlineData("ab132456", false)]
        [InlineData("123456789", false)]
        [InlineData("12345", false)]
        public void ActorId_ValidateRegularExpression_ReturnsExpectedResult(string input, bool expectedResult)
        {
            // Arrange
            ActorIdParameter actorIdParameter = new ActorIdParameter();

            // Act
            bool isValid = IsValidProperty(actorIdParameter, input, "ActorId");

            // Assert
            Assert.Equal(expectedResult, isValid);
        }

        [Theory]
        [InlineData("nm123456789", true)]
        [InlineData("nm12345678", true)]
        [InlineData("nm1234567", true)]
        [InlineData("nm0000000", false)]
        [InlineData("tt0000001", false)]
        [InlineData("nm1234", false)]
        [InlineData("nM12345", false)]
        [InlineData("ab132456", false)]
        [InlineData("123456789", false)]
        [InlineData("12345", false)]
        public void ActorIdInMovieQueryParameters_ValidateRegularExpression_ReturnsExpectedResult(string input, bool expectedResult)
        {
            // Arrange
            MovieQueryParameters actorIdParameter = new MovieQueryParameters();

            // Act
            bool isValid = IsValidProperty(actorIdParameter, input, "ActorId");

            // Assert
            Assert.Equal(expectedResult, isValid);
        }

        [Theory]
        [InlineData("tt123456789", true)]
        [InlineData("tt12345678", true)]
        [InlineData("tt12345", true)]
        [InlineData("tt0000000", false)]
        [InlineData("nm123456789", false)]
        [InlineData("tt1234", false)]
        [InlineData("tT123456789111", false)]
        [InlineData("ab132456", false)]
        [InlineData("123456789", false)]
        [InlineData("12345", false)]
        public void MovieId_ValidateRegularExpression_ReturnsExpectedResult(string input, bool expectedResult)
        {
            // Arrange
            MovieIdParameter movieIdParameter = new MovieIdParameter();

            // Act
            bool isValid = IsValidProperty(movieIdParameter, input, "MovieId");

            // Assert
            Assert.Equal(expectedResult, isValid);
        }

        [Theory]
        [InlineData("The", true)]
        [InlineData("the matrix", true)]
        [InlineData("2001", true)]
        [InlineData("the quick brown fox jumped over the lazy dog", false)]
        [InlineData("t", false)]
        [InlineData("1", false)]
        public void QueryString_ValidateRegularExpression_ReturnsExpectedResult(string input, bool expectedResult)
        {
            // Arrange
            ActorQueryParameters queryParameter = new ActorQueryParameters();

            // Act
            bool isValid = IsValidProperty(queryParameter, input, "Q");

            // Assert
            Assert.Equal(expectedResult, isValid);
        }

        [Theory]
        [InlineData(0, 0)]
        [InlineData(1, 0)]
        [InlineData(55, 5400)]
        [InlineData(10000, 999900)]
        public void GivenPageNumber_ValidateOffset_ReturnsExpectedResult(int pageNumber, int expectedResult)
        {
            // Arrange
            ActorQueryParameters queryParameters = new ActorQueryParameters { PageNumber = pageNumber };

            // Act
            int actualResult = queryParameters.GetOffset();

            // Assert
            Assert.Equal(expectedResult, actualResult);
        }

        private static bool IsValidProperty(object inputObject, object input, string memberName)
        {
            ValidationContext validationContext = new ValidationContext(inputObject) { MemberName = memberName };
            return Validator.TryValidateProperty(input, validationContext, null);
        }
    }
}
