using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace F0.CodeAnalysis.CSharp.Tests.Benchmarking;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class TestCSharpDiagnosticSuppressor : DiagnosticSuppressor
{
	public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => ImmutableArray.Create<SuppressionDescriptor>();

	public override void ReportSuppressions(SuppressionAnalysisContext context)
	{
	}
}
