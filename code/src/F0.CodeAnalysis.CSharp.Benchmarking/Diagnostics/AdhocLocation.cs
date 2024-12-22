using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace F0.CodeAnalysis.CSharp.Diagnostics;

public sealed class AdhocLocation
{
	//TODO: INTERNALIZE ALL C'TORS
	public AdhocLocation(int markupLocation)
		=> MarkupLocation = markupLocation;

	public AdhocLocation(FileLinePositionSpan span)
		=> Span = span;

	public AdhocLocation(string filePath, int startLine, int startColumn, int endLine, int endColumn)
		=> Span = new FileLinePositionSpan(filePath, new LinePosition(startLine - 1, startColumn - 1), new LinePosition(endLine - 1, endColumn - 1));

	public AdhocLocation(int fileIndex, int startLine, int startColumn, int endLine, int endColumn)
		=> Span = new FileLinePositionSpan(CreateFilePath(fileIndex), new LinePosition(startLine - 1, startColumn - 1), new LinePosition(endLine - 1, endColumn - 1));

	internal int? MarkupLocation { get; }
	internal FileLinePositionSpan Span { get; }

	public static AdhocLocation Create(int markupLocation)
		=> new AdhocLocation(markupLocation);

	public static AdhocLocation Create(FileLinePositionSpan span)
		=> new AdhocLocation(span);

	public static AdhocLocation Create(string filePath, LinePositionSpan span)
		=> new AdhocLocation(new FileLinePositionSpan(filePath, span));

	public static AdhocLocation Create(string filePath, LinePosition start, LinePosition end)
		=> new AdhocLocation(new FileLinePositionSpan(filePath, new LinePositionSpan(start, end)));

	public static AdhocLocation Create(string filePath, int startLine, int startColumn, int endLine, int endColumn)
		=> new AdhocLocation(new FileLinePositionSpan(filePath, new LinePositionSpan(new LinePosition(startLine, startColumn), new LinePosition(endLine, endColumn))));

	internal FileLinePositionSpan GetSpan()
	{
		if (!Span.IsValid)
		{
			throw new InvalidOperationException($"{nameof(Span)} is invalid.");
		}

		Debug.Assert(!MarkupLocation.HasValue, $"{nameof(MarkupLocation)} has been set to {MarkupLocation}.");

		return Span;
	}

	private static string CreateFilePath(int index)
	{
		if (index < 0)
		{
			throw new InvalidOperationException("Non-negative number required.");
		}

		const string fileName = "Benchmark";
		const string fileExtension = ".cs";

		return $"/0/{fileName}{index}{fileExtension}";
	}
}
