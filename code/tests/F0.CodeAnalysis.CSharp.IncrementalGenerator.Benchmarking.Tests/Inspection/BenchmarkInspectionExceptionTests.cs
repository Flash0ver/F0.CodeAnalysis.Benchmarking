using F0.CodeAnalysis.CSharp.Inspection;

namespace F0.CodeAnalysis.CSharp.Tests.Inspection;

public class BenchmarkInspectionExceptionTests
{
	[Fact]
	public void Throw()
	{
		Exception exception = Assert.Throws<BenchmarkInspectionException>(() => BenchmarkInspectionException.Throw("Message"));

		string message = "Message";
		Assert.Equal(message, exception.Message);
		Assert.Null(exception.InnerException);
	}

	[Fact]
	public void Throw_Without_AdditionalMessage()
	{
		Exception exception = Assert.Throws<BenchmarkInspectionException>(() => BenchmarkInspectionException.Throw("1", "2", "3"));

		string message = "1"
			+ Environment.NewLine + "   Expected: 2"
			+ Environment.NewLine + "   Actual:   3";
		Assert.Equal(message, exception.Message);
		Assert.Null(exception.InnerException);
	}

	[Fact]
	public void Throw_With_AdditionalMessage()
	{
		string additionalMessage = "Additional Message";
		Exception exception = Assert.Throws<BenchmarkInspectionException>(() => BenchmarkInspectionException.Throw("1", "2", "3", additionalMessage));

		string message = "1"
			+ Environment.NewLine + "   Expected: 2"
			+ Environment.NewLine + "   Actual:   3"
			+ Environment.NewLine + "Additional Message";
		Assert.Equal(message, exception.Message);
		Assert.Null(exception.InnerException);
	}

	[Fact]
	public void Constructor_Parameterless()
	{
		BenchmarkInspectionException exception = new();

		Assert.Equal($"Exception of type '{typeof(BenchmarkInspectionException)}' was thrown.", exception.Message);
		Assert.Null(exception.InnerException);
	}

	[Fact]
	public void Constructor_Message()
	{
		string message = "240";
		BenchmarkInspectionException exception = new(message);

		Assert.Equal(message, exception.Message);
		Assert.Null(exception.InnerException);
	}

	[Fact]
	public void Constructor_InnerException()
	{
		Exception innerException = new ArgumentException("F0");
		BenchmarkInspectionException exception = new("240", innerException);

		Assert.Equal("240", exception.Message);
		Assert.Same(innerException, exception.InnerException);
	}
}
