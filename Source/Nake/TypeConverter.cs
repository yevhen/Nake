using System;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace Nake
{
    static class TypeConverter
    {
        public static object Convert(object value, Type conversionType)
        {
            return System.Convert.ChangeType(value, conversionType);
        }

        public static bool IsSupported(ITypeSymbol type)
        {
            return type.IsBoolean() || type.IsInteger() || type.IsString();
        }
    }

    static class TypeSymbolExtensions
    {
        public static bool IsBoolean(this ITypeSymbol type)
        {
            return FullName(type) == "System.Boolean";
        }

        public static bool IsString(this ITypeSymbol type)
        {
            return FullName(type) == "System.String";
        }

        public static bool IsInteger(this ITypeSymbol type)
        {
            return FullName(type) == "System.Int32";
        }

        static string FullName(ISymbol type)
        {
            return type.ContainingNamespace.Name + "." + type.Name;
        }         
    }
}
