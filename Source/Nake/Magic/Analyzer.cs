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
            model = compilation.GetSemanticModel(tree);

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

        static bool IsTask(ISymbol symbol)
        {
            return symbol.GetAttributes().SingleOrDefault(x => x.AttributeClass.Name == "TaskAttribute") != null;
        }     

        static bool IsStep(ISymbol symbol)
        {
            return symbol.GetAttributes().SingleOrDefault(x => x.AttributeClass.Name == "StepAttribute") != null;
        }     

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var symbol = ModelExtensions.GetSymbolInfo(model, node).Symbol as IMethodSymbol;
            
            if (symbol == null)
                return;

            if (!IsStep(symbol))
            {
                base.VisitInvocationExpression(node);
                return;
            }

            var task = result.Find(symbol);

            if (task == null)
            {
                task = new Task(symbol, true);                
                result.Add(symbol, task);
            }
            
            result.Add(node, new ProxyInvocation(task, node));

            if (current != null)
                current.AddDependency(task);

            base.VisitInvocationExpression(node);
        }

        public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            visitingConstant = node.Modifiers.Any(SyntaxKind.ConstKeyword);

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
            if (StringInterpolation.Qualifies(node))
                result.Add(node, new StringInterpolation(model, node, visitingConstant));
            
            base.VisitLiteralExpression(node);
        }
    }
}