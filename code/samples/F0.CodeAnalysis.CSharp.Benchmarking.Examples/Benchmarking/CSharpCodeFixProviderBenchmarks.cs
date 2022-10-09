using F0.CodeAnalysis.CSharp.Benchmarking;
using F0.CodeAnalysis.CSharp.Diagnostics;

namespace F0.CodeAnalysis.CSharp.Examples.Benchmarking;

public class CSharpCodeFixProviderBenchmarks
{
	private readonly CSharpCodeFixProviderBenchmark<CSharpCodeFixProvider> benchmark = new();

	[GlobalSetup]
	public Task GlobalSetup()
	{
		string source = @"
using System;

namespace Benchmarking
{
	public sealed class TypeName
	{
		int {|#0:myField|};
	}

	public readonly struct NAME
	{
		int {|#1:MyProperty|} { get; set; }
	}
}
";

		List<string> sources = new(300);
		for (int i = 0; i < sources.Capacity; i++)
		{
			string text = $@"
using System;

namespace Benchmarking
{{
	public sealed class TypeName{i}
	{{
	}}

	public readonly struct NAME{i}
	{{
	}}
}}
";

			sources.Add(text);
		}

		CSharpCodeFixProviderBenchmarkInitializationContext context = new()
		{
			Source = source,
			AdditionalSources = sources,
			Diagnostics =
			{
				new AdhocDiagnostic(0, CSharpDiagnosticAnalyzer.DiagnosticId, CSharpDiagnosticAnalyzer.DiagnosticCategory),
				new AdhocDiagnostic(1, CSharpDiagnosticAnalyzer.DiagnosticId, CSharpDiagnosticAnalyzer.DiagnosticCategory),
			},
			Analyzers = { new CSharpDiagnosticAnalyzer() },
		};

		return benchmark.InitializeAsync(context);
	}

	[Benchmark]
	public Task Benchmark()
		=> benchmark.InvokeAsync();

	[GlobalCleanup]
	public Task GlobalCleanup()
	{
		string fixedSource = @"
using System;

namespace Benchmarking
{
	public sealed class TYPENAME
	{
	}

	public readonly struct NAME
	{
	}
}
";

		List<string> fixedTexts = new(300);
		for (int i = 0; i < fixedTexts.Capacity; i++)
		{
			string text = $@"
using System;

namespace Benchmarking
{{
	public sealed class TYPENAME{i}
	{{
	}}

	public readonly struct NAME{i}
	{{
	}}
}}
";

			fixedTexts.Add(text);
		}

		CSharpCodeFixProviderBenchmarkInspectionContext context = new()
		{
			Source = fixedSource,
			AdditionalSources = fixedTexts,
		};

		return benchmark.InspectAsync(context);
	}
}
