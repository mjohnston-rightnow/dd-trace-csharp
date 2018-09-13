using Datadog.Trace.ExtensionMethods;
using Xunit;

namespace Datadog.Trace.Tests.ExtensionMethods
{
    public class StringExtensionsTests
    {
        [Theory]
        [InlineData("", "")]
        [InlineData("abc", "Abc")]
        [InlineData("ABC", "Abc")]
        [InlineData("Abc", "Abc")]
        [InlineData("aBC", "Abc")]
        public void ToSentenceCaseInvariant(string value, string expected)
        {
            var result = value.ToSentenceCaseInvariant();
            Assert.Equal(expected, result);
        }
    }
}
