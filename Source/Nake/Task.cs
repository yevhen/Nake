using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Roslyn.Compilers;
using Roslyn.Compilers.CSharp;

namespace Nake
{
    class Task
    {
        const string ScriptClass = "Submission#0";

        readonly List<Task> dependencies = new List<Task>();
        readonly HashSet<TaskInvocation> invocations = new HashSet<TaskInvocation>();

        readonly MethodSymbol symbol;
        MethodInfo reflected;

        public Task(MethodSymbol symbol)
        {
            CheckSignature(symbol);
            CheckPlacement(symbol);
            CheckSummary(symbol);

            this.symbol = symbol;
        }

        static void CheckSignature(MethodSymbol symbol)
        {
            if (!symbol.IsStatic ||
                !symbol.ReturnsVoid ||
                 symbol.DeclaredAccessibility != Accessibility.Public ||
                 symbol.IsGenericMethod ||
                 symbol.Parameters.Any(p => p.RefKind != RefKind.None || !TypeConverter.IsSupported(p.Type)))
                throw new TaskSignatureViolationException(symbol.ToString());
        }

        static void CheckPlacement(MethodSymbol symbol)
        {
            var parentType = symbol.ContainingType;
            
            while (parentType.Name != ScriptClass)
            {
                var isNamespace =
                    parentType.IsStatic &&
                    parentType.DeclaredAccessibility == Accessibility.Public;

                if (!isNamespace)
                    throw new TaskPlacementViolationException(symbol.ToString());

                parentType = parentType.ContainingType;
            }
        }

        static void CheckSummary(MethodSymbol symbol)
        {
            if (symbol.GetDocumentationComment().HadXmlParseError)
                throw new InvalidXmlDocumentationException(symbol.ToString());
        }

        public static bool IsAnnotated(MethodSymbol symbol)
        {
            return symbol.GetAttributes().SingleOrDefault(x => x.AttributeClass.Name == "TaskAttribute") != null;
        }

        public string Summary
        {
            get { return symbol.GetDocumentationComment().SummaryTextOpt ?? ""; }
        }

        public bool IsGlobal()
        {
            return !FullName.Contains(".");
        }

        public string Name
        {
            get
            {
                return IsGlobal()
                        ? FullName
                        : FullName.Substring(FullName.LastIndexOf(".", StringComparison.Ordinal) + 1);
            }
        }

        public string FullName
        {
            get
            {
                return DisplayName.Substring(0, DisplayName.IndexOf("(", StringComparison.Ordinal));
            }
        }

        public string DeclaringType
        {
            get
            {                
                if (IsGlobal())
                    return ScriptClass;

                return ScriptClass + "+" + FullName.Substring(0,
                    FullName.LastIndexOf(".", StringComparison.Ordinal)).Replace(".", "+");
            }
        }

        public string DisplayName
        {
            get { return symbol.ToString(); }
        }

        public bool HasRequiredParameters()
        {
            return symbol.Parameters.Any(x => !x.HasDefaultValue);
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
                .GetMethod(Name, BindingFlags.Static | BindingFlags.Public);
        }

        public void Invoke(TaskArgument[] arguments)
        {
            var invocation = new TaskInvocation(this, reflected, arguments);

            var alreadyInvoked = !invocations.Add(invocation);
            if (alreadyInvoked)
                return;

            invocation.Invoke();
        }       

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
