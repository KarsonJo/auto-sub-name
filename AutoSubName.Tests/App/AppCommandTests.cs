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
        result.ExitCode.ShouldBe(0);
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
        result.ExitCode.ShouldBe(0);
        result.Error.ShouldBeEmpty();

        // Assert
        Sut.FileExists(video).ShouldBeTrue();
        Sut.FileExists(subtitle).ShouldBeTrue();
    }
}

public static class AppCommandTestExtensions
{
    public record Result(int ExitCode, string Output, string Error);

    public static async Task<Result> ExecuteAppCommandAsync(this ISut sut, params string[] args)
    {
        var output = new StringWriter();
        var error = new StringWriter();
        var config = new InvocationConfiguration { Output = output, Error = error };

        var result = await AppCommand.Create(sut.Services).Parse(args).InvokeAsync(config);

        return new Result(result, output.ToString(), error.ToString());
    }
}
