using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nake;

public class TaskArgument
{
    static string FullTypeName => typeof(TaskArgument).FullName;

    public static string BuildArgumentString(IReadOnlyList<ParameterSyntax> parameters)
    {
        return parameters.Count != 0
            ? $"new {FullTypeName}[]{{{string.Join(", ", parameters.Select(Format))}}}"
            : $"new {FullTypeName}[0]";

        static string Format(ParameterSyntax parameter)
        {
            return $@"new {FullTypeName}(""{ArgumentName()}"", {ArgumentName()})";
            string ArgumentName() => parameter.Identifier.ValueText;
        }
    }
    
    public readonly string Name;
    public readonly object Value;

    public TaskArgument(object value)
        : this("", value)
    {}

    public TaskArgument(string name, object value)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Value = value;
    }

    public bool IsPositional() => string.IsNullOrEmpty(Name);
    public bool IsNamed() => !IsPositional();

    public object Convert(Type conversionType)
    {
        return conversionType.IsEnum 
                ? ConvertEnumValue(conversionType) 
                : ConvertSimpleValue(conversionType);
    }

    object ConvertEnumValue(Type enumType)
    {
        var parts = Value.ToString().Split('.');

        var value = parts.Length == 1 
            ? parts[0] 
            : parts[1];

        return Enum.Parse(enumType, value, true);
    }

    object ConvertSimpleValue(Type conversionType) => TypeConverter.Convert(Value, conversionType);
}