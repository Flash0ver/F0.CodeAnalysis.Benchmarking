using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using F0.CodeAnalysis.CSharp.Collections.Generic;
using F0.CodeAnalysis.CSharp.Diagnostics;
using F0.CodeAnalysis.CSharp.Inspection;
using F0.CodeAnalysis.CSharp.Markup;
using F0.CodeAnalysis.CSharp.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace F0.CodeAnalysis.CSharp.Benchmarking;

public sealed class CSharpDiagnosticAnalyzerBenchmark<TDiagnosticAnalyzer>
	where TDiagnosticAnalyzer : DiagnosticAnalyzer, new()
{
	private readonly ImmutableArray<DiagnosticAnalyzer> analyzers;
	private Compilation compilation;
	private AnalyzerOptions? options;
	private ImmutableArray<Location> locations;
	private ImmutableArray<string> additionalPaths;

	public CSharpDiagnosticAnalyzerBenchmark()
	{
		Analyzer = new TDiagnosticAnalyzer();
		analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(Analyzer);

		compilation = null!;
	}

	internal TDiagnosticAnalyzer Analyzer { get; }

	public void Initialize(CSharpDiagnosticAnalyzerBenchmarkInitializationContext context)
	{
		_ = context ?? throw new ArgumentNullException(nameof(context));

		AnalyzerConfigOptionsProvider? optionsProvider = context.AnalyzerConfigOptions.Count > 0 ? new AdhocAnalyzerConfigOptionsProvider(context.AnalyzerConfigOptions) : null;

		compilation = CreateCompilation(context, out ImmutableArray<AdditionalText> additionalTexts, out locations);
		options = CreateAnalyzerOptions(additionalTexts, optionsProvider);
		additionalPaths = context.AdditionalTexts.Select(static additionalText => additionalText.Path).ToImmutableArray();
	}

	public async Task InvokeAsync()
	{
		CompilationWithAnalyzers compilationWithAnalyzers = compilation.WithAnalyzers(analyzers, options, CancellationToken.None);

		_ = await compilationWithAnalyzers.GetAnalysisResultAsync(CancellationToken.None).ConfigureAwait(false);
	}

	public async Task InspectAsync(CSharpDiagnosticAnalyzerBenchmarkInspectionContext context)
	{
		_ = context ?? throw new ArgumentNullException(nameof(context));
		context.Diagnostics.WithLocations(locations);

		CompilationWithAnalyzers compilationWithAnalyzers = compilation.WithAnalyzers(analyzers, options, CancellationToken.None);

		AnalysisResult result = await compilationWithAnalyzers.GetAnalysisResultAsync(CancellationToken.None).ConfigureAwait(false);

		ImmutableArray<Diagnostic> diagnostics = OrderDiagnostics(result.GetAllDiagnostics());
		Inspector.Diagnostics(context.Diagnostics, diagnostics);
	}

	private static AnalyzerOptions? CreateAnalyzerOptions(ImmutableArray<AdditionalText> additionalFiles, AnalyzerConfigOptionsProvider? optionsProvider)
	{
		return optionsProvider is null
			? additionalFiles.IsDefaultOrEmpty
				? null
				: new AnalyzerOptions(additionalFiles)
			: new AnalyzerOptions(additionalFiles, optionsProvider);
	}

	private ImmutableArray<Diagnostic> OrderDiagnostics(ImmutableArray<Diagnostic> diagnostics)
	{
		List<Diagnostic> sorted = new(diagnostics.Length);

		foreach (SyntaxTree syntaxTree in compilation.SyntaxTrees)
		{
			IEnumerable<Diagnostic> ordered = diagnostics
				.Where((Diagnostic diagnostic) =>
				{
					if (diagnostic.Location.Kind == LocationKind.SourceFile)
					{
						Debug.Assert(diagnostic.Location.SourceTree is not null, $"Expected {nameof(SyntaxTree)} not to be <null>.");
						return diagnostic.Location.SourceTree == syntaxTree;
					}

					return false;
				})
				.OrderBy(static (Diagnostic diagnostic) => diagnostic.Location.SourceSpan);

			sorted.AddRange(ordered);
		}

		foreach (string additionalPath in additionalPaths)
		{
			IEnumerable<Diagnostic> additional = diagnostics
				.Where((Diagnostic diagnostic) =>
				{
					if (diagnostic.Location.Kind == LocationKind.ExternalFile)
					{
						Debug.Assert(diagnostic.Location.SourceTree is null, $"Expected {nameof(SyntaxTree)} to be <null>.");
						FileLinePositionSpan lineSpan = diagnostic.Location.GetLineSpan();
						return lineSpan.Path.Equals(additionalPath, StringComparison.Ordinal);
					}

					return false;
				})
				.OrderBy(static (Diagnostic diagnostic) => diagnostic.Location.SourceSpan);

			sorted.AddRange(additional);
		}

		Debug.Assert(sorted.Count == diagnostics.Length, $"Sorted {sorted.Count} diagnostics, but ordered a sequence of {diagnostics.Length} diagnostics.");
		Debug.Assert(sorted.Intersect(diagnostics).Count() == diagnostics.Length, $"Sorted '{String.Join(",", sorted.Select(static diagnostic => diagnostic.Id))}' diagnostics, but ordered the diagnostics '{String.Join(",", diagnostics.Select(static diagnostic => diagnostic.Id))}'.");

		return sorted.ToImmutableArray();
	}

	private static Compilation CreateCompilation(CSharpDiagnosticAnalyzerBenchmarkInitializationContext context, out ImmutableArray<AdditionalText> additionalFiles, out ImmutableArray<Location> locations)
	{
		List<Location> outLocations = new();

		IEnumerable<string> sources = context.Source.ToEnumerable().Concat(context.AdditionalSources);
		IEnumerable<string> parsed = sources.Select((source, index) =>
		{
			string sanitized = MarkupParser.Parse(CreateFilePath(index), source, out ImmutableArray<Location> locations);
			outLocations.AddRange(locations);
			return sanitized;
		});
		IEnumerable<(string Path, string Source)> addionalTexts = context.AdditionalTexts.Select((source, index) =>
		{
			string sanitized = MarkupParser.Parse(CreateFilePath(index), source.Text, out ImmutableArray<Location> locations);
			outLocations.AddRange(locations);
			return (source.Path, sanitized);
		});

		const string assemblyName = "CompilerGeneratedCompilation";
		IEnumerable<SyntaxTree> syntaxTrees = parsed.Select(source => CSharpSyntaxTree.ParseText(source, context.ParseOptions));
		IEnumerable<MetadataReference> references = context.MetadataReferences ?? new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) };
		CSharpCompilationOptions options = context.CompilationOptions ?? new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

		var compilation = CSharpCompilation.Create(assemblyName, syntaxTrees.ToArray(), references, options);
		additionalFiles = addionalTexts.Select(static add => new AdhocAdditionalText(add.Path, add.Source)).ToImmutableArray<AdditionalText>();
		locations = ImmutableArray.CreateRange(outLocations);
		return compilation;
	}

	private static string CreateFilePath(int index)
	{
		const string fileName = "Benchmark";
		const string fileExtension = ".cs";

		return $"/0/{fileName}{index}{fileExtension}";
	}
}
