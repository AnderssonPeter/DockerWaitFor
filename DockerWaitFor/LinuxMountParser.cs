using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace DockerMountDelay
{

    public class LinuxMountParser
    {
        private readonly string path;

        public LinuxMountParser(IOptions<LinuxMountParserOptions> options)
        {
            this.path = options.Value.Path;
        }

        public async Task<IEnumerable<Mount>> GetMountsAsync(CancellationToken cancellationToken = default) => (await File.ReadAllLinesAsync(path, cancellationToken)).Select(line => line.Split(' ')).Select(fields => new Mount(fields[0], fields[1], fields[3]));
    }
}
