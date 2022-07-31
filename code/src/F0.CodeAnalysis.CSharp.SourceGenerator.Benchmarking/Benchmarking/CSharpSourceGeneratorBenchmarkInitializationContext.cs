using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace F0.CodeAnalysis.CSharp.Benchmarking;

public sealed class CSharpSourceGeneratorBenchmarkInitializationContext
{
	public string? Source { get; init; }
	public ICollection<string> AdditionalSources { get; init; } = new Collection<string>();
	public ICollection<(string Path, string Text)> AdditionalTexts { get; init; } = new Collection<(string Path, string Text)>();
	public CSharpParseOptions? ParseOptions { get; init; }
	public CSharpCompilationOptions? CompilationOptions { get; init; }
	public IEnumerable<MetadataReference>? MetadataReferences { get; init; }
	public ICollection<(string Key, string Value)> AnalyzerConfigOptions { get; init; } = new Collection<(string Key, string Value)>();
}
