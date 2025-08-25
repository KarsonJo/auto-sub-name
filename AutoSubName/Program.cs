using AutoSubName.Commands;
using AutoSubName.RenameSubs.Setup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;

namespace AutoSubName;

public static class Program
{
    public class CreateAppResult
    {
        public IHost App { get; set; } = null!;
        public LoggingLevelSwitch LoggingSwitch { get; set; } = null!;
    }

    /// <summary>
    /// All core application logic is defined here.
    /// </summary>
    /// <returns></returns>
    public static CreateAppResult CreateApp(Action<HostApplicationBuilder>? configuration = null)
    {
        var builder = Host.CreateApplicationBuilder();
        var services = builder.Services;

        services.AddMediator(o =>
        {
            o.ServiceLifetime = ServiceLifetime.Scoped;
        });

        LoggingLevelSwitch @switch = new();
        services.AddSerilog(x =>
            x.MinimumLevel.ControlledBy(@switch)
                .WriteTo.Console(outputTemplate: "{Message:lj}{NewLine}{Exception}")
        );

        services.AddRenameSubs();

        configuration?.Invoke(builder);

        return new CreateAppResult { App = builder.Build(), LoggingSwitch = @switch };
    }

    /// <summary>
    /// Entry point for the console application.
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    static Task<int> Main(string[] args)
    {
        return AppCommand.Create(CreateApp()).Parse(args).InvokeAsync();
    }
}
