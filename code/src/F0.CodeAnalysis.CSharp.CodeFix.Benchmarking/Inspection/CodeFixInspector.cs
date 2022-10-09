using System.Collections.Immutable;
using F0.CodeAnalysis.CSharp.Diffing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace F0.CodeAnalysis.CSharp.Inspection;

internal static class CodeFixInspector
{
	internal static async Task DocumentsAsync(ImmutableArray<string> expected, ImmutableArray<Document> actual)
	{
		if (expected.Length != actual.Length)
		{
			BenchmarkInspectionException.Throw("Unexpected number of documents:", expected.Length, actual.Length);
		}

		for (int i = 0; i < expected.Length; i++)
		{
			string expectedDocument = expected[i];
			Document actualDocument = actual[i];

			await DocumentAsync(expectedDocument, actualDocument, i).ConfigureAwait(false);
		}
	}

	internal static async Task DocumentAsync(string expected, Document actual, int index)
	{
		SourceText actualText = await actual.GetTextAsync(CancellationToken.None).ConfigureAwait(false);
		string actualCode = actualText.ToString();

		if (!actualCode.Equals(expected, StringComparison.Ordinal))
		{
			string diff = Diff.GetDiff(expected, actualCode);
			string message = $"Expected and actual source text of document #{index} differ: " + Environment.NewLine + diff;
			BenchmarkInspectionException.Throw(message);
		}
	}
}
