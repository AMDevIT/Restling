using AMDevIT.Restling.Storage.Json;

namespace AMDevIT.Restling.Tests.Storage
{
    internal sealed class TestDataEncryptionKeyProtector : IDataEncryptionKeyProtector
    {
        #region Const

        private const byte Mask = 0xA7;

        #endregion

        #region Properties

        public string KeyId => "test-key";

        #endregion

        #region Methods

        /// <summary>Applies the reversible test-only key transformation.</summary>
        public ValueTask<byte[]> ProtectAsync(ReadOnlyMemory<byte> dataEncryptionKey,
                                              CancellationToken cancellationToken = default)
        {
            return ValueTask.FromResult(Transform(dataEncryptionKey, cancellationToken));
        }

        /// <summary>Reverses the test-only key transformation.</summary>
        public ValueTask<byte[]> UnprotectAsync(string keyId,
                                                ReadOnlyMemory<byte> protectedDataEncryptionKey,
                                                CancellationToken cancellationToken = default)
        {
            Assert.AreEqual(this.KeyId, keyId);
            return ValueTask.FromResult(Transform(protectedDataEncryptionKey, cancellationToken));
        }

        /// <summary>Transforms a key without providing production cryptographic protection.</summary>
        private static byte[] Transform(ReadOnlyMemory<byte> content, CancellationToken cancellationToken)
        {
            byte[] result = content.ToArray();

            cancellationToken.ThrowIfCancellationRequested();
            for (int index = 0; index < result.Length; index++)
                result[index] ^= Mask;
            return result;
        }

        #endregion
    }
}
