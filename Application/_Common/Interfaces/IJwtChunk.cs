using System.Collections.Generic;

namespace Application.Common.Interfaces
{
    public interface IJwtChunk
    {
        List<string> Chunk(string jwt);
        string Combine(List<string> chunks);
    }
}
