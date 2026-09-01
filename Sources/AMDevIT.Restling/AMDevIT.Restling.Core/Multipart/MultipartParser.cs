using AMDevIT.Restling.Core.Codecs;
using System.Net.Http.Headers;
using System.Text;

namespace AMDevIT.Restling.Core.Multipart
{
    /// <summary>Parses buffered MIME multipart entities without converting bodies to text.</summary>
    internal static class MultipartParser
    {
        #region Methods

        public static MultipartDocument Parse(byte[] content,
                                              MediaTypeHeaderValue contentType,
                                              ContentCodecRegistry codecs,
                                              ContentCodecContext codecContext,
                                              MultipartOptions options,
                                              int depth = 0)
        {
            List<BoundaryMatch> boundaries;
            List<MultipartPart> parts = [];
            string boundary;
            byte[] preamble;
            byte[] epilogue = [];
            int closingIndex = -1;

            ArgumentNullException.ThrowIfNull(content);
            ArgumentNullException.ThrowIfNull(contentType);
            ArgumentNullException.ThrowIfNull(codecs);
            ArgumentNullException.ThrowIfNull(codecContext);
            ArgumentNullException.ThrowIfNull(options);
            options.Validate();

            if (contentType.MediaType?.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase) != true)
                throw new InvalidDataException("The content type is not multipart.");
            if (depth > options.MaxNestingDepth)
                throw new InvalidDataException("The multipart nesting limit was exceeded.");

            boundary = GetBoundary(contentType);
            boundaries = FindBoundaries(content, boundary);
            if (boundaries.Count == 0)
                throw new InvalidDataException("The multipart boundary was not found in the content.");

            preamble = SliceWithoutDelimiterCrLf(content, 0, boundaries[0].Start);

            for (int index = 0; index < boundaries.Count; index++)
            {
                BoundaryMatch current = boundaries[index];
                if (current.IsClosing)
                {
                    closingIndex = index;
                    epilogue = content[current.AfterLine..];
                    break;
                }

                if (index + 1 >= boundaries.Count)
                    throw new InvalidDataException("The multipart closing boundary is missing.");
                if (parts.Count >= options.MaxParts)
                    throw new InvalidDataException("The multipart part-count limit was exceeded.");

                BoundaryMatch next = boundaries[index + 1];
                parts.Add(ParsePart(content,
                                    current.AfterLine,
                                    next.Start,
                                    contentType,
                                    codecs,
                                    codecContext,
                                    options,
                                    depth));
            }

            if (closingIndex < 0)
                throw new InvalidDataException("The multipart closing boundary is missing.");

            return new MultipartDocument(MediaTypeHeaderValue.Parse(contentType.ToString()), parts, preamble, epilogue);
        }

        internal static List<int> FindBoundaryOffsets(IReadOnlyList<byte> content, string boundary)
        {
            return FindBoundaries(content, boundary).Select(match => match.Start).ToList();
        }

        internal static string GetBoundary(MediaTypeHeaderValue contentType)
        {
            NameValueHeaderValue? parameter = contentType.Parameters.FirstOrDefault(item =>
                string.Equals(item.Name, "boundary", StringComparison.OrdinalIgnoreCase));
            string? boundary = parameter?.Value?.Trim().Trim('"');

            if (string.IsNullOrWhiteSpace(boundary))
                throw new InvalidDataException("A multipart boundary parameter is required.");
            const string allowed = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ'()+_,-./:=? ";
            if (boundary.Length > 70 || boundary.EndsWith(' ') || boundary.Any(character => !allowed.Contains(character)))
                throw new InvalidDataException("The multipart boundary is invalid.");
            return boundary;
        }

