using AutoSubName.Tests.RenameSubs.Utils;
using AutoSubName.Tests.Utils;
using AutoSubName.Tests.Utils.Suts;
using AutoSubName.Tests.Utils.TestApp;
using Mediator;
using Shouldly;
using static System.Guid;

namespace AutoSubName.Tests.RenameSubs.Features;

public static class RenameSubtitlesTests
{
    public class DirectCall : StandaloneSetup<CoreAppSut>
    {
        [Fact]
        public async Task RenameSubtitles_WhenNoSubtitleFiles_ShouldNotTouchFiles()
        {
            // Arrange
            var video = await Sut.CreateVideoFileAsync();

            // Act
            await Sut.Scoped<IMediator>()
                .Call(x => x.Send(Sut.SeedRenameSubtitlesDirectCallCommand()));

            // Assert
            Sut.FileExists(video).ShouldBeTrue();
        }

        [Fact]
        public async Task RenameSubtitles_WhenSubtitleFileDoesNotHaveEpisodeNumber_ShouldNotTouchFiles()
        {
            // Arrange
            var video = await Sut.CreateVideoFileAsync();
            var subtitle = await Sut.CreateSubtitleFileAsync();

            // Act
            await Sut.Scoped<IMediator>()
                .Call(x => x.Send(Sut.SeedRenameSubtitlesDirectCallCommand()));

            // Assert
            Sut.FileExists(video).ShouldBeTrue();
            Sut.FileExists(subtitle).ShouldBeTrue();
        }

        [Fact]
        public async Task RenameSubtitles_WhenSubtitleFileDoesNotHaveMatchingEpisodeNumber_ShouldNotTouchFiles()
        {
            // Arrange
            var videoName = $"{NewGuid()} S01E01";
            var video = await Sut.CreateVideoFileAsync(fileName: videoName);
            var subtitle = await Sut.CreateSubtitleFileAsync(fileName: $"{NewGuid()} S01E02");

            // Act
            await Sut.Scoped<IMediator>()
                .Call(x => x.Send(Sut.SeedRenameSubtitlesDirectCallCommand()));

            // Assert
            Sut.FileExists(video).ShouldBeTrue();
            Sut.FileExists(subtitle).ShouldBeTrue();
        }

        [Fact]
        public async Task RenameSubtitles_WhenSubtitleFileHasMatchingEpisodeNumber_ShouldRenameFiles()
        {
            // Arrange
            var episode = "S01E01";
            var videoName = $"{NewGuid()} {episode}";
            var video = await Sut.CreateVideoFileAsync(fileName: videoName);
            var subtitle = await Sut.CreateSubtitleFileAsync(fileName: $"{NewGuid()} {episode}");

            // Act
            await Sut.Scoped<IMediator>()
                .Call(x => x.Send(Sut.SeedRenameSubtitlesDirectCallCommand()));

            // Assert
            Sut.FileExists(video).ShouldBeTrue();
            Sut.FileExists(subtitle).ShouldBeFalse();
            Sut.FileExists($"{videoName}.srt").ShouldBeTrue();
        }

        [Fact]
        public async Task RenameSubtitles_WhenHaveMultipleSubtitleFiles_ShouldBulkRenameFiles()
        {
            // Arrange
            var showName = $"{NewGuid()}";
            var episode1 = "S01E01";
            var episode2 = "S01E02";
            var video1 = await Sut.CreateVideoFileAsync(fileName: $"{showName} {episode1}");
            var video2 = await Sut.CreateVideoFileAsync(fileName: $"{showName} {episode2}");
            var subtitle1 = await Sut.CreateSubtitleFileAsync(fileName: $"{NewGuid()} {episode1}");
            var subtitle2 = await Sut.CreateSubtitleFileAsync(fileName: $"{NewGuid()} {episode2}");

            // Act
            await Sut.Scoped<IMediator>()
                .Call(x => x.Send(Sut.SeedRenameSubtitlesDirectCallCommand()));

            // Assert
            Sut.FileExists($"{showName} {episode1}.srt").ShouldBeTrue();
            Sut.FileExists($"{showName} {episode2}.srt").ShouldBeTrue();
        }
    }
}
