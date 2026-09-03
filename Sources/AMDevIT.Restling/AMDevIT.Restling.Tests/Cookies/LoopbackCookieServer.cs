using System.Net;
using System.Net.Sockets;
using System.Text;

namespace AMDevIT.Restling.Tests.Cookies
{
    /// <summary>Serves a bounded HTTP script over loopback so tests exercise real transport cookie handling.</summary>
    internal sealed class LoopbackCookieServer : IAsyncDisposable
    {
        #region Fields

        private readonly TcpListener listener;
        private readonly CancellationTokenSource lifetime = new(TimeSpan.FromSeconds(15));

        #endregion

        #region Properties

        public Uri BaseUri { get; }
        public Task<IReadOnlyList<Request>> Requests { get; }

        #endregion

        #region .ctor

        /// <summary>Binds an ephemeral loopback port and starts serving one response per connection.</summary>
        public LoopbackCookieServer(params string[] responses)
        {
            this.listener = new TcpListener(IPAddress.Loopback, 0);
            this.listener.Start();
            this.BaseUri = new Uri($"http://127.0.0.1:{((IPEndPoint)this.listener.LocalEndpoint).Port}/");
            this.Requests = this.ServeAsync(responses);
        }

        #endregion

        #region Methods

        /// <summary>Builds a small response with separate header lines and a known body length.</summary>
        public static string Response(int statusCode, params string[] headers)
        {
            return ResponseWithBody(statusCode, "ok", "text/plain; charset=utf-8", headers);
        }

        /// <summary>Builds a small response with a caller-provided body and content type.</summary>
        public static string ResponseWithBody(int statusCode, string body, string contentType, params string[] headers)
        {
            byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
            return $"HTTP/1.1 {statusCode} Test\r\n" +
                   string.Concat(headers.Select(header => header + "\r\n")) +
                   $"Content-Type: {contentType}\r\nContent-Length: {bodyBytes.Length}\r\nConnection: close\r\n\r\n{body}";
        }

        /// <summary>Cancels pending accepts and closes the listener even when a test fails early.</summary>
        public async ValueTask DisposeAsync()
        {
            this.lifetime.Cancel();
            this.listener.Stop();
            try
            {
                await this.Requests;
            }
            catch (OperationCanceledException)
            {
            }
            catch (SocketException) when (this.lifetime.IsCancellationRequested)
            {
            }
            catch (ObjectDisposedException) when (this.lifetime.IsCancellationRequested)
            {
            }
            finally
            {
                this.lifetime.Dispose();
            }
        }

        /// <summary>Records requests and emits the configured responses without using external services.</summary>
        private async Task<IReadOnlyList<Request>> ServeAsync(string[] responses)
        {
            List<Request> requests = [];

            foreach (string response in responses)
            {
                using TcpClient connection = await this.listener.AcceptTcpClientAsync(this.lifetime.Token);
                using NetworkStream stream = connection.GetStream();
                requests.Add(await ReadRequestAsync(stream, this.lifetime.Token));
                await stream.WriteAsync(Encoding.ASCII.GetBytes(response), this.lifetime.Token);
            }

            return requests;
        }

        /// <summary>Reads bounded HTTP headers and any fixed-length body sent by these tests.</summary>
        private static async Task<Request> ReadRequestAsync(NetworkStream stream, CancellationToken cancellationToken)
        {
            List<byte> bytes = [];
            byte[] next = new byte[1];
            Dictionary<string, string> headers = new(StringComparer.OrdinalIgnoreCase);

            while (bytes.Count < 32768)
            {
                await stream.ReadExactlyAsync(next, cancellationToken);
                bytes.Add(next[0]);
                if (bytes.Count >= 4 && bytes[^4] == '\r' && bytes[^3] == '\n' && bytes[^2] == '\r' && bytes[^1] == '\n')
                    break;
            }

            if (bytes.Count >= 32768)
                throw new InvalidDataException("Loopback request headers exceeded the test limit.");

            string[] lines = Encoding.ASCII.GetString(bytes.ToArray()).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
            string[] requestLine = lines[0].Split(' ');
            foreach (string line in lines.Skip(1))
            {
                int separator = line.IndexOf(':');
                headers.Add(line[..separator], line[(separator + 1)..].Trim());
            }

            int length = headers.TryGetValue("Content-Length", out string? contentLength) ? int.Parse(contentLength) : 0;
            if (length < 0 || length > 65536 || headers.ContainsKey("Transfer-Encoding"))
                throw new InvalidDataException("Unsupported body framing for a loopback test request.");
            byte[] body = new byte[length];
            await stream.ReadExactlyAsync(body, cancellationToken);
            return new Request(requestLine[0], requestLine[1], headers, body);
        }

        #endregion

        /// <summary>A captured request, independent of the lifetime of its TCP connection.</summary>
        public sealed record Request(string Method, string Target, IReadOnlyDictionary<string, string> Headers, byte[] Body);
    }
}
