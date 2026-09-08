using AMDevIT.Restling.Core.Codecs;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;

namespace AMDevIT.Restling.Core.Multipart
{
    /// <summary>Reads finite parts from a potentially unbounded multipart/x-mixed-replace stream.</summary>
    internal static class MultipartMixedReplaceReader
    {
        #region Methods

        public static async IAsyncEnumerable<MultipartPart> ReadAsync(Stream stream,
                                                                      MediaTypeHeaderValue contentType,
                                                                      ContentCodecRegistry codecs,
                                                                      ContentCodecContext codecContext,
                                                                      MultipartOptions options,
                                                                      [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            List<byte> pending = [];
            byte[] readBuffer = new byte[81_920];
            string boundary = MultipartParser.GetBoundary(contentType);
            byte[] closingMarker = Encoding.ASCII.GetBytes($"--{boundary}--\r\n");

            while (true)
            {
                List<int> offsets = MultipartParser.FindBoundaryOffsets(pending, boundary);
                while (offsets.Count >= 2)
                {
                    if (IsClosing(pending, offsets[0], boundary))
                        yield break;

                    byte[] singlePart = new byte[offsets[1] - offsets[0] + closingMarker.Length];
                    pending.CopyTo(offsets[0], singlePart, 0, offsets[1] - offsets[0]);
                    closingMarker.CopyTo(singlePart, offsets[1] - offsets[0]);
                    MultipartDocument document = MultipartParser.Parse(singlePart,
                                                                        contentType,
                                                                        codecs,
                                                                        codecContext,
                                                                        options);
                    pending.RemoveRange(0, offsets[1]);
                    foreach (MultipartPart part in document.Parts)
                        yield return part;
                    offsets = MultipartParser.FindBoundaryOffsets(pending, boundary);
                }

                if (offsets.Count == 1 && IsClosing(pending, offsets[0], boundary))
                    yield break;
                if (pending.Count > options.MaxPartBytes + options.MaxHeaderBytes + 1024)
                    throw new InvalidDataException("The multipart stream part-size limit was exceeded.");

                int read = await stream.ReadAsync(readBuffer.AsMemory(), cancellationToken);
                if (read == 0)
                    throw new EndOfStreamException("The multipart/x-mixed-replace stream ended without a closing boundary.");
                pending.EnsureCapacity(pending.Count + read);
                for (int index = 0; index < read; index++)
                    pending.Add(readBuffer[index]);
            }
        }

        private static bool IsClosing(IReadOnlyList<byte> content, int offset, string boundary)
        {
            int suffix = offset + 2 + Encoding.ASCII.GetByteCount(boundary);
            return suffix + 1 < content.Count && content[suffix] == 45 && content[suffix + 1] == 45;
        }

        #endregion
    }
}
