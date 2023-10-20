using emailBackup.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace emailBackup
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(
                    "appsettings.json",
                    optional: true,
                    reloadOnChange: true
                )
                .AddUserSecrets<Program>()
                .AddCommandLine(args)
                .AddEnvironmentVariables("EB_")
                .Build();

            var serviceProvider = new ServiceCollection()
                .AddSingleton<IConfigurationRoot>(config)
                .AddTransient<App, App>()
                .BuildServiceProvider();

            var app = serviceProvider.GetService<App>();
            if (app != null)
            {
                await app.Run();
            }
        }
    }
}