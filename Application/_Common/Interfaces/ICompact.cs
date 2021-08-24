
namespace Application.Common.Interfaces
{
    public interface ICompact
    {
        byte[] Compress(string data);
        string Decompress(byte[] data);
    }
}
