using TopSpeed.Localization;
using Xunit;

namespace TopSpeed.Tests
{
    [Trait("Category", "Behavior")]
    public sealed class LocalizationBehaviorTests
    {
        [Fact]
        public void MarkedTemplate_ShouldFormatWithoutTranslation()
        {
            LocalizationService.Format("Alice says: {0}", "Hello")
                .Should()
                .Be("Alice says: Hello");
        }

        [Fact]
        public void MarkedText_ShouldRoundTripWhenNoCatalogIsLoaded()
        {
            var marked = LocalizationService.Mark("Alice says: {0}");

            LocalizationService.Format(marked, "Hello")
                .Should()
                .Be("Alice says: Hello");
        }
    }
}
