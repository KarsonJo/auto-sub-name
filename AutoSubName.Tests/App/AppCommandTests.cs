using System.CommandLine;
using AutoSubName.Commands;
using AutoSubName.Tests.RenameSubs.Utils;
using AutoSubName.Tests.Utils.Suts;
using AutoSubName.Tests.Utils.TestApp;
using Shouldly;
using static System.Guid;

namespace AutoSubName.Tests.App;

public class AppCommandTests : StandaloneSetup<CoreAppSut>
{
    [Fact]
    public async Task InvokeCommand_WhenInvalidDirectory_ShouldExitWithError()
    {
        // Act
        var result = await Sut.ExecuteAppCommandAsync("--dir");

        // Assert
        result.ExitCode.ShouldBe(1);
        result.Error.Trim().ShouldBe("Required argument missing for option: '--dir'.");
    }

    [Fact]
    public async Task InvokeCommand_WhenValidDirectory_ShouldRenameSubtitles()
    {
        // Arrange
        var episode = "S01E01";
        var videoName = $"{NewGuid()} {episode}";
        var video = await Sut.CreateVideoFileAsync(fileName: videoName);
        var subtitle = await Sut.CreateSubtitleFileAsync(fileName: $"{NewGuid()} {episode}");

        // Act
        var result = await Sut.ExecuteAppCommandAsync("--dir", Sut.RootFileDirectory);

        // Assert
        result.ExitCode.ShouldBe(0, result.Error);
        result.Error.ShouldBeEmpty();

        Sut.FileExists(video).ShouldBeTrue();
        Sut.FileExists(subtitle).ShouldBeFalse();
        Sut.FileExists($"{videoName}.srt").ShouldBeTrue();
    }

    [Fact]
    public async Task InvokeCommand_WhenExecuteInSubDirectory_ShouldNotRenameSubtitlesOutsideDirectory()
    {
        // Arrange
        var episode = "S01E01";
        var videoName = $"{NewGuid()} {episode}";
        var video = await Sut.CreateVideoFileAsync(fileName: videoName);
        var subtitle = await Sut.CreateSubtitleFileAsync(fileName: $"{NewGuid()} {episode}");

        var directory = Path.Combine(Sut.RootFileDirectory, "sub");
        Directory.CreateDirectory(directory);

        // Act
        var result = await Sut.ExecuteAppCommandAsync("--dir", directory);

        // Assert
        result.ExitCode.ShouldBe(0, result.Error);
        result.Error.ShouldBeEmpty();

        Sut.FileExists(video).ShouldBeTrue();
        Sut.FileExists(subtitle).ShouldBeTrue();
    }

    [Fact]
    public async Task InvokeCommand_WhenEnableRecursive_ShouldRenameSubtitlesInSubDirectories()
    {
        // Arrange
        var subPath = Path.Combine(Sut.RootFileDirectory, "sub");
        Directory.CreateDirectory(subPath);

        // Arrange
        var episode = "S01E01";
        var videoName = $"{NewGuid()} {episode}";
        var video = await Sut.CreateVideoFileAsync(fileName: videoName, basePath: subPath);
        var subtitle = await Sut.CreateSubtitleFileAsync(
            fileName: $"{NewGuid()} {episode}",
            basePath: subPath
        );

        // Act
        List<string> args = ["--dir", Sut.RootFileDirectory, "--recursive"];
        var result = await Sut.ExecuteAppCommandAsync([.. args]);

        // Assert
        result.ExitCode.ShouldBe(0, result.Error);
        result.Error.ShouldBeEmpty();

        Sut.FileExists(video, basePath: subPath).ShouldBeTrue();
        Sut.FileExists(subtitle, basePath: subPath).ShouldBeFalse();
        Sut.FileExists($"{videoName}.srt", basePath: subPath).ShouldBeTrue();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task InvokeCommand_WhenDisableRecursive_ShouldNotRenameSubtitlesInSubDirectories(
        bool useDefault
    )
    {
        // Arrange
        var subPath = Path.Combine(Sut.RootFileDirectory, "sub");
        Directory.CreateDirectory(subPath);

        var episode = "S01E01";
        var videoName = $"{NewGuid()} {episode}";
        var video = await Sut.CreateVideoFileAsync(fileName: videoName, basePath: subPath);
        var subtitle = await Sut.CreateSubtitleFileAsync(
            fileName: $"{NewGuid()} {episode}",
            basePath: subPath
        );

        // Act
        List<string> args = ["--dir", Sut.RootFileDirectory];
        if (!useDefault)
        {
            args.AddRange(["--recursive", "false"]);
        }
        var result = await Sut.ExecuteAppCommandAsync(args);

        // Assert
        result.ExitCode.ShouldBe(0, result.Error);
        result.Error.ShouldBeEmpty();

        Sut.FileExists(video, basePath: subPath).ShouldBeTrue();
        Sut.FileExists(subtitle, basePath: subPath).ShouldBeTrue();
    }

    [Fact]
    public async Task InvokeCommand_WhenUseCustomNamingPattern_ShouldRenameToCustomFormat()
    {
        // Arrange
        var episode = "S01E01";
        var videoName = $"{NewGuid()} {episode}";
        var video = await Sut.CreateVideoFileAsync(fileName: videoName);
        var subtitle = await Sut.CreateSubtitleFileAsync(fileName: $"{NewGuid()} {episode}");

        // Act
        List<string> args = ["--dir", Sut.RootFileDirectory, "--pattern", "{name}.custom.{ext}"];
        var result = await Sut.ExecuteAppCommandAsync(args);

        // Assert
        result.ExitCode.ShouldBe(0, result.Error);
        result.Error.ShouldBeEmpty();

        Sut.FileExists(video).ShouldBeTrue();
        Sut.FileExists(subtitle).ShouldBeFalse();
        Sut.FileExists($"{videoName}.custom.srt").ShouldBeTrue();
    }

    [Fact]
    public async Task InvokeCommand_WhenSetLanguageFormat_ShouldRenameToLanguageFormat()
    {
        // Arrange
        var episode = "S01E01";
        var videoName = $"{NewGuid()} {episode}";
        var video = await Sut.CreateVideoFileAsync(fileName: videoName);
        var subtitle = await Sut.CreateSubtitleFileAsync(
            fileName: $"zh-Hans.{NewGuid()}.{episode}"
        );

        // Act
        List<string> args = ["--dir", Sut.RootFileDirectory, "--language-format", "TwoLetter"];
        var result = await Sut.ExecuteAppCommandAsync(args);

        // Assert
        result.ExitCode.ShouldBe(0, result.Error);
        result.Error.ShouldBeEmpty();

        Sut.FileExists(video).ShouldBeTrue();
        Sut.FileExists(subtitle).ShouldBeFalse();
        Sut.FileExists($"{videoName}.zh.srt").ShouldBeTrue();
    }
}

public static class AppCommandTestExtensions
{
    public record Result(int ExitCode, string Output, string Error);

    public static Task<Result> ExecuteAppCommandAsync(this ISut sut, params string[] args)
    {
        return sut.ExecuteAppCommandAsync((IReadOnlyList<string>)args);
    }

    public static async Task<Result> ExecuteAppCommandAsync(
        this ISut sut,
        IReadOnlyList<string> args
    )
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var config = new InvocationConfiguration { Output = output, Error = error };

        var result = await AppCommand.Create(sut.Services).Parse(args).InvokeAsync(config);

        return new Result(result, output.ToString(), error.ToString());
    }
}
