using AutoSubName.Tests.Utils.TestApp;
using AutoSubName.Tests.Utils.TestApp.Resources;
using Microsoft.Extensions.DependencyInjection;
using static AutoSubName.Program;

namespace AutoSubName.Tests.Utils.Suts;

public interface ISut
{
    CreateAppResult CreateAppResult { get; }
    string RootFileDirectory { get; }
    public IServiceProvider Services { get; }
}

public class CoreAppSut(TestResourceManager manager) : ISut, IAsyncLifetime
{
    public CreateAppResult CreateAppResult { get; private set; } = null!;
    public IServiceProvider Services { get; private set; } = null!;
    public string RootFileDirectory { get; private set; } = null!;

    protected virtual void ConfigureServices(IServiceCollection s) { }

    public virtual async ValueTask InitializeAsync()
    {
        // Services
        CreateAppResult = CreateApp(builder =>
        {
            ConfigureServices(builder.Services);
        });

        Services = CreateAppResult.App.Services;

        // Root Directory
        RootFileDirectory = Path.Combine(
            (await manager.GetResource<TempDirectoryResource>()).DirectoryPath,
            Guid.NewGuid().ToString()
        );
        Directory.CreateDirectory(RootFileDirectory);
    }

    public virtual async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        var host = CreateAppResult.App;
        if (host is IAsyncDisposable asyncHost)
            await asyncHost.DisposeAsync();
        else
            host.Dispose();
    }
}
