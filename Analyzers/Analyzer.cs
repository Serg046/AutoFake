using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class Analyzer : DiagnosticAnalyzer
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage("MicrosoftCodeAnalysisReleaseTracking", "RS2008:Enable analyzer release tracking", Justification = "No need")]
		internal static readonly DiagnosticDescriptor AccessModifierRule =
			new("AF0001", "Access modifier break",
				"Please do not use public access modifier for classes, structs and records",
				"Analyzers",
				DiagnosticSeverity.Warning, isEnabledByDefault: true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
			= ImmutableArray.Create(AccessModifierRule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(AnalyzeTypeDeclarationNode, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.RecordDeclaration);
		}

		private void AnalyzeTypeDeclarationNode(SyntaxNodeAnalysisContext context)
		{
			if (context.ContainingSymbol is ITypeSymbol { DeclaredAccessibility: Accessibility.Public } typeSymbol
				&& !IsInheritedFrom(typeSymbol, nameof(Exception)))
			{
				var typeSyntax = (TypeDeclarationSyntax)context.Node;
				context.ReportDiagnostic(Diagnostic.Create(AccessModifierRule, typeSyntax.Identifier.GetLocation()));
			}
		}

		private bool IsInheritedFrom(ITypeSymbol type, string baseTypeName)
		{
			while (type.BaseType != null)
			{
				type = type.BaseType;
				if (type.Name == baseTypeName) return true;
			}

			return false;
		}
	}
}
