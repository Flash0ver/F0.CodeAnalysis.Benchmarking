using System.Collections.ObjectModel;
using F0.CodeAnalysis.CSharp.Benchmarking;
using Microsoft.CodeAnalysis.CSharp;

namespace F0.CodeAnalysis.CSharp.Examples.Benchmarking;

public class CSharpIncrementalGeneratorBenchmarks
{
	private readonly CSharpIncrementalGeneratorBenchmark<CSharpIncrementalGenerator> benchmark = new();

	[GlobalSetup]
	public void GlobalSetup()
	{
		string source = @"namespace Benchmarking;

public sealed class CSharpIncrementalGeneratorBenchmarks
{
}

public readonly struct IncrementalGeneratorInitializationContext
{
}";

		List<string> additionalSources = new(10_000);
		for (int i = 0; i < additionalSources.Capacity; i++)
		{
			string additionalSource = $@"namespace Benchmarking;

public sealed class CSharpIncrementalGeneratorBenchmarks{i}
{{
}}

public readonly struct IncrementalGeneratorInitializationContext{i}
{{
}}
";
			additionalSources.Add(additionalSource);
		}

		CSharpIncrementalGeneratorBenchmarkInitializationContext context = new()
		{
			Source = source,
			AdditionalSources = new Collection<string>(additionalSources),
			ParseOptions = new CSharpParseOptions(LanguageVersion.CSharp10),
		};

		benchmark.Initialize(context);
	}

	[Benchmark]
	public object Benchmark()
		=> benchmark.Invoke();

	[GlobalCleanup]
	public void GlobalCleanup()
	{
		CSharpIncrementalGeneratorBenchmarkInspectionContext context = new()
		{
			Source = ($"{LanguageVersion.CSharp10}.g.cs", CSharpIncrementalGenerator.CSharp10),
			AdditionalSources = { ("Additional.g.cs", "// # of classes: 10001") },
		};

		benchmark.Inspect(context);
	}
}
