﻿using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace VSDiagnostics.Diagnostics.Attributes.FlagsEnumValuesAreNotPowersOfTwo
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class FlagsEnumValuesAreNotPowersOfTwoAnalyzer : DiagnosticAnalyzer
    {
        private const string DiagnosticId = nameof(FlagsEnumValuesAreNotPowersOfTwoAnalyzer);
        private const string DiagnosticIdValuesDontFit = "AnotherIDWeHaveToInvent"; // TODO
        private const DiagnosticSeverity Severity = DiagnosticSeverity.Error;

        private static readonly string Category = VSDiagnosticsResources.AttributesCategory;
        private static readonly string Message = VSDiagnosticsResources.FlagsEnumValuesAreNotPowersOfTwoAnalyzerMessage;

        private static readonly string MessageValuesDontFit =
            VSDiagnosticsResources.FlagsEnumValuesAreNotPowersOfTwoValuesDontFitAnalyzerMessage;

        private static readonly string Title = VSDiagnosticsResources.FlagsEnumValuesAreNotPowersOfTwoAnalyzerTitle;

        internal static DiagnosticDescriptor DefaultRule
            => new DiagnosticDescriptor(DiagnosticId, Title, Message, Category, Severity, true);

        internal static DiagnosticDescriptor ValuesDontFitRule
            => new DiagnosticDescriptor(DiagnosticIdValuesDontFit, Title, MessageValuesDontFit, Category, Severity,
                    true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(DefaultRule, ValuesDontFitRule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSymbol, SyntaxKind.EnumDeclaration);
        }

        private void AnalyzeSymbol(SyntaxNodeAnalysisContext context)
        {
            var declarationExpression = (EnumDeclarationSyntax) context.Node;
            var flagsAttribute = declarationExpression.AttributeLists.FirstOrDefault(
                a => a.Attributes.FirstOrDefault(
                    t =>
                    {
                        var symbol = context.SemanticModel.GetSymbolInfo(t).Symbol;
                        return symbol == null || symbol.ContainingType.MetadataName == typeof (FlagsAttribute).Name;
                    }) != null);


            if (flagsAttribute == null)
            {
                return;
            }

            var enumName = context.SemanticModel.GetDeclaredSymbol(declarationExpression).Name;
            var enumMemberDeclarations =
                declarationExpression.ChildNodes().OfType<EnumMemberDeclarationSyntax>().ToList();
            Action reportDiagnostic =
                () =>
                    context.ReportDiagnostic(Diagnostic.Create(DefaultRule,
                        declarationExpression.Identifier.GetLocation(),
                        enumName));

            foreach (var member in enumMemberDeclarations)
            {
                if (member.EqualsValue == null)
                {
                    continue;
                }

                var descendantNodes = member.EqualsValue.Value.DescendantNodesAndSelf().ToList();
                if (descendantNodes.OfType<LiteralExpressionSyntax>().Any() &&
                    descendantNodes.OfType<IdentifierNameSyntax>().Any())
                {
                    return;
                }
            }

            var enumType = declarationExpression.BaseList?.Types[0].Type;
            string keyword;
            if (enumType == null)
            {
                keyword = "int";
            }
            else
            {
                var x = context.SemanticModel.GetTypeInfo(enumType);
                keyword = x.Type.Name;
            }

            // We have to make sure that by moving to powers of two, we won't exceed the type's maximum value 
            // For example: 255 is the last possible value for a byte enum
            if (IsOutsideOfRange(keyword, enumMemberDeclarations.Count))
            {
                context.ReportDiagnostic(Diagnostic.Create(ValuesDontFitRule,
                    declarationExpression.Identifier.GetLocation(),
                    enumName, keyword.ToLower()));
                return;
            }

            foreach (var member in enumMemberDeclarations)
            {
                // member doesn't have defined value - "foo" instead of "foo = 4"
                if (member.EqualsValue == null)
                {
                    reportDiagnostic();
                    return;
                }

                if (member.EqualsValue.Value is BinaryExpressionSyntax)
                {
                    var descendantNodes = member.EqualsValue.Value.DescendantNodesAndSelf().ToList();
                    if (descendantNodes.Any() &&
                        descendantNodes.All(n => n is IdentifierNameSyntax || n is BinaryExpressionSyntax))
                    {
                        continue;
                    }
                }

                var symbol = context.SemanticModel.GetDeclaredSymbol(member);
                var value = symbol.ConstantValue;

                switch (value.GetType().Name)
                {
                    case nameof(Int16):
                        if (!IsPowerOfTwo((short) value))
                        {
                            reportDiagnostic();
                            return;
                        }
                        break;
                    case nameof(UInt16):
                        if (!IsPowerOfTwo((ushort) value))
                        {
                            reportDiagnostic();
                            return;
                        }
                        break;
                    case nameof(Int32):
                        if (!IsPowerOfTwo((int) value))
                        {
                            reportDiagnostic();
                            return;
                        }
                        break;
                    case nameof(UInt32):
                        if (!IsPowerOfTwo((uint) value))
                        {
                            reportDiagnostic();
                            return;
                        }
                        break;
                    case nameof(Int64):
                        if (!IsPowerOfTwo((long) value))
                        {
                            reportDiagnostic();
                            return;
                        }
                        break;
                    case nameof(UInt64):
                        if (!IsPowerOfTwo((ulong) value))
                        {
                            reportDiagnostic();
                            return;
                        }
                        break;
                    case nameof(Byte):
                        if (!IsPowerOfTwo((byte) value))
                        {
                            reportDiagnostic();
                            return;
                        }
                        break;
                    case nameof(SByte):
                        if (!IsPowerOfTwo((sbyte) value))
                        {
                            reportDiagnostic();
                            return;
                        }
                        break;
                    default:
                        throw new ArgumentException("This enum-backing type is not supported.");
                }
            }
        }

        /// <summary>
        /// Determines whether a given value is a power of two
        /// </summary>
        /// <param name="value">The value to check</param>
        /// <returns></returns>
        private bool IsPowerOfTwo(double value)
        {
            var logValue = Math.Log(value, 2);
            return value == 0 || logValue - Math.Round(logValue) == 0;
        }

        /// <summary>
        /// Returns whether or not all values can be changed to powers of two without introducing out of range values.
        /// </summary>
        /// <param name="keyword">The type keyword that forms the base type of the enum</param>
        /// <param name="amountOfMembers">// Indicates how many values an enum of this type can have</param>
        /// <returns></returns>
        private bool IsOutsideOfRange(string keyword, int amountOfMembers)
        {
            Func<int, bool> exceedsRange = (int amountAllowed) => amountOfMembers > amountAllowed;

            switch (keyword)
            {
                case nameof(SByte):

                    if (exceedsRange(8))
                    {
                        return true;
                    }
                    break;


                case nameof(Byte):
                    if (exceedsRange(9))
                    {
                        return true;
                    }
                    break;

                default:
                    return false;
                //throw new ArgumentException("Unsupported base enum type encountered");
            }

            return false;
        }
    }
}