using AMDevIT.Restling.Core.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AMDevIT.Restling.Core.Common
{
    internal static class CharsetParser
    {
        #region Consts

        private const string UTF8 = "utf-8";
        private const string UTF16 = "utf-16";
        private const string UTF32 = "utf-32";
        private const string ASCII = "ascii";
        private const string ISO_8859_1 = "iso-8859-1";
        private const string WINDOWS_1252 = "windows-1252";

        #endregion

        #region Methods

        public static Charset Parse(string? charsetString)
        {
            Charset charset = charsetString switch
            {
                UTF8 => Charset.UTF8,
                UTF16 => Charset.UTF16,
                UTF32 => Charset.UTF32,
                ASCII => Charset.ASCII,
                ISO_8859_1 => Charset.ISO_8859_1,
                WINDOWS_1252 => Charset.WINDOWS_1252,
                _ => Charset.UTF8
            };

            return charset;
        }

        #endregion
    }
}
