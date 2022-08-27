using System.Collections.ObjectModel;
using F0.CodeAnalysis.CSharp.Diagnostics;

namespace F0.CodeAnalysis.CSharp.Benchmarking;

public sealed class CSharpDiagnosticAnalyzerBenchmarkInspectionContext
{
	public ICollection<AdhocDiagnostic> Diagnostics { get; init; } = new Collection<AdhocDiagnostic>();
}
