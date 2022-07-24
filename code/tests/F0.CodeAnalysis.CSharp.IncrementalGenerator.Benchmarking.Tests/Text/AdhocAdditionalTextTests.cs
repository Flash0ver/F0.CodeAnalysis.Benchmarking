using System.Text;
using F0.CodeAnalysis.CSharp.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace F0.CodeAnalysis.CSharp.Tests.Text;

public class AdhocAdditionalTextTests
{
	[Fact]
	public void Default_Without_Text()
	{
		AdditionalText additionalText = new AdhocAdditionalText("Path.txt");

		additionalText.Path.Should().Be("Path.txt");

		SourceText? sourceText = additionalText.GetText();
		Assert.Null(sourceText);
	}

	[Fact]
	public void Default_With_Text()
	{
		AdditionalText additionalText = new AdhocAdditionalText("Path.txt", "Text");

		additionalText.Path.Should().Be("Path.txt");

		SourceText? sourceText = additionalText.GetText();
		Assert.NotNull(sourceText);

		sourceText.ToString().Should().Be("Text");
		sourceText.Encoding.Should().BeNull();
		sourceText.ChecksumAlgorithm.Should().Be(SourceHashAlgorithm.Sha1);
	}

	[Fact]
	public void NonDefault_Without_Text()
	{
		AdditionalText additionalText = new AdhocAdditionalText("Path.txt", null, Encoding.UTF8, SourceHashAlgorithm.Sha256);

		additionalText.Path.Should().Be("Path.txt");

		SourceText? sourceText = additionalText.GetText();
		Assert.Null(sourceText);
	}

	[Fact]
	public void NonDefault_With_Text()
	{
		AdditionalText additionalText = new AdhocAdditionalText("Path.txt", "Text", Encoding.UTF8, SourceHashAlgorithm.Sha256);

		additionalText.Path.Should().Be("Path.txt");

		SourceText? sourceText = additionalText.GetText();
		Assert.NotNull(sourceText);

		sourceText.ToString().Should().Be("Text");
		sourceText.Encoding.Should().Be(Encoding.UTF8);
		sourceText.ChecksumAlgorithm.Should().Be(SourceHashAlgorithm.Sha256);
	}

	[Fact]
	public void GetText_With_Cancellation()
	{
		AdditionalText additionalText = new AdhocAdditionalText("Path.txt", "Text");

		Func<SourceText?> sourceText = () => additionalText.GetText(new CancellationToken(true));

		Assert.Throws<OperationCanceledException>(sourceText);
	}
}
