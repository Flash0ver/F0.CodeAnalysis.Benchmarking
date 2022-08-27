using System.Text;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace F0.CodeAnalysis.CSharp.Diffing;

internal static class Diff
{
	internal static string GetDiff(string original, string modified)
	{
		StringBuilder diffText = new();

		Differ differ = new();
		InlineDiffBuilder diffBuilder = new(differ);
		DiffPaneModel diffModel = diffBuilder.BuildDiffModel(original, modified, false);

		foreach (DiffPiece diffPiece in diffModel.Lines)
		{
			_ = diffPiece.Type switch
			{
				ChangeType.Inserted => diffText.Append('+'),
				ChangeType.Deleted => diffText.Append('-'),
				_ => diffText.Append(' '),
			};
			_ = diffText.AppendLine(diffPiece.Text);
		}

		return diffText.ToString();
	}

	internal static bool WriteDiff<T>(StringBuilder builder, string name, T expected, T actual)
		where T : class?, IEquatable<T>?
	{
		if (expected is not null && !expected.Equals(actual))
		{
			_ = builder.AppendLine($"- {name}: {expected}");
			_ = builder.AppendLine($"+ {name}: {actual}");
			return true;
		}
		else if (actual is not null)
		{
			_ = builder.AppendLine($"  {name}: {actual}");
		}

		return false;
	}

	internal static bool WriteDiff<T>(StringBuilder builder, string name, T? expected, T actual)
		where T : struct
	{
		if (expected.HasValue && !Nullable.Equals(expected, actual))
		{
			_ = builder.AppendLine($"- {name}: {expected}");
			_ = builder.AppendLine($"+ {name}: {actual}");
			return true;
		}
		else
		{
			_ = builder.AppendLine($"  {name}: {actual}");
		}

		return false;
	}

	internal static bool WriteSequenceDiff<T>(StringBuilder builder, string name, ICollection<T> expected, IReadOnlyCollection<T> actual)
		where T : IEquatable<T>
	{
		if (expected.Count > 0 && !expected.SequenceEqual(actual))
		{
			_ = builder.AppendLine($"- {name}: {String.Join(", ", expected)}");
			_ = builder.AppendLine($"+ {name}: {String.Join(", ", actual)}");
			return true;
		}
		else if (actual.Any())
		{
			_ = builder.AppendLine($"  {name}: {String.Join(", ", actual)}");
		}

		return false;
	}

	internal static bool WriteOrderedSequenceDiff<T>(StringBuilder builder, string name, ICollection<T> expected, IReadOnlyCollection<T> actual)
		where T : IEquatable<T>
	{
		T[] sortedExpected = expected.OrderBy(static element => element).ToArray();
		T[] sortedActual = actual.OrderBy(static element => element).ToArray();

		return WriteSequenceDiff(builder, name, sortedExpected, sortedActual);
	}

	internal static bool WriteSequenceDiff<TExpected, TActual, TElement>(StringBuilder builder, string name, ICollection<TExpected> expected, IReadOnlyCollection<TActual> actual, Func<TExpected, TElement> expectedSelector, Func<TActual, TElement> actualSelector)
		where TElement : IEquatable<TElement>
	{
		TElement[] mappedExpected = expected.Select(expectedSelector).ToArray();
		TElement[] mappedActual = actual.Select(actualSelector).ToArray();

		return WriteSequenceDiff(builder, name, mappedExpected, mappedActual);
	}

	internal static bool WriteOrderedSequenceDiff<TExpected, TActual, TElement>(StringBuilder builder, string name, ICollection<TExpected> expected, IReadOnlyCollection<TActual> actual, Func<TExpected, TElement> expectedSelector, Func<TActual, TElement> actualSelector)
		where TElement : IEquatable<TElement>
	{
		TElement[] sortedExpected = expected.Select(expectedSelector).OrderBy(static element => element).ToArray();
		TElement[] sortedActual = actual.Select(actualSelector).OrderBy(static element => element).ToArray();

		return WriteSequenceDiff(builder, name, sortedExpected, sortedActual);
	}
}
