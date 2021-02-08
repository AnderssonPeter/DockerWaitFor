using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DockerMountDelay
{
    class Program
    {
        public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.Configure<ContainerClientOptions>((o) =>
                    {
                        o.Endpoint = Environment.GetEnvironmentVariable("DOCKER_ENDPOINT") ?? "unix:///var/run/docker.sock";
                    });
                    services.Configure<LinuxMountParserOptions>((o) =>
                    {
                        o.Path = Environment.GetEnvironmentVariable("MOUNT_PATH") ?? "/app/proc/mounts";
                    });
                    services.Configure<WorkerOptions>((o) =>
                    {
                        var scanInterval = int.Parse(Environment.GetEnvironmentVariable("SCAN_INTERVAL") ?? "30");
                        o.ScanInterval = TimeSpan.FromSeconds(scanInterval);
                    });
                    services.AddSingleton<ContainerClient>();
                    services.AddSingleton<LinuxMountParser>();
                    services.AddHostedService<Worker>();
                });
    }
}
