using System;
using System.Collections.Generic;
using System.Linq;

using Roslyn.Compilers.CSharp;

namespace Nake
{
    class TypeConverter
    {
        public static object Convert(object value, Type conversionType)
        {
            return System.Convert.ChangeType(value, conversionType);
        }

        public static bool IsSupported(TypeSymbol type)
        {
            return type.IsBoolean() || type.IsInteger() || type.IsString();
        }
    }

    static class TypeSymbolExtensions
    {
        public static bool IsBoolean(this TypeSymbol type)
        {
            return FullName(type) == "System.Boolean";
        }

        public static bool IsString(this TypeSymbol type)
        {
            return FullName(type) == "System.String";
        }

        public static bool IsInteger(this TypeSymbol type)
        {
            return FullName(type) == "System.Int32";
        }

        static string FullName(Symbol type)
        {
            return type.ContainingNamespace.Name + "." + type.Name;
        }         
    }
}
