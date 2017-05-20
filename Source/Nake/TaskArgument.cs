using System;
using System.Linq;
using System.Reflection;

namespace Nake
{
	public class TaskArgument
	{
		public readonly string Name = "";
		public readonly object Value;

		public TaskArgument(object value)
			: this("", value)
		{ }

		public TaskArgument(string name, object value)
		{
			Name = name ?? throw new ArgumentNullException("name");
			Value = value;
		}

		public bool IsPositional()
		{
			return string.IsNullOrEmpty(Name);
		}

		public bool IsNamed()
		{
			return !IsPositional();
		}

		public object Convert(Type conversionType)
		{
			return conversionType.GetTypeInfo().IsEnum
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

		object ConvertSimpleValue(Type conversionType)
		{
			return TypeConverter.Convert(Value, conversionType);
		}
	}
}