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
    [Fact]
    public void Match_WhenNoVideo_ShouldReturnEmptyList()
    {
        // Arrange
        List<MediaFile> files =
        [
            Sut.SeedMediaFile(x =>
            {
                x.SetProperty(x => x.FileName, "abc.srt");
                x.SetProperty(x => x.Type, MediaType.Subtitle);
            }),
        ];

        // Act
        var result = Sut.Scoped<IMatcher>().Call(x => x.Match(files));

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void Match_WhenNoSubtitle_ShouldReturnEmptyList()
    {
        // Arrange
        List<MediaFile> files =
        [
            Sut.SeedMediaFile(x =>
            {
                x.SetProperty(x => x.FileName, "abc.mp4");
                x.SetProperty(x => x.Type, MediaType.Video);
            }),
        ];

        // Act
        var result = Sut.Scoped<IMatcher>().Call(x => x.Match(files));

        // Assert
        result.ShouldBeEmpty();
    }

    [Fact]
    public void Match_WhenNothingInCommon_ShouldReturnEmptyList()
    {
        // Arrange
        List<MediaFile> files =
        [
            Sut.SeedMediaFile(x =>
            {
                x.SetProperty(x => x.FileName, "abc.srt");
                x.SetProperty(x => x.Type, MediaType.Subtitle);
            }),
            Sut.SeedMediaFile(x =>
            {
                x.SetProperty(x => x.FileName, "123.mp4");
                x.SetProperty(x => x.Type, MediaType.Video);
            }),
            Sut.SeedMediaFile(x =>
            {
                x.SetProperty(x => x.FileName, "456.mp4");
                x.SetProperty(x => x.Type, MediaType.Video);
            }),
        ];

        // Act
        var result = Sut.Scoped<IMatcher>().Call(x => x.Match(files));

        // Assert
        result.ShouldBeEmpty();
    }

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
        result.ShouldHaveSingleItem().Video.ShouldNotBeNull().FileName.ShouldBe($"{keyword}.mp4");
    }

    [Fact]
    public void Match_WhenSupportedKeywordButNoMatch_ShouldReturnResultWithEmptyVideo()
    {
        // Arrange
        List<MediaFile> files =
        [
            Sut.SeedMediaFile(x =>
            {
                x.SetProperty(x => x.FileName, $"S01E01.srt");
                x.SetProperty(x => x.Type, MediaType.Subtitle);
            }),
            Sut.SeedMediaFile(x =>
            {
                x.SetProperty(x => x.FileName, $"S02E02.mp4");
                x.SetProperty(x => x.Type, MediaType.Video);
            }),
        ];

        // Act
        var result = Sut.Scoped<IMatcher>().Call(x => x.Match(files));

        // Assert
        result.ShouldHaveSingleItem().Video.ShouldBeNull();
    }

    [Fact]
    public void Match_WhenVideosAndSubtitlesHaveTheSameCount_ShouldReturnResultAccordingToOrder()
    {
        // Arrange
        List<MediaFile> files =
        [
            Sut.SeedMediaFile(x =>
            {
                x.SetProperty(x => x.FileName, "01 show.name.srt");
                x.SetProperty(x => x.Type, MediaType.Subtitle);
            }),
            Sut.SeedMediaFile(x =>
            {
                x.SetProperty(x => x.FileName, "02 show.name.srt");
                x.SetProperty(x => x.Type, MediaType.Subtitle);
            }),
            Sut.SeedMediaFile(x =>
            {
                x.SetProperty(x => x.FileName, "show.name.Episode 1.mp4");
                x.SetProperty(x => x.Type, MediaType.Video);
            }),
            Sut.SeedMediaFile(x =>
            {
                x.SetProperty(x => x.FileName, "show.name.Episode 2.mp4");
                x.SetProperty(x => x.Type, MediaType.Video);
            }),
        ];

        // Act
        var result = Sut.Scoped<IMatcher>().Call(x => x.Match(files));

        // Assert
        result.Count.ShouldBe(2);
        result.ForEach(x =>
            x.Video.ShouldNotBeNull()
                .FileName.ShouldBe($"show.name.Episode {result.IndexOf(x) + 1}.mp4")
        );
        result.ForEach(x =>
            x.Subtitle.ShouldNotBeNull()
                .FileName.ShouldBe($"0{result.IndexOf(x) + 1} show.name.srt")
        );
    }
}
