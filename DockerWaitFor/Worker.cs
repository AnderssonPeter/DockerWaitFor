using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DockerMountDelay
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> logger;
        private readonly LinuxMountParser linuxMountParser;
        private readonly ContainerClient containerClient;
        private readonly TimeSpan scanInterval;

        public Worker(ILogger<Worker> logger, IOptions<WorkerOptions> options, LinuxMountParser linuxMountParser, ContainerClient containerClient)
        {
            this.logger = logger;
            scanInterval = options.Value.ScanInterval;
            this.linuxMountParser = linuxMountParser;
            this.containerClient = containerClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                logger.LogDebug("Sleeping for {Time} seconds", scanInterval);
                await Task.Delay(scanInterval, stoppingToken);
                logger.LogDebug("Reading docker mounts");
                var mounts = (await linuxMountParser.GetMountsAsync(stoppingToken)).Select(mount => mount.MountPoint);

                var containers = await containerClient.GetContainersAsync(stoppingToken);
                foreach(var container in containers)
                {
                    logger.LogInformation("Checking container {ID} - {Name}", container.Id, container.Name);
                    

                    if (container.State == ContainerState.Restarting)
                    {
                        logger.LogWarning("Container {ID} - {Name} is restarting, aborting", container.Id, container.Name);
                        continue;
                    }
                    else if (container.State == ContainerState.Paused)
                    {
                        logger.LogWarning("Container {ID} - {Name} is paused, aborting", container.Id, container.Name);
                        continue;
                    }

                    var missingMounts = container.GetMissingMounts(mounts);
                    var requiredMountsMounted = !missingMounts.Any();
                    if (!requiredMountsMounted)
                    {
                        logger.LogWarning("Container {ID} - {Name} is missing {Mounts}", container.Id, container.Name, missingMounts);
                    }
                    if (container.State != ContainerState.Running && requiredMountsMounted && container.StartOnAvailableMounts)
                    {
                        await container.StartAsync(stoppingToken);
                    }
                    else if (container.State == ContainerState.Running && !requiredMountsMounted && container.StopOnMissingMounts)
                    {
                        await container.StopAsync(stoppingToken);
                    }
                }
            }
        }
    }
}
