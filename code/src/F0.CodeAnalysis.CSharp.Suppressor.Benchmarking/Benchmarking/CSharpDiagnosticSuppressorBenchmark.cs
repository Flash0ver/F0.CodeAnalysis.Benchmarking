using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace F0.CodeAnalysis.CSharp.Benchmarking;

public sealed class CSharpDiagnosticSuppressorBenchmark<TDiagnosticSuppressor>
	where TDiagnosticSuppressor : DiagnosticSuppressor, new()
{
	public CSharpDiagnosticSuppressorBenchmark()
	{
		Suppressor = new TDiagnosticSuppressor();
	}

	internal TDiagnosticSuppressor Suppressor { get; }

	public void Initialize(CSharpDiagnosticSuppressorBenchmarkInitializationContext context)
	{
	}

	public object Invoke()
	{
		return null!;
	}

	public void Inspect(CSharpDiagnosticSuppressorBenchmarkInspectionContext context)
	{
	}
}
