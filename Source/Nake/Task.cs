using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using AsyncTask = System.Threading.Tasks.Task;

using Microsoft.CodeAnalysis;
using Nake.Magic;

namespace Nake
{
    class Task
    {
        internal const string ScriptClass = "Submission#0";

        readonly List<Task> dependencies = new List<Task>();
        readonly HashSet<TaskInvocation> invocations = new HashSet<TaskInvocation>();

        readonly string signature;
        readonly bool step;
        MethodInfo reflected;

        public Task(IMethodSymbol symbol, bool step)
        {
            CheckSignature(symbol);
            signature = symbol.ToString();
            this.step = step;
        }

        public Task(TaskDeclaration declaration)
        {
            signature = declaration.Signature;
            step = declaration.IsStep;
        }

        static void CheckSignature(IMethodSymbol symbol)
        {
            bool hasDuplicateParameters = symbol.Parameters
                .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .Any(p => p.Count() > 1);

            if ((!symbol.ReturnsVoid && symbol.ReturnType.ToString() != "System.Threading.Tasks.Task") ||                
                symbol.IsGenericMethod ||
                symbol.Parameters.Any(p => p.RefKind != RefKind.None || !TypeConverter.IsSupported(p.Type)) ||
                hasDuplicateParameters)
                throw new TaskSignatureViolationException(symbol.ToString());
        }  

        internal bool IsGlobal
        {
            get { return !FullName.Contains("."); }
        }

        string Name
        {
            get
            {
                return IsGlobal
                        ? FullName
                        : FullName.Substring(FullName.LastIndexOf(".", StringComparison.Ordinal) + 1);
            }
        }

        public string FullName
        {
            get { return Signature.Substring(0, Signature.IndexOf("(", StringComparison.Ordinal)); }
        }

        string DeclaringType
        {
            get
            {
                return !IsGlobal
                        ? ScriptClass + "+" + FullName.Substring(0, FullName.LastIndexOf(".", StringComparison.Ordinal)).Replace(".", "+")
                        : ScriptClass;
            }
        }

        public string Signature
        {
            get { return signature; }
        }

        public void AddDependency(Task dependency)
        {
            if (dependency == this)
                throw new RecursiveTaskCallException(this);

            var via = new List<string>();

            if (dependency.IsDependantUpon(this, via))
                throw new CyclicDependencyException(this, dependency, string.Join(" -> ", via) + " -> " + FullName);

            dependencies.Add(dependency);
        }

        bool IsDependantUpon(Task other, ICollection<string> chain)
        {
            chain.Add(FullName);

            return dependencies.Contains(other) ||
                   dependencies.Any(dependency => dependency.IsDependantUpon(other, chain));
        }
        
        public void Reflect(Assembly assembly)
        {
            reflected = assembly.GetType(DeclaringType)
                .GetMethod(Name, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Debug.Assert(reflected != null);            
        }

        public async AsyncTask Invoke(object script, TaskArgument[] arguments)
        {
            var invocation = new TaskInvocation(script, this, reflected, arguments);

            if (step)
            {
                var alreadyInvoked = !invocations.Add(invocation);
                if (alreadyInvoked)
                    return;
            }

            await invocation.Invoke();
        }       

        public override string ToString()
        {
            return Signature;
        }
    }
}
