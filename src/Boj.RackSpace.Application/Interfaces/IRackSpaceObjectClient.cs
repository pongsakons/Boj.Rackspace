using Boj.RackSpace.Domain.Models;

namespace Boj.RackSpace.Application.Interfaces
{
    public interface IRackSpaceObjectClient
    {
        Task<IEnumerable<RackSpaceObject>> GetObjectsAsync(string container, CancellationToken cancellationToken);

        Task UploadAsync(string container,string objectName,Stream stream,CancellationToken cancellationToken);

        Task<Stream> DownloadAsync(string container,string objectName,CancellationToken cancellationToken);

        Task DeleteAsync(string container,string objectName,CancellationToken cancellationToken);
    }
}
