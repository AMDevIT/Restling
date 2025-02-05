using System.Runtime.InteropServices;
using System.Security;

namespace AMDevIT.Restling.Core.Security
{
    internal static class SecureStringExtensions
    {
        internal static byte[] ToByteArray(this SecureString secureString)
        {
            if (secureString == null)
                throw new ArgumentNullException(nameof(secureString));

            IntPtr ptr = IntPtr.Zero;

            try
            {
                int length;
                byte[] byteArray;

                ptr = Marshal.SecureStringToBSTR(secureString);
                length = secureString.Length;
                byteArray = new byte[length * sizeof(char)];

                for (int i = 0; i < length; i++)
                {
                    short unicodeChar = Marshal.ReadInt16(ptr, i * sizeof(char));
                    byteArray[i * 2] = (byte)(unicodeChar & 0xFF);
                    byteArray[i * 2 + 1] = (byte)(unicodeChar >> 8);
                }

                return byteArray;
            }
            finally
            {  
                if (ptr != IntPtr.Zero)
                    Marshal.ZeroFreeBSTR(ptr);
            }
        }

        internal static string ToUnsecureString(this SecureString secureString)
        {
            if (secureString != null)
            {
                IntPtr ptr = IntPtr.Zero;
                try
                {
                    ptr = Marshal.SecureStringToBSTR(secureString);
                    return Marshal.PtrToStringBSTR(ptr);
                }
                finally
                {
                    if (ptr != IntPtr.Zero)
                        Marshal.ZeroFreeBSTR(ptr);
                }
            }

            throw new ArgumentNullException(nameof(secureString));
        }
    }
}
