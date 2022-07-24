using System.Collections.ObjectModel;
using F0.CodeAnalysis.CSharp.Diagnostics;

namespace F0.CodeAnalysis.CSharp.Benchmarking;

public sealed class CSharpIncrementalGeneratorBenchmarkInspectionContext
{
	public (string HintName, string Source)? Source { get; init; }
	public ICollection<(string HintName, string Source)> AdditionalSources { get; init; } = new Collection<(string HintName, string Source)>();
	public ICollection<AdhocDiagnostic> Diagnostics { get; init; } = new Collection<AdhocDiagnostic>();
}
