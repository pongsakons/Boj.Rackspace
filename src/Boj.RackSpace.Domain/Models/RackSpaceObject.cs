namespace Boj.RackSpace.Domain.Models
{
    public class RackSpaceObject
    {
        public string Name { get; set; } = default!;

        public long Size { get; set; }

        public string ContentType { get; set; } = default!;

        public string ETag { get; set; } = default!;
    }
}
