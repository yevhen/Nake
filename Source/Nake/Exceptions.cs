using System;
using System.Collections.Generic;
using static System.Environment;

using Microsoft.CodeAnalysis;

namespace Nake
{
    using Magic;

    class NakeException : Exception
    {
        public NakeException(string message)
            : base(message)
        {}

        public NakeException(string message, Exception inner)
            : base(message, inner)
        {}

        public NakeException(string message, params object[] args)
            : base(string.Format(message, args))
        {}
    }

    class ScriptCompilationException : NakeException
    {
        public ScriptCompilationException(IEnumerable<Diagnostic> diagnostics)
            : base($"Script compilation failure! See diagnostics below.{NewLine}" +
                   $"{string.Join(NewLine, diagnostics)}")
        {}
    }

    class RewrittenScriptCompilationException : NakeException
    {
        public readonly string SourceText;
        public readonly string RewrittenText;

        public RewrittenScriptCompilationException(string sourceText, string rewrittenText, IEnumerable<Diagnostic> diagnostics)
            : base("Oops, we broke your script after rewriting it. " +
                   $"Open an issue on GH and include the following.{NewLine}" +
                   $"Source:{NewLine}{sourceText}{NewLine}" +
                   $"Rewritten:{NewLine}{rewrittenText}{NewLine}" +
                   $"{string.Join(NewLine, diagnostics)}")
        {
            SourceText = sourceText;
            RewrittenText = rewrittenText;
        }
    }

    class TaskNotFoundException : NakeException
    {
        internal TaskNotFoundException(string name)
            : base("Task '{0}' cannot be found", name)
        {}
    }

    class TaskArgumentException : NakeException
    {
        public TaskArgumentException(Task task, string message)
            : base("Failed to bind parameters for task '{0}'.\r\nError: {1}", task, message)
        {}

        public TaskArgumentException(Task task, string parameter, int position, string message)
            : base("Failed to bind parameter '{1}' in position {2} when invoking task '{0}'.\r\nError: {3}",
                    task, parameter, position, message)
        {}
    }

    class TaskArgumentOrderException : NakeException
    {
        public TaskArgumentOrderException(string task)
            : base("Positional arguments cannot be specified after named arguments. Task: {0}", task)
        {}
    }

    class TaskInvocationException : NakeException
    {
        readonly Exception source;

        public TaskInvocationException(Task task, Exception source)
            : base($"'{task}' task failed. Error: '{source.Message}'", source)
        {
            this.source = source;
        }

        public Exception SourceException => source;
    }

    class DuplicateTaskException : NakeException
    {
        public static DuplicateTaskException Create(Task existent, Task duplicate)
        {
            return Create(existent.Signature, duplicate.Signature);
        }

        public static DuplicateTaskException Create(TaskDeclaration existent, TaskDeclaration duplicate)
        {
            return Create(existent.Signature, duplicate.Signature);
        }

        static DuplicateTaskException Create(string existent, string duplicate)
        {
            var caseOnlyDifference = String.Equals(existent, duplicate, 
                StringComparison.CurrentCultureIgnoreCase);

            if (caseOnlyDifference)
                return new DuplicateTaskException(existent, duplicate,
                    "Task names are case-insensitive");

            return new DuplicateTaskException(existent, duplicate,
                    "Overloads are not supported. Use optional parameters instead");            
        }

        DuplicateTaskException(string existent, string duplicate, string reason)
            : base("Duplicate task declaration: '{0}' and '{1}'. {2}", existent, duplicate, reason)
        {}
    }

    class CyclicDependencyException : NakeException
    {
        public CyclicDependencyException(Task from, Task to, string via)
            : base("Cyclic dependency detected from '{0}' to '{1}' via {2}", from, to, via)
        {}
    }

    class RecursiveTaskCallException : NakeException
    {
        public RecursiveTaskCallException(Task task)
            : base("Recursive call detected within '{0}' task", task)
        {}
    }

    class TaskSignatureViolationException : NakeException
    {
        public TaskSignatureViolationException(string method)
            : base("Bad task method signature: '{0}'. Should be public static void non-generic, have no out or ref parameters, have no duplicate parameters that differ only by case and all parameters should be either bool, int, string or enum type", method)
        {}
    }
}
