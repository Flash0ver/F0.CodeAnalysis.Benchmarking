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

public sealed class CSharpIncrementalGeneratorBenchmark<TIncrementalGenerator>
	where TIncrementalGenerator : IIncrementalGenerator, new()
{
	private readonly ISourceGenerator[] generators;
	private GeneratorDriver driver;
	private Compilation inputCompilation;
	private ImmutableArray<Location> locations;

	public CSharpIncrementalGeneratorBenchmark()
	{
		Generator = new TIncrementalGenerator();
		generators = new ISourceGenerator[] { Generator.AsSourceGenerator() };

		driver = null!;
		inputCompilation = null!;
	}

	internal TIncrementalGenerator Generator { get; }

	public void Initialize(CSharpIncrementalGeneratorBenchmarkInitializationContext context)
	{
		_ = context ?? throw new ArgumentNullException(nameof(context));

		IEnumerable<AdditionalText>? additionalTexts = context.AdditionalTexts.Select(static additionalText => new AdhocAdditionalText(additionalText.Path, additionalText.Text));
		CSharpParseOptions? parseOptions = context.ParseOptions;
		AnalyzerConfigOptionsProvider? optionsProvider = context.AnalyzerConfigOptions.Count > 0 ? new AdhocAnalyzerConfigOptionsProvider(context.AnalyzerConfigOptions) : null;
		GeneratorDriverOptions driverOptions = context.DriverOptions;

		driver = CSharpGeneratorDriver.Create(generators, additionalTexts, parseOptions, optionsProvider, driverOptions);
		inputCompilation = CreateCompilation(context, out locations);
	}

	public object Invoke()
	{
		_ = driver.RunGenerators(inputCompilation, CancellationToken.None);

		return null!;
	}

	public object InvokeWithMemoization()
	{
		driver = driver.RunGenerators(inputCompilation, CancellationToken.None);

		return null!;
	}

	public void Inspect(CSharpIncrementalGeneratorBenchmarkInspectionContext context)
	{
		_ = context ?? throw new ArgumentNullException(nameof(context));
		context.Diagnostics.WithLocations(locations);

		(string HintName, string Source)[] sources = context.Source.ToEnumerable().Concat(context.AdditionalSources).ToArray();

		GeneratorDriver newDriver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics, CancellationToken.None);

		Inspector.Diagnostics(context.Diagnostics, diagnostics);

		Inspector.SyntaxTrees(inputCompilation.SyntaxTrees.Count() + sources.Length, outputCompilation.SyntaxTrees);
		Inspector.Diagnostics(AdhocDiagnostics.Empty, outputCompilation.GetDiagnostics());

		GeneratorDriverRunResult runResult = newDriver.GetRunResult();

		Inspector.SyntaxTrees(sources.Length, runResult.GeneratedTrees);
		Inspector.Diagnostics(context.Diagnostics, runResult.Diagnostics);

		Debug.Assert(runResult.Results.Length == 1, "There Can Be Only One (Generator Result)");
		Debug.Assert(generators.Length == 1, "There Can Be Only One (Generator)");
		GeneratorRunResult generatorResult = runResult.Results[0];
		GeneratorInspector.Generator(generators[0], generatorResult.Generator);
		Inspector.Diagnostics(context.Diagnostics, generatorResult.Diagnostics);
		GeneratorInspector.GeneratedSources(sources.Length, generatorResult.GeneratedSources);
		Inspector.Exception(generatorResult.Exception);

		for (int i = 0; i < sources.Length; i++)
		{
			GeneratorInspector.Source(i, sources[i], generatorResult.GeneratedSources[i]);
		}
	}

	private static Compilation CreateCompilation(CSharpIncrementalGeneratorBenchmarkInitializationContext context, out ImmutableArray<Location> locations)
	{
		List<Location> outLocations = new();

		IEnumerable<string> sources = context.Source.ToEnumerable().Concat(context.AdditionalSources);
		IEnumerable<string> parsed = sources.Select((source, index) =>
		{
			string sanitized = MarkupParser.Parse(CreateFilePath(index), source, out ImmutableArray<Location> locations);
			outLocations.AddRange(locations);
			return sanitized;
		});

		const string assemblyName = "CompilerGeneratedCompilation";
		IEnumerable<SyntaxTree> syntaxTrees = parsed.Select(source => CSharpSyntaxTree.ParseText(source, context.ParseOptions));
		IEnumerable<MetadataReference> references = context.MetadataReferences ?? new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) };
		CSharpCompilationOptions options = context.CompilationOptions ?? new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

		var compilation = CSharpCompilation.Create(assemblyName, syntaxTrees.ToArray(), references, options);
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
