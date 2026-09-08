namespace AMDevIT.Restling.Storage.Json
{
    internal sealed class JsonCookieEnvelope
    {
        #region Properties

        public string Algorithm { get; set; } = "AES-256-GCM";
        public string Ciphertext { get; set; } = null!;
        public string Format { get; set; } = "Restling.Storage.Json.Encrypted";
        public string KeyId { get; set; } = null!;
        public string Nonce { get; set; } = null!;
        public string ProtectedDataEncryptionKey { get; set; } = null!;
        public string Tag { get; set; } = null!;
        public int Version { get; set; } = 1;

        #endregion
    }
}
