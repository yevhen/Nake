using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using AsyncTask = System.Threading.Tasks.Task;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nake
{
    using Magic;

    class Task
    {
        public static Task[] From(IEnumerable<TaskDeclaration> declarations) => 
            declarations.Select(x => new Task(x)).ToArray();

        internal const string ScriptClass = "Submission#0";
        const string SystemThreadingTaskType = "System.Threading.Tasks.Task";

        readonly List<Task> dependencies = new List<Task>();
        readonly HashSet<BodyInvocation> invocations = new HashSet<BodyInvocation>();

        readonly IMethodSymbol symbol;
        readonly bool step;
        MethodInfo reflected;

        public Task(IMethodSymbol symbol, bool step)
        {
            CheckSignature(symbol);
            Signature = symbol.ToString();
            this.symbol = symbol;
            this.step = step;
        }

        public Task(TaskDeclaration declaration)
        {
            Signature = declaration.Signature;
            step = declaration.IsStep;
        }

        static void CheckSignature(IMethodSymbol symbol)
        {
            var hasDuplicateParameters = symbol.Parameters
                .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .Any(p => p.Count() > 1);

            if (!symbol.ReturnsVoid && symbol.ReturnType.ToString() != SystemThreadingTaskType ||                
                symbol.IsGenericMethod ||
                symbol.Parameters.Any(p => p.RefKind != RefKind.None || !TypeConverter.IsSupported(p.Type)) ||
                hasDuplicateParameters)
                throw new TaskSignatureViolationException(symbol.ToString());
        }

        public MethodDeclarationSyntax Replace(MethodDeclarationSyntax method)
        {
            var alreadyExpressionBodied = method.Body == null;

            var originalBody = MethodBody(method);
            var proxyBody = ProxyBody(method, originalBody);

            var newBody = SyntaxFactory.ParseExpression(proxyBody);
            var expressionBody = SyntaxFactory.ArrowExpressionClause(newBody);

            var replacement = method
                .WithBody(null)
                .WithoutTrailingTrivia()
                .WithExpressionBody(expressionBody);

            if (!alreadyExpressionBodied)
                replacement = replacement.WithTrailingTrivia(SyntaxFactory.EndOfLine(";"));

            return replacement;
        }

        string ProxyBody(BaseMethodDeclarationSyntax method, string body)
        {
            var arguments = TaskArgument.BuildArgumentString(method.ParameterList.Parameters);
            
            return symbol.IsAsync
                ? TaskRegistry.BuildInvokeTaskAsyncString(FullName, arguments, body)
                : TaskRegistry.BuildInvokeTaskString(FullName, arguments, body);
        }

        static string MethodBody(BaseMethodDeclarationSyntax method)
        {
            if (method.Body != null)
                return method.Body.ToFullString();

            var body = method.ExpressionBody.ToFullString();
            return body.Substring(body.IndexOf("=>", StringComparison.Ordinal) + 2);
        }

        public string Signature { get; }

        public string FullName => Signature.Substring(0, Signature.IndexOf("(", StringComparison.Ordinal));
        internal bool IsGlobal => !FullName.Contains(".");

        string Name => IsGlobal ? 
            FullName : 
            FullName.Substring(FullName.LastIndexOf(".", StringComparison.Ordinal) + 1);
        
        string DeclaringType => !IsGlobal
            ? ScriptClass + "+" + FullName.Substring(0, FullName.LastIndexOf(".", StringComparison.Ordinal)).Replace(".", "+")
            : ScriptClass;

        public void AddDependency(Task dependency)
        {
            if (dependency == this)
                throw new RecursiveTaskCallException(this);

            var via = new List<string>();

            if (dependency.IsDependentUpon(this, via))
                throw new CyclicDependencyException(this, dependency, string.Join(" -> ", via) + " -> " + FullName);

            dependencies.Add(dependency);
        }

        bool IsDependentUpon(Task other, ICollection<string> chain)
        {
            chain.Add(FullName);

            return dependencies.Contains(other) ||
                   dependencies.Any(dependency => dependency.IsDependentUpon(other, chain));
        }
        
        public void Reflect(Assembly assembly)
        {
            reflected = assembly.GetType(DeclaringType)
                .GetMethod(Name, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Debug.Assert(reflected != null);            
        }

        public async AsyncTask Invoke(object script, TaskArgument[] arguments) => 
            await new TaskInvocation(script, this, reflected, arguments).Invoke();

        public async AsyncTask Invoke(IEnumerable<TaskArgument> arguments, Func<AsyncTask> body)
        {
            if (AlreadyInvoked(arguments))
                return;

            await body();
        }

        public void Invoke(IEnumerable<TaskArgument> arguments, Action body)
        {
            if (AlreadyInvoked(arguments))
                return;

            body();
        }

        bool AlreadyInvoked(IEnumerable<TaskArgument> arguments)
        {
            lock (invocations)
                return step && !invocations.Add(new BodyInvocation(arguments));
        }

        class BodyInvocation
        {
            readonly object[] values;

            public BodyInvocation(IEnumerable<TaskArgument> arguments) => 
                values = arguments.Select(x => x.Value).ToArray();

            public override bool Equals(object obj) => 
                !ReferenceEquals(null, obj) && (ReferenceEquals(this, obj) || Equals((BodyInvocation) obj));

            bool Equals(BodyInvocation other) => 
                !values.Where((value, index) => !value.Equals(other.values[index])).Any();

            public override int GetHashCode() => 
                values.Aggregate(0, (current, value) => current ^ value.GetHashCode());
        }

        public override string ToString() => Signature;
    }
}
