using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AMDevIT.Restling.Tests.Common
{
    internal static class StringUtils
    {
        #region Methods

        internal static string DictionaryToString(Dictionary<string, string>? dictionary)
        {
            if (dictionary == null || dictionary.Count == 0)
                return "None";

            return string.Join(", ", dictionary.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
        }

        #endregion
    }
}
