using System.IO.Compression;
using System.Text;

namespace SurfScoutBackend.Utilities.Gzip
{
    public class GzipCompressor
    {
        public GzipCompressor() { }
        public byte[] CompressToGzip(string input)
        {
            var inputBytes = Encoding.UTF8.GetBytes(input);

            using var outputStream = new MemoryStream();

            using (var gzipStream = new GZipStream(outputStream, CompressionLevel.Optimal, leaveOpen: true))
            {
                gzipStream.Write(inputBytes, 0, inputBytes.Length);
            }

            outputStream.Position = 0;

            return outputStream.ToArray();
        }
    }
}
