using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace F0.CodeAnalysis.CSharp.Benchmarking;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class NullCSharpDiagnosticSuppressor : DiagnosticSuppressor
{
	public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray<SuppressionDescriptor>.Empty;

	public override void ReportSuppressions(SuppressionAnalysisContext context)
	{
	}
}
