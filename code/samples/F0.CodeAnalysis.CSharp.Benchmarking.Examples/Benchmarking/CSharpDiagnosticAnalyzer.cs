using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace F0.CodeAnalysis.CSharp.Examples.Benchmarking;

// https://github.com/dotnet/roslyn-sdk/blob/cd89910e5a9d3da3ee5e326618024c4aba68ff11/src/VisualStudio.Roslyn.SDK/Roslyn.SDK/ProjectTemplates/CSharp/Diagnostic/Analyzer/DiagnosticAnalyzer.cs
[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpDiagnosticAnalyzer : DiagnosticAnalyzer
{
	public const string DiagnosticId = "ID0001";

	private static readonly DiagnosticDescriptor Rule = new(
		DiagnosticId,
		"Type name contains lowercase letters",
		"Type name '{0}' contains lowercase letters",
		"Naming",
		DiagnosticSeverity.Warning,
		true,
		"Type names should be all uppercase."
	);

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
	}

	private static void AnalyzeSymbol(SymbolAnalysisContext context)
	{
		Debug.Assert(context.Symbol is INamedTypeSymbol);
		var namedTypeSymbol = Unsafe.As<INamedTypeSymbol>(context.Symbol);

		if (namedTypeSymbol.Name.ToCharArray().Any(Char.IsLower))
		{
			var diagnostic = Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], namedTypeSymbol.Name);

			context.ReportDiagnostic(diagnostic);
		}
	}
}