        private static MultipartPart ParsePart(byte[] source,
                                               int start,
                                               int end,
                                               MediaTypeHeaderValue parentContentType,
                                               ContentCodecRegistry codecs,
                                               ContentCodecContext codecContext,
                                               MultipartOptions options,
                                               int depth)
        {
            Dictionary<string, IReadOnlyList<string>> headers;
            MultipartDocument? nested = null;
            MediaTypeHeaderValue partContentType;
            byte[] body;
            int bodyStart;
            int effectiveEnd = TrimDelimiterCrLf(source, start, end);
            int separator = FindSequence(source, start, effectiveEnd, [13, 10, 13, 10]);

            if (start + 1 < effectiveEnd && source[start] == 13 && source[start + 1] == 10)
            {
                headers = new(StringComparer.OrdinalIgnoreCase);
                bodyStart = start + 2;
            }
            else
            {
                if (separator < 0)
                    throw new InvalidDataException("A multipart part does not contain a header terminator.");
                if (separator - start > options.MaxHeaderBytes)
                    throw new InvalidDataException("The multipart header-size limit was exceeded.");
                headers = ParseHeaders(source[start..separator]);
                bodyStart = separator + 4;
            }

            if (effectiveEnd - bodyStart > options.MaxPartBytes)
                throw new InvalidDataException("The multipart part-size limit was exceeded.");
            body = source[bodyStart..effectiveEnd];

            if (!headers.TryGetValue("Content-Type", out IReadOnlyList<string>? values) || values.Count == 0)
            {
                string defaultType = string.Equals(parentContentType.MediaType, "multipart/digest", StringComparison.OrdinalIgnoreCase)
                    ? "message/rfc822"
                    : "text/plain";
                headers["Content-Type"] = new[] { defaultType };
                partContentType = MediaTypeHeaderValue.Parse(defaultType);
            }
            else
            {
                partContentType = MediaTypeHeaderValue.Parse(values[0]);
            }

            if (partContentType.MediaType?.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase) == true)
                nested = Parse(body, partContentType, codecs, codecContext, options, depth + 1);

            return new MultipartPart(headers, body, nested, codecs, codecContext);
        }

        private static Dictionary<string, IReadOnlyList<string>> ParseHeaders(byte[] content)
        {
            Dictionary<string, List<string>> parsed = new(StringComparer.OrdinalIgnoreCase);
            string[] lines = Encoding.ASCII.GetString(content).Split("\r\n", StringSplitOptions.None);
            string? currentName = null;

            foreach (string line in lines)
            {
                if ((line.StartsWith(' ') || line.StartsWith('\t')) && currentName != null)
                {
                    List<string> values = parsed[currentName];
                    values[^1] = $"{values[^1]} {line.Trim()}";
                    continue;
                }

                int colon = line.IndexOf(':');
                if (colon <= 0)
                    throw new InvalidDataException("A multipart part contains an invalid header.");
                currentName = line[..colon].Trim();
                string value = line[(colon + 1)..].Trim();
                if (!parsed.TryGetValue(currentName, out List<string>? valuesForName))
                {
                    valuesForName = [];
                    parsed[currentName] = valuesForName;
                }
                valuesForName.Add(value);
            }

            return parsed.ToDictionary(item => item.Key,
                                       item => (IReadOnlyList<string>)item.Value.AsReadOnly(),
                                       StringComparer.OrdinalIgnoreCase);
        }

        private static List<BoundaryMatch> FindBoundaries(IReadOnlyList<byte> content, string boundary)
        {
            byte[] marker = Encoding.ASCII.GetBytes($"--{boundary}");
            List<BoundaryMatch> result = [];

            for (int index = 0; index <= content.Count - marker.Length; index++)
            {
                if (index != 0 && (index < 2 || content[index - 2] != 13 || content[index - 1] != 10))
                    continue;
                if (!Matches(content, index, marker))
                    continue;

                int cursor = index + marker.Length;
                bool closing = cursor + 1 < content.Count && content[cursor] == 45 && content[cursor + 1] == 45;
                if (closing)
                    cursor += 2;
                while (cursor < content.Count && (content[cursor] == 32 || content[cursor] == 9))
                    cursor++;
                if (cursor == content.Count)
                {
                    result.Add(new BoundaryMatch(index, cursor, closing));
                    continue;
                }
                if (cursor + 1 >= content.Count || content[cursor] != 13 || content[cursor + 1] != 10)
                    continue;
                result.Add(new BoundaryMatch(index, cursor + 2, closing));
            }

            return result;
        }

        private static bool Matches(IReadOnlyList<byte> content, int offset, IReadOnlyList<byte> marker)
        {
            for (int index = 0; index < marker.Count; index++)
            {
                if (content[offset + index] != marker[index])
                    return false;
            }
            return true;
        }

        private static int FindSequence(byte[] source, int start, int end, byte[] sequence)
        {
            for (int index = start; index <= end - sequence.Length; index++)
            {
                if (source.AsSpan(index, sequence.Length).SequenceEqual(sequence))
                    return index;
            }
            return -1;
        }

        private static int TrimDelimiterCrLf(byte[] source, int start, int end)
        {
            return end - start >= 2 && source[end - 2] == 13 && source[end - 1] == 10 ? end - 2 : end;
        }

        private static byte[] SliceWithoutDelimiterCrLf(byte[] source, int start, int end)
        {
            return source[start..TrimDelimiterCrLf(source, start, end)];
        }

        private readonly record struct BoundaryMatch(int Start, int AfterLine, bool IsClosing);

        #endregion
    }
}
