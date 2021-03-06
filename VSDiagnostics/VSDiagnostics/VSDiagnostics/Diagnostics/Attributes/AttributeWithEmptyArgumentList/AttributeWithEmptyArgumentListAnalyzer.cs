﻿using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using VSDiagnostics.Utilities;

namespace VSDiagnostics.Diagnostics.Attributes.AttributeWithEmptyArgumentList
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AttributeWithEmptyArgumentListAnalyzer : DiagnosticAnalyzer
    {
        private const DiagnosticSeverity Severity = DiagnosticSeverity.Warning;

        private static readonly string Category = VSDiagnosticsResources.AttributesCategory;
        private static readonly string Message = VSDiagnosticsResources.AttributeWithEmptyArgumentListAnalyzerMessage;
        private static readonly string Title = VSDiagnosticsResources.AttributeWithEmptyArgumentListAnalyzerTitle;

        internal static DiagnosticDescriptor Rule
            =>
                new DiagnosticDescriptor(DiagnosticId.AttributeWithEmptyArgumentList, Title, Message, Category, Severity,
                    true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(AnalyzeCSharpSymbol, SyntaxKind.Attribute);

        private void AnalyzeCSharpSymbol(SyntaxNodeAnalysisContext context)
        {
            var attributeSyntax = (AttributeSyntax) context.Node;

            // attribute must have arguments
            // if there are no parenthesis, the ArgumentList is null
            // if there are empty parenthesis, the ArgumentList is empty
            if (attributeSyntax.ArgumentList == null || attributeSyntax.ArgumentList.Arguments.Any())
            {
                return;
            }

            var attributeName = attributeSyntax.Name.ToString();

            context.ReportDiagnostic(Diagnostic.Create(Rule, attributeSyntax.GetLocation(), attributeName));
        }
    }
}