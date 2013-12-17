using System;

namespace NServiceBus
{
    public static class StringExtensions
    {
        public static string ToPascalCase(this string camelCaseName)
        {
            if (camelCaseName == "")
            {
                return "";
            }
            return Char.ToUpperInvariant(camelCaseName[0]) + camelCaseName.Substring(1);
        }
        
// ReSharper disable InconsistentNaming
        public static string ToCamelCase(this string PascalCaseName)
// ReSharper restore InconsistentNaming
        {
            if (PascalCaseName == "")
            {
                return "";
            }
            return Char.ToLowerInvariant(PascalCaseName[0]) + PascalCaseName.Substring(1);
        }
    }
}