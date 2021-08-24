using Application.Common.Interfaces;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System.IO;
using System.Text;

namespace Infrastructure
{
    public class Compact : ICompact
    {

        public byte[] Compress(string data)
        {

            var deflater = new Deflater(1, true);
            byte[] bytes = Encoding.UTF8.GetBytes(data);

            using MemoryStream inms = new MemoryStream(bytes);
            using MemoryStream outms = new MemoryStream();
            using DeflaterOutputStream dos = new DeflaterOutputStream(outms, deflater);
            inms.CopyTo(dos);

            dos.Finish();

            byte[] encoded = outms.ToArray();

            return encoded;
        }


        public string Decompress(byte[] data)
        {
            Inflater decompressor = new Inflater(true);
            decompressor.SetInput(data);

            // Create an expandable byte array to hold the decompressed data  
            MemoryStream bos = new MemoryStream(data.Length);

            // Decompress the data  
            byte[] buf = new byte[1024];
            while (!decompressor.IsFinished)
            {
                int count = decompressor.Inflate(buf);
                bos.Write(buf, 0, count);
            }

            // Get the decompressed data  
            var str = Encoding.UTF8.GetString(bos.ToArray());
            return str;
        }

        public static Stream GenerateStreamFromString(string s)
        {
            var stream =  new MemoryStream(Encoding.UTF8.GetBytes(s ?? ""));
            stream.Position = 0;
            return stream;            
        }

    }
}
