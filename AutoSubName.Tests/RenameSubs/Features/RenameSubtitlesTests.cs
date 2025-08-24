using AutoSubName.RenameSubs.Services;
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

        [Fact]
        public async Task RenameSubtitles_WhenSubtitleFileHasLanguageTag_ShouldRenameFilesWithLanguageTag()
        {
            // Arrange
            var episode = "S01E01";
            var videoName = $"{NewGuid()} {episode}";
            var video = await Sut.CreateVideoFileAsync(fileName: videoName);
            var subtitle = await Sut.CreateSubtitleFileAsync(
                fileName: $"zh-Hans.{NewGuid()}.{episode}"
            );

            // Act
            await Sut.Scoped<IMediator>()
                .Call(x => x.Send(Sut.SeedRenameSubtitlesDirectCallCommand()));

            // Assert
            Sut.FileExists(video).ShouldBeTrue();
            Sut.FileExists(subtitle).ShouldBeFalse();
            Sut.FileExists($"{videoName}.zh-Hans.srt").ShouldBeTrue();
        }

        [Fact]
        public async Task RenameSubtitles_WhenEnableRecursive_ShouldRenameFilesRecursively()
        {
            // Arrange
            var subPath = Path.Combine(Sut.RootFileDirectory, "sub");
            Directory.CreateDirectory(subPath);

            var showName = $"{NewGuid()}";
            var episode1 = "S01E01";
            var episode2 = "S01E02";
            var video1 = await Sut.CreateVideoFileAsync(
                fileName: $"{showName} {episode1}",
                basePath: Sut.RootFileDirectory
            );
            var video2 = await Sut.CreateVideoFileAsync(
                fileName: $"{showName} {episode2}",
                basePath: subPath
            );
            var subtitle1 = await Sut.CreateSubtitleFileAsync(
                fileName: $"{NewGuid()} {episode1}",
                basePath: Sut.RootFileDirectory
            );
            var subtitle2 = await Sut.CreateSubtitleFileAsync(
                fileName: $"{NewGuid()} {episode2}",
                basePath: subPath
            );

            var command = Sut.SeedRenameSubtitlesDirectCallCommand(x =>
            {
                x.FolderPath = Sut.RootFileDirectory;
                x.Recursive = true;
            });

            // Act
            await Sut.Scoped<IMediator>().Call(x => x.Send(command));

            // Assert
            Sut.FileExists($"{showName} {episode1}.srt", basePath: Sut.RootFileDirectory)
                .ShouldBeTrue();
            Sut.FileExists($"{showName} {episode2}.srt", basePath: subPath).ShouldBeTrue();
        }

        [Fact]
        public async Task RenameSubtitles_WhenDisableRecursive_ShouldNotTouchParentFiles()
        {
            // Arrange
            var subPath = Path.Combine(Sut.RootFileDirectory, "sub");
            Directory.CreateDirectory(subPath);

            var showName = $"{NewGuid()}";
            var episode1 = "S01E01";
            var episode2 = "S01E02";
            var video1 = await Sut.CreateVideoFileAsync(
                fileName: $"{showName} {episode1}",
                basePath: Sut.RootFileDirectory
            );
            var video2 = await Sut.CreateVideoFileAsync(
                fileName: $"{showName} {episode2}",
                basePath: subPath
            );
            var subtitle1 = await Sut.CreateSubtitleFileAsync(
                fileName: $"{NewGuid()} {episode1}",
                basePath: Sut.RootFileDirectory
            );
            var subtitle2 = await Sut.CreateSubtitleFileAsync(
                fileName: $"{NewGuid()} {episode2}",
                basePath: subPath
            );

            var command = Sut.SeedRenameSubtitlesDirectCallCommand(x =>
            {
                x.FolderPath = subPath;
                x.Recursive = false;
            });

            // Act
            await Sut.Scoped<IMediator>().Call(x => x.Send(command));

            // Assert
            Sut.FileExists($"{showName} {episode1}.srt", basePath: Sut.RootFileDirectory)
                .ShouldBeFalse();
            Sut.FileExists($"{showName} {episode2}.srt", basePath: subPath).ShouldBeTrue();
        }

        [Fact]
        public async Task RenameSubtitles_WhenUseCustomNamingPattern_ShouldRenameFilesWithCustomNamingPattern()
        {
            // Arrange
            var episode = "S01E01";
            var videoName = $"{NewGuid()} {episode}";
            var video = await Sut.CreateVideoFileAsync(fileName: videoName);
            var subtitle = await Sut.CreateSubtitleFileAsync(fileName: $"{NewGuid()} {episode}");

            var command = Sut.SeedRenameSubtitlesDirectCallCommand(x =>
            {
                x.CustomNamingPattern = "{name}.custom.{ext}";
            });

            // Act
            await Sut.Scoped<IMediator>().Call(x => x.Send(command));

            // Assert
            Sut.FileExists(video).ShouldBeTrue();
            Sut.FileExists(subtitle).ShouldBeFalse();
            Sut.FileExists($"{videoName}.custom.srt").ShouldBeTrue();
        }

        [Fact]
        public async Task RenameSubtitles_WhenCustomNamingPatternDoesNotContainLanguageVariable_ShouldIgnoreLanguage()
        {
            // Arrange
            var episode = "S01E01";
            var videoName = $"{NewGuid()} {episode}";
            var video = await Sut.CreateVideoFileAsync(fileName: videoName);
            var subtitle = await Sut.CreateSubtitleFileAsync(
                fileName: $"zh-Hans.{NewGuid()}.{episode}"
            );

            var command = Sut.SeedRenameSubtitlesDirectCallCommand(x =>
            {
                x.CustomNamingPattern = "{name}.{ext}";
            });

            // Act
            await Sut.Scoped<IMediator>().Call(x => x.Send(command));

            // Assert
            Sut.FileExists(video).ShouldBeTrue();
            Sut.FileExists(subtitle).ShouldBeFalse();
            Sut.FileExists($"{videoName}.srt").ShouldBeTrue();
        }

        [Theory]
        [InlineData(ISubtitleRenamer.LanguageFormat.TwoLetter, "zh")]
        [InlineData(ISubtitleRenamer.LanguageFormat.ThreeLetter, "zho")]
        [InlineData(ISubtitleRenamer.LanguageFormat.Ietf, "zh-Hans")]
        [InlineData(ISubtitleRenamer.LanguageFormat.English, "Chinese (Simplified)")]
        [InlineData(ISubtitleRenamer.LanguageFormat.Native, "中文（简体）")]
        public async Task RenameSubtitles_WhenSetLanguageFormat_ShouldRenameFilesWithLanguageFormat(
            ISubtitleRenamer.LanguageFormat format,
            string output
        )
        {
            // Arrange
            var episode = "S01E01";
            var videoName = $"{NewGuid()} {episode}";
            var video = await Sut.CreateVideoFileAsync(fileName: videoName);
            var subtitle = await Sut.CreateSubtitleFileAsync(
                fileName: $"zh-Hans.{NewGuid()}.{episode}"
            );

            var command = Sut.SeedRenameSubtitlesDirectCallCommand(x =>
            {
                x.LanguageFormat = format;
            });

            // Act
            await Sut.Scoped<IMediator>().Call(x => x.Send(command));

            // Assert
            Sut.FileExists(video).ShouldBeTrue();
            Sut.FileExists(subtitle).ShouldBeFalse();
            Sut.FileExists($"{videoName}.{output}.srt").ShouldBeTrue();
        }
    }
}
