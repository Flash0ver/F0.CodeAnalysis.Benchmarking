using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using F0.CodeAnalysis.CSharp.Diagnostics;
using F0.CodeAnalysis.CSharp.Diffing;
using Microsoft.CodeAnalysis;

namespace F0.CodeAnalysis.CSharp.Inspection;

internal static class Inspector
{
	internal static void Diagnostics(ImmutableArray<Diagnostic> expected, ImmutableArray<Diagnostic> actual)
	{
		if (expected.Length != actual.Length)
		{
			IEnumerable<string> messages = actual.Select(static diagnostic => "   - " + diagnostic.ToString());
			string diagnostics = String.Join(Environment.NewLine, messages);
			BenchmarkInspectionException.Throw("Unexpected number of diagnostics:", expected.Length, actual.Length, diagnostics);
		}
	}

	internal static void Diagnostics(IEnumerable<AdhocDiagnostic> expected, ImmutableArray<Diagnostic> actual)
	{
		AdhocDiagnostic[] diagnostics = expected.ToArray();
		if (diagnostics.Length != actual.Length)
		{
			StringBuilder diff = new();

			_ = diff.AppendLine("Unexpected number of diagnostics:");
			_ = diff.Append("  Expected: ");
			_ = diff.AppendLine(diagnostics.Length.ToString(NumberFormatInfo.InvariantInfo));
			_ = diff.Append("  Actual:   ");
			_ = diff.AppendLine(actual.Length.ToString(NumberFormatInfo.InvariantInfo));

			if (!actual.IsEmpty)
			{
				_ = diff.AppendLine();
				_ = diff.AppendLine("Unexpected diagnostics:");
			}

			foreach (Diagnostic diagnostic in actual)
			{
				_ = diff.Append("  - ");
				_ = diff.Append(diagnostic.Severity);
				_ = diff.Append(' ');
				_ = diff.Append(diagnostic.Id);
				_ = diff.Append(": ");
				_ = diff.AppendLine(diagnostic.GetMessage(CultureInfo.InvariantCulture));
			}

			BenchmarkInspectionException.Throw(diff.ToString());
		}

		for (int i = 0; i < diagnostics.Length; i++)
		{
			AdhocDiagnostic expectedDiagnostic = diagnostics[i];
			Diagnostic actualDiagnostic = actual[i];

			InspectDiagnostic(expectedDiagnostic, actualDiagnostic, i);
		}
	}

	internal static void SyntaxTrees(int expected, IEnumerable<SyntaxTree> actual)
	{
		SyntaxTree[] actualSyntaxTrees = actual.ToArray();

		if (expected != actualSyntaxTrees.Length)
		{
			BenchmarkInspectionException.Throw("Unexpected number of syntax trees:", expected, actualSyntaxTrees.Length);
		}
	}

	internal static void Source(string expected, string actual)
	{
		if (!actual.Equals(expected, StringComparison.Ordinal))
		{
			string diff = Diff.GetDiff(expected, actual);
			string message = "Expected and actual source text differ: " + Environment.NewLine + diff;
			BenchmarkInspectionException.Throw(message);
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

	internal static void Exception(Exception? exception)
	{
		if (exception is not null)
		{
			BenchmarkInspectionException.Throw($"Unexpected exception: {exception}");
		}
	}

	private static void InspectDiagnostic(AdhocDiagnostic expected, Diagnostic actual, int index)
	{
		StringBuilder message = new($"Unexpected {nameof(Diagnostic)} #{index}:");
		_ = message.AppendLine();

		Debug.Assert(actual.Descriptor.CustomTags is ImmutableArray<string>);

		bool @throw = Diff.WriteDiff(message, nameof(Diagnostic.Id), expected.Id, actual.Id)
			| Diff.WriteDiff(message, nameof(Diagnostic.Descriptor.Category), expected.Category, actual.Descriptor.Category)
			| Diff.WriteDiff(message, nameof(Diagnostic.Descriptor.Title), expected.Title, actual.Descriptor.Title)
			| Diff.WriteDiff(message, nameof(Diagnostic.Descriptor.Description), expected.Description, actual.Descriptor.Description)
			| Diff.WriteDiff(message, nameof(AdhocDiagnostic.Message), expected.Message, actual.GetMessage(null))
			| Diff.WriteDiff(message, nameof(Diagnostic.Descriptor.MessageFormat), expected.MessageFormat, actual.Descriptor.MessageFormat)
			| Diff.WriteDiff(message, nameof(Diagnostic.DefaultSeverity), expected.DefaultSeverity, actual.DefaultSeverity)
			| Diff.WriteDiff(message, nameof(Diagnostic.Severity), expected.Severity, actual.Severity)
			| Diff.WriteDiff(message, nameof(Diagnostic.WarningLevel), expected.WarningLevel, actual.WarningLevel)
			| Diff.WriteDiff(message, nameof(Diagnostic.IsSuppressed), expected.IsSuppressed, actual.IsSuppressed)
			| Diff.WriteDiff(message, nameof(Diagnostic.Location), expected.Location?.SourceSpan, actual.Location.SourceSpan)
			| Diff.WriteDiff(message, nameof(Diagnostic.Descriptor.HelpLinkUri), expected.HelpLink, actual.Descriptor.HelpLinkUri)
			| Diff.WriteDiff(message, nameof(Diagnostic.Descriptor.IsEnabledByDefault), expected.IsEnabledByDefault, actual.Descriptor.IsEnabledByDefault)
			| Diff.WriteSequenceDiff(message, nameof(Diagnostic.AdditionalLocations), expected.AdditionalLocations, actual.AdditionalLocations, static expected => expected.SourceSpan, static actual => actual.SourceSpan)
			| Diff.WriteSequenceDiff(message, nameof(Diagnostic.Descriptor.CustomTags), expected.CustomTags, actual.Descriptor.CustomTags.ToImmutableArray())
			| Diff.WriteOrderedSequenceDiff(message, nameof(Diagnostic.Properties), expected.Properties, actual.Properties, static expected => (expected.Key, expected.Value), static actual => (actual.Key, actual.Value));

		if (@throw)
		{
			BenchmarkInspectionException.Throw(message.ToString());
		}
	}
}
