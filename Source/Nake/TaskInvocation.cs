using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using AsyncTask = System.Threading.Tasks.Task;

namespace Nake;

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
                    $"Cannot bind argument ({i + 1}) -> {(argument.IsNamed() ? argument.Name + ":" : "")}{argument.Value}");

            result[parameter] = Convert(argument, i, parameter);
        }

        return result.Values.ToArray();
    }

    ParameterInfo? GetParameterByPosition(int position) => 
        method.GetParameters().SingleOrDefault(x => x.Position == position);

    ParameterInfo? GetParameterByName(string name) => 
        method.GetParameters().SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.OrdinalIgnoreCase));

    object Convert(TaskArgument arg, int position, ParameterInfo parameter)
    {
        try
        {
            return arg.Convert(parameter.ParameterType);
        }
        catch (InvalidCastException e)
        {
            throw new TaskArgumentException(task, parameter.Name ?? "", position, e.Message);
        }
        catch (FormatException e)
        {
            throw new TaskArgumentException(task, parameter.Name ?? "", position, e.Message);
        }
        catch (ArgumentException e)
        {
            throw new TaskArgumentException(task, parameter.Name ?? "", position, e.Message);
        }
        catch (OverflowException e)
        {
            throw new TaskArgumentException(task, parameter.Name ?? "", position, e.Message);
        }
    }

    public async AsyncTask Invoke()
    {
        try
        {
            object? host = null;

            if (!method.IsStatic)
                host = GetMethodHost();                    

            var result = method.Invoke(
                host, BindingFlags.OptionalParamBinding, null,
                values, CultureInfo.InvariantCulture);

            switch (result)
            {
                case AsyncTask t:
                    await t;
                    break;
                default:
                    await AsyncTask.CompletedTask;
                    break;
            }
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
        return task.IsGlobal ? script : Activator.CreateInstance(method.DeclaringType) ?? throw new InvalidOperationException($"Failed to create instance of {method.DeclaringType}");
    }
}