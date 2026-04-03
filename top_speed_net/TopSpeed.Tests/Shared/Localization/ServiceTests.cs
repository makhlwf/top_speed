using TopSpeed.Localization;
using Xunit;

namespace TopSpeed.Tests.Localization
{
    [Trait("Category", "SharedLocalization")]
    public sealed class LocalizationServiceTests
    {
        [Fact]
        public void Format_Uses_All_Arguments()
        {
            var text = LocalizationService.Format(
                LocalizationService.Mark("{0} says: {1}"),
                "Alice",
                "Hello");

            Assert.Equal("Alice says: Hello", text);
        }

        [Fact]
        public void FormatWithContext_Formats_Template()
        {
            var text = LocalizationService.FormatWithContext(
                "chat",
                LocalizationService.Mark("{0} says: {1}"),
                "Alice",
                "Hello");

            Assert.Equal("Alice says: Hello", text);
        }
    }
}



