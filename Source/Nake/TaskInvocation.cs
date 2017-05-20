﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Nake
{
	class TaskInvocation
	{
		readonly object script;
		readonly Task task;
		readonly MethodInfo method;
		readonly object[] values;

		public TaskInvocation(object script, Task task, MethodInfo method, IList<TaskArgument> arguments)
		{
			this.script = script;
			this.task = task;
			this.method = method;

			values = Bind(arguments);
		}

		object[] Bind(IList<TaskArgument> arguments)
		{
			var result = method.GetParameters()
				.ToDictionary(parameter => parameter, parameter => Type.Missing);

			for (var i = 0; i < arguments.Count; i++)
			{
				var argument = arguments[i];

				var parameter = argument.IsPositional()
					? GetParameterByPosition(i)
					: GetParameterByName(argument.Name);

				if (parameter == null)
					throw new TaskArgumentException(task,
						string.Format("Cannot bind argument ({0}) -> {1}{2}",
									  i + 1, argument.IsNamed() ? argument.Name + ":" : "", argument.Value));

				result[parameter] = Convert(argument, i, parameter);
			}

			return result.Values.ToArray();
		}

		ParameterInfo GetParameterByPosition(int position)
		{
			return method.GetParameters().SingleOrDefault(x => x.Position == position);
		}

		ParameterInfo GetParameterByName(string name)
		{
			return method.GetParameters().SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));
		}

		object Convert(TaskArgument arg, int position, ParameterInfo parameter)
		{
			try
			{
				return arg.Convert(parameter.ParameterType);
			}
			catch (InvalidCastException e)
			{
				throw new TaskArgumentException(task, parameter.Name, position, e.Message);
			}
			catch (FormatException e)
			{
				throw new TaskArgumentException(task, parameter.Name, position, e.Message);
			}
			catch (ArgumentException e)
			{
				throw new TaskArgumentException(task, parameter.Name, position, e.Message);
			}
			catch (OverflowException e)
			{
				throw new TaskArgumentException(task, parameter.Name, position, e.Message);
			}
		}

		public void Invoke()
		{
			try
			{
				object host = null;

				if (!method.IsStatic)
					host = GetMethodHost();

				method.Invoke(
					host, new Object[]
					{
						BindingFlags.Public | BindingFlags.NonPublic,
						null,
						values,
						CultureInfo.InvariantCulture
					});
			}
			catch (ArgumentException)
			{
				throw new TaskArgumentException(task, "Missing parameter does not have a default value.");
			}
			catch (TargetParameterCountException)
			{
				throw new TaskArgumentException(task, "Parameter count mismatch");
			}
			catch (TargetInvocationException ex)
			{
				throw new TaskInvocationException(task, ex.GetBaseException());
			}
		}

		object GetMethodHost()
		{
			Debug.Assert(method.DeclaringType != null);
			return task.IsGlobal ? script : Activator.CreateInstance(method.DeclaringType);
		}

		bool Equals(TaskInvocation other)
		{
			return !values.Where((value, index) => value != other.values[index]).Any();
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;

			if (ReferenceEquals(this, obj))
				return true;

			return obj.GetType() == typeof(TaskInvocation) && Equals((TaskInvocation)obj);
		}

		public override int GetHashCode()
		{
			return values.Aggregate(0, (current, value) => current ^ value.GetHashCode());
		}

		public static bool operator ==(TaskInvocation left, TaskInvocation right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(TaskInvocation left, TaskInvocation right)
		{
			return !Equals(left, right);
		}
	}
}