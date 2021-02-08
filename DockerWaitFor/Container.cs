using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;

namespace DockerMountDelay
{
    public class Container
    {
        private readonly ILogger<Container> logger;
        private readonly DockerClient client;
        private readonly ContainerListResponse container;

        public string Id => container.ID;
        public string Name => container.Names.First()?.TrimStart('/');
        public ContainerState State { get; private set; }
        
        public bool StartOnAvailableMounts { get; private set; }
        public bool StopOnMissingMounts { get; private set; }
        public IEnumerable<string> RequiredMounts { get; private set; }

        public Container(ILogger<Container> logger, ContainerListResponse container, DockerClient client)
        {
            this.logger = logger;
            this.client = client;
            this.container = container;
            State = Enum.Parse<ContainerState>(container.State, true);
            var labels = container.Labels.Where(label => label.Key.StartsWith(Constants.LabelPrefix))
                                         .Select(label => KeyValuePair.Create(label.Key.Substring(Constants.LabelPrefix.Length), label.Value));

            StopOnMissingMounts = bool.Parse(labels.Where(label => label.Key == Constants.StopOnMissing).Select(label => label.Value).SingleOrDefault() ?? "true");
            StartOnAvailableMounts = bool.Parse(labels.Where(label => label.Key == Constants.StartOnAvailable).Select(label => label.Value).SingleOrDefault() ?? "true");
            RequiredMounts = labels.Where(label => label.Key.StartsWith(Constants.Mount))
                                   .Select(label => label.Value)
                                   .ToList();
        }

        public IEnumerable<string> GetMissingMounts(IEnumerable<string> currentMounts) => 
            RequiredMounts.Where(requiredMount => !currentMounts.Any(mount => mount.Equals(requiredMount) || mount.StartsWith(requiredMount)));

        public async Task<bool> StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Stopping container {ID}", Id);
            var result = await client.Containers.StartContainerAsync(Id, new ContainerStartParameters(), cancellationToken);
            if (result)
            {
                State = ContainerState.Running;
            }
            else
            {
                logger.LogError("Failed to stop container {ID}", Id);
            }
            return result;
        }

        public async Task<bool> StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Stopping container {ID}", Id);
            var result = await client.Containers.StopContainerAsync(Id, new ContainerStopParameters(), cancellationToken);
            if (result)
            {
                State = ContainerState.Exited;
            }
            else
            {
                logger.LogError("Failed to stop container {ID}", Id);
            }
            return result;
        }
    }
}
