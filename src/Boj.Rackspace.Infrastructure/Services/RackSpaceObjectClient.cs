using Boj.RackSpace.Application.Interfaces;
using Boj.RackSpace.Domain.Models;

namespace Boj.Rackspace.Infrastructure.Services
{
    public class RackSpaceObjectClient : IRackSpaceObjectClient
    {
        public Task DeleteAsync(string container, string objectName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<Stream> DownloadAsync(string container, string objectName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<RackSpaceObject>> GetObjectsAsync(string container, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task UploadAsync(string container, string objectName, Stream stream, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
