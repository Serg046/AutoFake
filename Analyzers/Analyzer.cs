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
			new("AF0001", "Type access modifier violation",
				"Do not use public access modifier for classes, structs and records",
				"Analyzers",
				DiagnosticSeverity.Warning, isEnabledByDefault: true);

		internal static readonly DiagnosticDescriptor InterfaceAccessModifierRule =
			new("AF0002", "Interface access modifier violation",
				"Use public access modifier for interfaces",
				"Analyzers",
				DiagnosticSeverity.Warning, isEnabledByDefault: true);

		internal static readonly DiagnosticDescriptor DIRule =
			new("AF0003", "DI-container registration violation",
				"Register an interface instead of the concrete implementation",
				"Analyzers",
				DiagnosticSeverity.Warning, isEnabledByDefault: true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
			= ImmutableArray.Create(TypeAccessModifierRule, InterfaceAccessModifierRule, DIRule);

		public override void Initialize(AnalysisContext context)
		{
			context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.EnableConcurrentExecution();
			context.RegisterSyntaxNodeAction(AnalyzeTypeDeclarationNode, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.RecordDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzeInterfaceDeclarationNode, SyntaxKind.InterfaceDeclaration);
			context.RegisterSyntaxNodeAction(AnalyzeInvocationNode, SyntaxKind.InvocationExpression);
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
				var interfaceSyntax = (InterfaceDeclarationSyntax)context.Node;
				context.ReportDiagnostic(Diagnostic.Create(InterfaceAccessModifierRule, interfaceSyntax.Identifier.GetLocation()));
			}
		}

		private void AnalyzeInvocationNode(SyntaxNodeAnalysisContext context)
		{
			if (context.SemanticModel.GetSymbolInfo(context.Node).Symbol is IMethodSymbol methodSymbol
				&& methodSymbol.ContainingType.ToString() == "DryIoc.Registrator"
				&& (methodSymbol.Name.StartsWith("Register") || methodSymbol.Name == "Use"))
			{
				ProcessRegistrationMethodSymbol(context, methodSymbol);
			}
		}

		private void ProcessRegistrationMethodSymbol(SyntaxNodeAnalysisContext context, IMethodSymbol methodSymbol)
		{
			var invocation = (InvocationExpressionSyntax)context.Node;
			if (methodSymbol.IsGenericMethod && methodSymbol.TypeArguments.Length > 0)
			{
				var genericArgument = methodSymbol.TypeArguments[0];
				AnalyzeType(context, invocation, genericArgument);
			}
			else if (invocation.ArgumentList.Arguments.Count > 0
				&& invocation.ArgumentList.Arguments[0].Expression is TypeOfExpressionSyntax typeOfSyntax
				&& context.SemanticModel.GetSymbolInfo(typeOfSyntax.Type).Symbol is ITypeSymbol typeOfArgument)
			{
				AnalyzeType(context, invocation, typeOfArgument);
			}
		}

		private void AnalyzeType(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation, ITypeSymbol typeSymbol)
		{
			if (typeSymbol.TypeKind == TypeKind.Delegate && typeSymbol is INamedTypeSymbol nts && nts.DelegateInvokeMethod != null)
			{
				AnalyzeType(context, invocation, nts.DelegateInvokeMethod.ReturnType);
			}
			else if (typeSymbol.TypeKind != TypeKind.Interface)
			{
				var syntax = invocation.FirstAncestorOrSelf<ExpressionStatementSyntax>() ?? (CSharpSyntaxNode)invocation;
				context.ReportDiagnostic(Diagnostic.Create(DIRule, syntax.GetLocation()));
			}
		}
	}
}
