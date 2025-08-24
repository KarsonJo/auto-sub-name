using AutoSubName.RenameSubs.Entities;
using AutoSubName.RenameSubs.Services;
using AutoSubName.Tests.RenameSubs.Utils;
using AutoSubName.Tests.Utils;
using AutoSubName.Tests.Utils.Suts;
using AutoSubName.Tests.Utils.TestApp;
using Shouldly;

namespace AutoSubName.Tests.RenameSubs.Services;

public class MatcherTests : ClassFixtureSetup<CoreAppSut>
{
    [Theory]
    // S01E01
    [InlineData("S01E01")]
    [InlineData("S1E1")]
    [InlineData("S01E9999")]
    // AAA-001
    [InlineData("ABC-001")]
    [InlineData("ABC-0001")]
    [InlineData("ABCD-001")]
    [InlineData("AB-001")]
    [InlineData("ABCDE-9999")]
    public void Match_WhenSupportedKeyword_ShouldReturnResult(string keyword)
    {
        // Arrange
        List<MediaFile> files =
        [
            Sut.SeedMediaFile(x =>
            {
                x.SetProperty(x => x.FileName, $"{keyword}.srt");
                x.SetProperty(x => x.Type, MediaType.Subtitle);
            }),
            Sut.SeedMediaFile(x =>
            {
                x.SetProperty(x => x.FileName, $"{keyword}.mp4");
                x.SetProperty(x => x.Type, MediaType.Video);
            }),
            Sut.SeedMediaFile(x =>
            {
                x.SetProperty(x => x.FileName, $"not-related.mp4");
                x.SetProperty(x => x.Type, MediaType.Video);
            }),
        ];

        // Act
        var result = Sut.Scoped<IMatcher>().Call(x => x.Match(files));

        // Assert
        result.ShouldHaveSingleItem().Video.FileName.ShouldBe($"{keyword}.mp4");
    }
}
