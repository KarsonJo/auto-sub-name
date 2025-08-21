using AutoSubName.Tests.Utils.TestApp;
using AutoSubName.Tests.Utils.TestApp.Resources;
using Microsoft.Extensions.DependencyInjection;

namespace AutoSubName.Tests.Utils.Suts;

public interface ISut
{
    string RootFileDirectory { get; }
    public ServiceProvider Services { get; }
}

public class CoreAppSut(TestResourceManager manager) : ISut, IAsyncLifetime
{
    public ServiceProvider Services { get; private set; } = null!;

    public string RootFileDirectory { get; private set; } = null!;

    protected virtual void ConfigureServices(IServiceCollection s) { }

    public async ValueTask InitializeAsync()
    {
        // Services
        var services = Program.CreateAppService();

        ConfigureServices(services);

        Services = services.BuildServiceProvider();

        // Root Directory
        RootFileDirectory = Path.Combine(
            (await manager.GetResource<TempDirectoryResource>()).DirectoryPath,
            Guid.NewGuid().ToString()
        );
        Directory.CreateDirectory(RootFileDirectory);
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        await Services.DisposeAsync();
    }
}
