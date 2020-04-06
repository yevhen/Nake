using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Nake.Magic
{
    class Analyzer : CSharpSyntaxWalker
    {
        readonly CSharpSyntaxTree tree;
        readonly SemanticModel model;
        readonly IDictionary<string, string> substitutions;

        Task current;
        bool visitingConstant;
        AnalyzerResult result;
        
        public Analyzer(CSharpCompilation compilation, IDictionary<string, string> substitutions)
        {
            tree = (CSharpSyntaxTree) compilation.SyntaxTrees.Single();
            model = compilation.GetSemanticModel(tree, ignoreAccessibility: false);

            this.substitutions = new Dictionary<string, string>(
                substitutions, new CaseInsensitiveEqualityComparer());
        }

        public AnalyzerResult Analyze()
        {
            result = new AnalyzerResult();
            tree.GetRoot().Accept(this);
            return result;
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            var symbol = model.GetDeclaredSymbol(node);

            var isTask = IsTask(symbol);
            var isStep = IsStep(symbol);

            if (!isTask && !isStep)
            {
                base.VisitMethodDeclaration(node);
                return;
            }

            current = result.Find(symbol);

            if (current == null)
            {
                current = new Task(symbol, isStep);
                result.Add(symbol, current);
            }

            base.VisitMethodDeclaration(node);
            current = null;
        }

        static bool IsTask(ISymbol symbol) => HasAttribute(symbol, "NakeAttribute");
        static bool IsStep(ISymbol symbol) => HasAttribute(symbol, "StepAttribute");

        static bool HasAttribute(ISymbol symbol, string attribute) => 
            symbol.GetAttributes().SingleOrDefault(x => x.AttributeClass.Name == attribute) != null;

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (ModelExtensions.GetSymbolInfo(model, node).Symbol is IMethodSymbol symbol)
            {
                var isStep = IsStep(symbol);
                var isTask = IsTask(symbol);

                if (isStep || isTask)
                {
                    var task = result.Find(symbol);
                    if (task == null)
                    {
                        task = new Task(symbol, isStep);
                        result.Add(symbol, task);
                    }

                    current?.AddDependency(task);
                }
            }

            base.VisitInvocationExpression(node);
        }

        public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            visitingConstant = node.Modifiers.Any(SyntaxKind.ConstKeyword);
            if (visitingConstant)
            {
                base.VisitFieldDeclaration(node);
                return;
            }

            foreach (var variable in node.Declaration.Variables)
            {
                var symbol = (IFieldSymbol) ModelExtensions.GetDeclaredSymbol(model, variable);

                var fullName = symbol.ToString();
                if (!substitutions.ContainsKey(fullName))
                    continue;

                if (FieldSubstitution.Qualifies(symbol))
                    result.Add(variable, new FieldSubstitution(variable, symbol, substitutions[fullName]));
            }

            base.VisitFieldDeclaration(node);
            visitingConstant = false;
        }

        public override void VisitLocalDeclarationStatement(LocalDeclarationStatementSyntax node)
        {
            visitingConstant = node.Modifiers.Any(SyntaxKind.ConstKeyword);
            base.VisitLocalDeclarationStatement(node);
            visitingConstant = false;
        }

        public override void VisitParameter(ParameterSyntax node)
        {
            visitingConstant = true;
            base.VisitParameter(node);
            visitingConstant = false;
        }

        public override void VisitAttribute(AttributeSyntax node)
        {
            visitingConstant = true;
            base.VisitAttribute(node);
            visitingConstant = false;
        }

        public override void VisitLiteralExpression(LiteralExpressionSyntax node)
        {
            var interpolation = EnvironmentVariableInterpolation.Match(model, node, visitingConstant);
            if (interpolation != null)
                result.Add(node, interpolation);

            base.VisitLiteralExpression(node);
        }

        public override void VisitInterpolatedStringExpression(InterpolatedStringExpressionSyntax node)
        { 
            var interpolation = EnvironmentVariableInterpolation.Match(model, node);
            if (interpolation != null)
                result.Add(node, interpolation);

            base.VisitInterpolatedStringExpression(node);
        }
    }
}