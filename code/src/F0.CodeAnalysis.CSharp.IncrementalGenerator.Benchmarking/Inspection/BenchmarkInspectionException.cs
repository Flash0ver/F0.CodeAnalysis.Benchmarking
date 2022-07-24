using System.Diagnostics.CodeAnalysis;

namespace F0.CodeAnalysis.CSharp.Inspection;

public sealed class BenchmarkInspectionException : Exception
{
	private const string Indent = "   ";

	[DoesNotReturn]
	internal static void Throw(string message)
		=> throw new BenchmarkInspectionException(message);

	[DoesNotReturn]
	internal static void Throw<T>(string assertion, T expected, T actual)
	{
		string message = assertion
			+ Environment.NewLine + Indent + $"Expected: {expected}"
			+ Environment.NewLine + Indent + $"Actual:   {actual}";
		throw new BenchmarkInspectionException(message);
	}

	[DoesNotReturn]
	internal static void Throw<T>(string assertion, T expected, T actual, string additionalMessage)
	{
		string message = assertion
			+ Environment.NewLine + Indent + $"Expected: {expected}"
			+ Environment.NewLine + Indent + $"Actual:   {actual}"
			+ Environment.NewLine + additionalMessage;
		throw new BenchmarkInspectionException(message);
	}

	public BenchmarkInspectionException()
	{
	}

	public BenchmarkInspectionException(string message)
		: base(message)
	{
	}

	public BenchmarkInspectionException(string message, Exception innerException)
		: base(message, innerException)
	{
	}
}
