using System.Collections.Immutable;
using F0.CodeAnalysis.CSharp.Diffing;
using Microsoft.CodeAnalysis;

namespace F0.CodeAnalysis.CSharp.Inspection;

internal static class GeneratorInspector
{
	internal static void Generator(ISourceGenerator expected, ISourceGenerator actual)
	{
		if (expected != actual)
		{
			BenchmarkInspectionException.Throw("Unexpected Generator:", expected.GetType(), actual.GetType());
		}
	}

	internal static void GeneratedSources(int expected, ImmutableArray<GeneratedSourceResult> actual)
	{
		if (expected != actual.Length)
		{
			IEnumerable<string> messages = actual.Select(static generatedSource => "   - " + generatedSource.HintName);
			string generatedSources = String.Join(Environment.NewLine, messages);
			BenchmarkInspectionException.Throw("Unexpected number of diagnostics:", expected, actual.Length, generatedSources);
		}
	}

	internal static void Source(int index, (string HintName, string Source) expected, GeneratedSourceResult actual)
	{
		string source = actual.SourceText.ToString();
		if (!actual.HintName.Equals(expected.HintName, StringComparison.Ordinal))
		{
			BenchmarkInspectionException.Throw($"Expected and actual hint name of source #{index} differ: ", expected.HintName, actual.HintName);
		}

		if (!source.Equals(expected.Source, StringComparison.Ordinal))
		{
			string diff = Diff.GetDiff(expected.Source, source);
			string message = $"Expected and actual source text of source #{index} differ: " + Environment.NewLine + diff;
			BenchmarkInspectionException.Throw(message);
		}
	}
}
