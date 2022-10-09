using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace F0.CodeAnalysis.CSharp.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpDiagnosticAnalyzerReporter : DiagnosticAnalyzer
{
	private readonly ImmutableArray<Diagnostic> diagnostics;

	public CSharpDiagnosticAnalyzerReporter(ImmutableArray<Diagnostic> diagnostics)
	{
		this.diagnostics = diagnostics;

		SupportedDiagnostics = diagnostics
			.Select(static (Diagnostic diagnostic) => diagnostic.Descriptor)
			.ToImmutableArray();
	}

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterCompilationAction(AnalyzeCompilation);
	}

	private void AnalyzeCompilation(CompilationAnalysisContext context)
	{
		foreach (Diagnostic diagnostic in diagnostics)
		{
			context.ReportDiagnostic(diagnostic);
		}
	}
}
