using System.Collections.ObjectModel;
using F0.CodeAnalysis.CSharp.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace F0.CodeAnalysis.CSharp.Benchmarking;

public sealed class CSharpCodeFixProviderBenchmarkInitializationContext
{
	public string? Source { get; init; }
	public ICollection<string> AdditionalSources { get; init; } = new Collection<string>();
	public ICollection<(string Path, string Text)> AdditionalTexts { get; init; } = new Collection<(string Path, string Text)>();
	public CSharpParseOptions? ParseOptions { get; init; }
	public CSharpCompilationOptions? CompilationOptions { get; init; }
	public IEnumerable<MetadataReference>? MetadataReferences { get; init; }
	public ICollection<(string Key, string Value)> AnalyzerConfigOptions { get; init; } = new Collection<(string Key, string Value)>();

	public ICollection<AdhocDiagnostic> Diagnostics { get; init; } = new Collection<AdhocDiagnostic>();
	public ICollection<DiagnosticAnalyzer> Analyzers { get; init; } = new Collection<DiagnosticAnalyzer>();
}
