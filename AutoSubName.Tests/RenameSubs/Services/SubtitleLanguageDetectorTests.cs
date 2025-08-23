using AutoSubName.RenameSubs.Services;
using AutoSubName.Tests.Utils;
using AutoSubName.Tests.Utils.Suts;
using AutoSubName.Tests.Utils.TestApp;
using Shouldly;

namespace AutoSubName.Tests.RenameSubs.Services;

public class SubtitleLanguageDetectorTests : ClassFixtureSetup<CoreAppSut>
{
    [Theory]
    [InlineData("file-name.en-us.srt")]
    [InlineData("file.name.en-us.srt")]
    [InlineData("file name.en-us.srt")]
    [InlineData("file.name.en-us.sth.srt")]
    [InlineData("file.name.en-us.zh-cn.srt")]
    [InlineData("file.name.en-us&zh-cn.srt")]
    [InlineData("file.name.en-US.srt")]
    [InlineData("1ca1.en-US.srt")] // fake positive of CA.
    [InlineData("精校en-US字幕.srt")] // non-ASCII.
    [InlineData("16F12CF2-5739-42CA-B2DB-F45D2211CC4D.en-US.srt")]
    public void DetectLanguage_WhenSupportedLanguage_ShouldReturnLanguage(string name)
    {
        // Arrange
        var path = Path.Combine(Sut.RootFileDirectory, name);

        // Act
        var result = Sut.Scoped<ISubtitleLanguageDetector>().Call(x => x.GetLanguage(path));

        // Assert
        result.ShouldNotBeNull().Name.ShouldBe("en", StringCompareShould.IgnoreCase);
    }

    [Theory]
    [InlineData("file-name.srt")]
    [InlineData("file-name.english.srt")]
    [InlineData("between-us.srt")]
    public void DetectLanguage_WhenNoSupportedLanguage_ShouldReturnNull(string name)
    {
        // Arrange
        var path = Path.Combine(Sut.RootFileDirectory, name);

        // Act
        var result = Sut.Scoped<ISubtitleLanguageDetector>().Call(x => x.GetLanguage(path));

        // Assert
        result.ShouldBeNull();
    }

    [Theory]
    [InlineData("zh-Hans", "zh")]
    [InlineData("zh-Hans", "zh-Hans")]
    [InlineData("zh-Hans", "zh-Hans-CN")]
    [InlineData("zh-Hans", "zh-CN")]
    [InlineData("zh-Hans", "CHS")]
    [InlineData("zh-Hant", "zh-Hant")]
    [InlineData("zh-Hant", "zh-hk")]
    [InlineData("zh-Hant", "zh-HK")]
    [InlineData("zh-Hant", "zh-TW")]
    [InlineData("zh-Hant", "zh-Hant-HK")]
    [InlineData("zh-Hant", "zh-Hant-TW")]
    [InlineData("en", "en-US")]
    [InlineData("en", "en-US-POSIX")]
    [InlineData("en", "en-GB")]
    [InlineData("en", "ENU")]
    [InlineData("en", "ENG")]
    public void DetectLanguage_WhenWithAlias_ShouldReturnCorrectly(string expected, string alias)
    {
        // Arrange
        var path = Path.Combine(Sut.RootFileDirectory, $"file-name.{alias}.srt");

        // Act
        var result = Sut.Scoped<ISubtitleLanguageDetector>().Call(x => x.GetLanguage(path));

        // Assert
        result.ShouldNotBeNull().Name.ShouldBe(expected, StringCompareShould.IgnoreCase);
    }
}
