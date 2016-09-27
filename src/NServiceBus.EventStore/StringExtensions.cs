using System;
using System.Collections.Generic;
using System.Linq;

namespace NServiceBus
{
    public static class StringExtensions
    {
        static Dictionary<string, string> ToPascalCase(this Dictionary<string, string> camelCaseKeyDictionary)
        {
            return camelCaseKeyDictionary.ToDictionary(kvp => kvp.Key.ToPascalCase(), kvp => kvp.Value);
        }

        static string ToPascalCase(this string camelCaseName)
        {
            if (camelCaseName == "")
            {
                return "";
            }
            return char.ToUpperInvariant(camelCaseName[0]) + camelCaseName.Substring(1);
        }
    }
}