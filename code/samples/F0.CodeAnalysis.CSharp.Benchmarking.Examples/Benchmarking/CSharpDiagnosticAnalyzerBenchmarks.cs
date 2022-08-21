using System.Collections.ObjectModel;
using F0.CodeAnalysis.CSharp.Benchmarking;
using F0.CodeAnalysis.CSharp.Diagnostics;
using Microsoft.CodeAnalysis;

namespace F0.CodeAnalysis.CSharp.Examples.Benchmarking;

public class CSharpDiagnosticAnalyzerBenchmarks
{
	private readonly CSharpDiagnosticAnalyzerBenchmark<CSharpDiagnosticAnalyzer> benchmark = new();

	[GlobalSetup]
	public void GlobalSetup()
	{
		List<string> sources = new(1_000);
		for (int i = 0; i < sources.Capacity; i++)
		{
			string source = $@"using System;

namespace Benchmarking;

public sealed class {{|#0:TypeName{i}|}}
{{
}}

public readonly struct TYPENAME{i}
{{
}}";

			sources.Add(source);
		}

		CSharpDiagnosticAnalyzerBenchmarkInitializationContext context = new()
		{
			AdditionalSources = new Collection<string>(sources),
		};

		benchmark.Initialize(context);
	}

	[Benchmark]
	public Task Benchmark()
		=> benchmark.InvokeAsync();

	[GlobalCleanup]
	public Task GlobalCleanup()
	{
		List<AdhocDiagnostic> diagnostics = new(1_000);
		for (int i = 0; i < diagnostics.Capacity; i++)
		{
			diagnostics.Add(new AdhocDiagnostic(i)
			{
				Id = CSharpDiagnosticAnalyzer.DiagnosticId,
				Category = "Naming",
				Message = $"Type name 'TypeName{i}' contains lowercase letters",
				MessageFormat = "Type name '{0}' contains lowercase letters",
				Severity = DiagnosticSeverity.Warning,
				DefaultSeverity = DiagnosticSeverity.Warning,
				IsEnabledByDefault = true,
				Title = "Type name contains lowercase letters",
				Description = "Type names should be all uppercase.",
			});
		}

		CSharpDiagnosticAnalyzerBenchmarkInspectionContext context = new()
		{
			Diagnostics = new Collection<AdhocDiagnostic>(diagnostics),
		};

		return benchmark.InspectAsync(context);
	}
}
