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
		internal static readonly DiagnosticDescriptor TypeAccessModifierRule =
			new("AF0001", "Type access modifier break",
				"Please do not use public access modifier for classes, structs and records",
				"Analyzers",
				DiagnosticSeverity.Warning, isEnabledByDefault: true);

		internal static readonly DiagnosticDescriptor InterfaceAccessModifierRule =
			new("AF0002", "Interface access modifier break",
				"Please use public access modifier for interfaces",
				"Analyzers",
				DiagnosticSeverity.Warning, isEnabledByDefault: true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
			= ImmutableArray.Create(TypeAccessModifierRule, InterfaceAccessModifierRule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(AnalyzeTypeDeclarationNode, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.RecordDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzeInterfaceDeclarationNode, SyntaxKind.InterfaceDeclaration);
		}

		private static Accessibility? GetAccessibility(SyntaxNodeAnalysisContext context) => context.ContainingSymbol?.DeclaredAccessibility;

		private void AnalyzeTypeDeclarationNode(SyntaxNodeAnalysisContext context)
		{
			if (GetAccessibility(context) == Accessibility.Public)
			{
				var typeSyntax = (TypeDeclarationSyntax)context.Node;
				context.ReportDiagnostic(Diagnostic.Create(TypeAccessModifierRule, typeSyntax.Identifier.GetLocation()));
			}
		}

		private void AnalyzeInterfaceDeclarationNode(SyntaxNodeAnalysisContext context)
		{
			if (GetAccessibility(context) != Accessibility.Public)
			{
				var syntax = (InterfaceDeclarationSyntax)context.Node;
				context.ReportDiagnostic(Diagnostic.Create(InterfaceAccessModifierRule, syntax.Identifier.GetLocation()));
			}
		}
	}
}
