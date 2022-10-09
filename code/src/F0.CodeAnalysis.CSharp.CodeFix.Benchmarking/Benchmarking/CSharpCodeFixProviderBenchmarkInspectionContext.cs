using System.Collections.ObjectModel;
using F0.CodeAnalysis.CSharp.Diagnostics;

namespace F0.CodeAnalysis.CSharp.Benchmarking;

public sealed class CSharpCodeFixProviderBenchmarkInspectionContext
{
	public string? Source { get; init; }
	public ICollection<string> AdditionalSources { get; init; } = new Collection<string>();
	public ICollection<AdhocDiagnostic> Diagnostics { get; init; } = new Collection<AdhocDiagnostic>();
}
