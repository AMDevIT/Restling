using System.Net.Http.Headers;
using System.Text.Json;

namespace AMDevIT.Restling.Core.Codecs
{
    /// <summary>Opt-in application/problem+json codec following RFC 9457 member type handling.</summary>
    public sealed class ProblemDetailsJsonCodec : IProblemDetailsCodec
    {
        #region Properties

        public bool IsBinary => false;

        #endregion

        #region Methods

        /// <inheritdoc />
        public bool CanRead(string? mediaType)
        {
            return string.Equals(mediaType, "application/problem+json", StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public bool CanWrite(string? mediaType) => this.CanRead(mediaType);

        /// <inheritdoc />
        public T? Deserialize<T>(byte[] content, MediaTypeHeaderValue? contentType, ContentCodecContext context)
        {
            if (typeof(T) != typeof(RestProblemDetails))
                throw new NotSupportedException("Problem documents decode to RestProblemDetails, not the success model.");
            return (T?)(object?)this.DeserializeProblem(content, contentType, context);
        }

        /// <inheritdoc />
        public RestProblemDetails? DeserializeProblem(byte[] content, MediaTypeHeaderValue? contentType, ContentCodecContext context)
        {
            RestProblemDetails problem = new();
            using JsonDocument document = JsonDocument.Parse(context.DecodeText(content, contentType));
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                throw new JsonException("A problem document must be a JSON object.");

            foreach (JsonProperty property in document.RootElement.EnumerateObject())
            {
                switch (property.Name)
                {
                    case "type":
                        if (property.Value.ValueKind == JsonValueKind.String)
                            problem.Type = property.Value.GetString()!;
                        break;
                    case "title":
                        if (property.Value.ValueKind == JsonValueKind.String)
                            problem.Title = property.Value.GetString();
                        break;
                    case "detail":
                        if (property.Value.ValueKind == JsonValueKind.String)
                            problem.Detail = property.Value.GetString();
                        break;
                    case "instance":
                        if (property.Value.ValueKind == JsonValueKind.String)
                            problem.Instance = property.Value.GetString();
                        break;
                    case "status":
                        if (property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetInt32(out int status))
                            problem.Status = status;
                        break;
                    default:
                        problem.Extensions[property.Name] = property.Value.Clone();
                        break;
                }
            }
            return problem;
        }

        /// <inheritdoc />
        public HttpContent Serialize<T>(T value, MediaTypeHeaderValue contentType, ContentCodecContext context)
        {
            if (value is not RestProblemDetails problem)
                throw new ArgumentException("The problem codec requires RestProblemDetails.", nameof(value));
            return context.CreateTextContent(JsonSerializer.Serialize(problem), contentType);
        }

        #endregion
    }
}
