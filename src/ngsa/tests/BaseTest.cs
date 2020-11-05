using System;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Moq;
using Xunit;

namespace tests
{
    public class BaseTest
    {
        protected static void ConstructorMustThrowArgumentNullException(Type type)
        {
            Assert.NotNull(type);

            foreach (ConstructorInfo constructor in type.GetConstructors())
            {
                ParameterInfo[] parameters = constructor.GetParameters();
                Mock[] mocks = parameters.Select(p =>
                {
                    Type mockType = typeof(Mock<>).MakeGenericType(p.ParameterType);
                    return (Mock)Activator.CreateInstance(mockType);
                }).ToArray();

                for (int index = 0; index < parameters.Length; index++)
                {
                    object[] mocksCopy = mocks.Select(m => m.Object).ToArray();
                    mocksCopy[index] = null;

                    string message = parameters[index].Name;
                    try
                    {
                        Assert.Throws<ArgumentNullException>(() =>
                        {
                            constructor.Invoke(mocksCopy);
                        });
                    }
                    catch (TargetInvocationException targetInvocationException)
                    {
                        targetInvocationException.InnerException.Should().BeOfType<ArgumentNullException>();
                        targetInvocationException.InnerException.Message.Should().Contain(message);
                    }
                }
            }
        }
    }
}
