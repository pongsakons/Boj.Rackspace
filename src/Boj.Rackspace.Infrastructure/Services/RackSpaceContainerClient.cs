using Boj.RackSpace.Application.Interfaces;

namespace Boj.Rackspace.Infrastructure.Services
{
    public class RackSpaceContainerClient : IRackSpaceContainerClient
    {
        public Task<IEnumerable<string>> GetContainersAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
