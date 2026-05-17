namespace Boj.RackSpace.Application.Interfaces
{
    public interface IRackSpaceContainerClient
    {
        Task<IEnumerable<string>> GetContainersAsync(CancellationToken cancellationToken);
    }
}
