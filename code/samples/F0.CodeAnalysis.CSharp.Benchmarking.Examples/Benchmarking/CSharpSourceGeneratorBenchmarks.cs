using System.Collections.ObjectModel;
using F0.CodeAnalysis.CSharp.Benchmarking;
using Microsoft.CodeAnalysis.CSharp;

namespace F0.CodeAnalysis.CSharp.Examples.Benchmarking;

public class CSharpSourceGeneratorBenchmarks
{
	private readonly CSharpSourceGeneratorBenchmark<CSharpSourceGenerator> benchmark = new();

	[GlobalSetup]
	public void GlobalSetup()
	{
		string source = @"namespace Benchmarking;

public sealed class CSharpSourceGeneratorBenchmarks
{
}

public readonly struct GeneratorInitializationContext
{
}";

		List<string> additionalSources = new(10_000);
		for (int i = 0; i < additionalSources.Capacity; i++)
		{
			string additionalSource = $@"namespace Benchmarking;

public sealed class CSharpSourceGeneratorBenchmarks{i}
{{
}}

public readonly struct GeneratorInitializationContext{i}
{{
}}
";
			additionalSources.Add(additionalSource);
		}

		CSharpSourceGeneratorBenchmarkInitializationContext context = new()
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
		CSharpSourceGeneratorBenchmarkInspectionContext context = new()
		{
			Source = ($"{LanguageVersion.CSharp10}.g.cs", CSharpSourceGenerator.CSharp10),
			AdditionalSources = { ("Additional.g.cs", "// # of classes: 10001") },
		};

		benchmark.Inspect(context);
	}
}
