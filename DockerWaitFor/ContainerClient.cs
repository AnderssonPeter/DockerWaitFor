using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DockerMountDelay
{
    public class ContainerClient : IDisposable
    {
        private readonly DockerClient client;
        private readonly ILogger<ContainerClient> logger;
        private readonly ILogger<Container> containerLogger;

        public ContainerClient(IOptions<ContainerClientOptions> options, ILogger<ContainerClient> logger, ILogger<Container> containerLogger)
        {
            client = new DockerClientConfiguration(new Uri(options.Value.Endpoint)).CreateClient();
            this.logger = logger;
            this.containerLogger = containerLogger;
        }

        public async Task<IEnumerable<Container>> GetContainersAsync(CancellationToken cancellationToken = default) =>
            (await client.Containers.ListContainersAsync(new ContainersListParameters() { 
                All = true, 
                Filters = new Dictionary<string, IDictionary<string, bool>> 
                {
                    {
                        "label", new Dictionary<string, bool>
                        {
                            { Constants.LabelPrefix + Constants.Enabled + "=true", true }
                        }
                    }
                }
            }, cancellationToken)).Select(c => new Container(containerLogger, c, client)).ToList();

        public void Dispose()
        {
            client.Dispose();
        }
    }
}
