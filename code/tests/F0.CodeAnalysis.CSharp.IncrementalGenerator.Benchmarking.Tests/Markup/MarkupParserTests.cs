using System.Collections.Immutable;
using F0.CodeAnalysis.CSharp.Markup;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace F0.CodeAnalysis.CSharp.Tests.Markup;

public class MarkupParserTests
{
	[Theory]
	[InlineData("{|#5:...|}", "...")]
	[InlineData("{|#5:...|#5}", "...")]
	public void Parse_OneLiner(string markup, string expected)
	{
		string sanitized = MarkupParser.Parse("FilePath", markup, out ImmutableArray<Location> locations);

		sanitized.Should().Be(expected);
		locations.Should().HaveCount(1);
		locations.Should().SatisfyRespectively(
			single =>
			{
				single.Kind.Should().NotBe(LocationKind.SourceFile);
				single.IsInSource.Should().BeFalse();
				single.IsInMetadata.Should().BeFalse();
				single.SourceTree.Should().BeNull();
				single.MetadataModule.Should().BeNull();
				single.SourceSpan.Should().Be(TextSpan.FromBounds(0, 3));
				single.GetLineSpan().Should().Be(new FileLinePositionSpan("FilePath", new LinePosition(0, 0), new LinePosition(0, 3)));
				single.GetMappedLineSpan().Should().Be(new FileLinePositionSpan("FilePath", new LinePosition(0, 0), new LinePosition(0, 3)));
			}
		);
	}

	[Fact]
	public void Parse_SingleLine()
	{
		string markup = "{|#0:public|} {|#1:class|} {|#2:MyClass|} { }";
		string expected = "public class MyClass { }";

		string sanitized = MarkupParser.Parse("FilePath", markup, out ImmutableArray<Location> locations);

		sanitized.Should().Be(expected);
		locations.Should().HaveCount(3);
		locations.Should().SatisfyRespectively(
			first =>
			{
				first.Kind.Should().NotBe(LocationKind.SourceFile);
				first.IsInSource.Should().BeFalse();
				first.IsInMetadata.Should().BeFalse();
				first.SourceTree.Should().BeNull();
				first.MetadataModule.Should().BeNull();
				first.SourceSpan.Should().Be(TextSpan.FromBounds(0, 6));
				first.GetLineSpan().Should().Be(new FileLinePositionSpan("FilePath", new LinePosition(0, 0), new LinePosition(0, 6)));
				first.GetMappedLineSpan().Should().Be(new FileLinePositionSpan("FilePath", new LinePosition(0, 0), new LinePosition(0, 6)));
			},
			second =>
			{
				second.Kind.Should().NotBe(LocationKind.SourceFile);
				second.IsInSource.Should().BeFalse();
				second.IsInMetadata.Should().BeFalse();
				second.SourceTree.Should().BeNull();
				second.MetadataModule.Should().BeNull();
				second.SourceSpan.Should().Be(TextSpan.FromBounds(7, 12));
				second.GetLineSpan().Should().Be(new FileLinePositionSpan("FilePath", new LinePosition(0, 7), new LinePosition(0, 12)));
				second.GetMappedLineSpan().Should().Be(new FileLinePositionSpan("FilePath", new LinePosition(0, 7), new LinePosition(0, 12)));
			},
			third =>
			{
				third.Kind.Should().NotBe(LocationKind.SourceFile);
				third.IsInSource.Should().BeFalse();
				third.IsInMetadata.Should().BeFalse();
				third.SourceTree.Should().BeNull();
				third.MetadataModule.Should().BeNull();
				third.SourceSpan.Should().Be(TextSpan.FromBounds(13, 20));
				third.GetLineSpan().Should().Be(new FileLinePositionSpan("FilePath", new LinePosition(0, 13), new LinePosition(0, 20)));
				third.GetMappedLineSpan().Should().Be(new FileLinePositionSpan("FilePath", new LinePosition(0, 13), new LinePosition(0, 20)));
			}
		);
	}

	[Fact]
	public void Parse_NoLocations()
	{
		string markup = @"
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class MSTest
{
	[TestMethod]
	public void Given_When_Then()
	{
	}

	[DataTestMethod]
	[DataRow(0x_F0)]
	public void MethodUnderTest_Scenario_ExpectedResult(int value)
	{
	}
}
";

		string sanitized = MarkupParser.Parse("FilePath", markup, out ImmutableArray<Location> locations);

		sanitized.Should().Be(markup);
		locations.Should().HaveCount(0);
	}

	[Fact]
	public void Parse_MultiLine()
	{
		string markup = @"
using System;
using NUnit.Framework;

[TestFixture]
public class NUnitTests
{
	[Test]
	public void {|#0:Given_When_Then|}()
	{
	}

	[TestCase(0x_F0)]
	public void {|#1:MethodUnderTest_Scenario_ExpectedResult|#1}(int value)
	{
	}
}
";

		string expected = @"
using System;
using NUnit.Framework;

[TestFixture]
public class NUnitTests
{
	[Test]
	public void Given_When_Then()
	{
	}

	[TestCase(0x_F0)]
	public void MethodUnderTest_Scenario_ExpectedResult(int value)
	{
	}
}
";

		string sanitized = MarkupParser.Parse("FilePath", markup, out ImmutableArray<Location> locations);

		sanitized.Should().Be(expected);
		locations.Should().HaveCount(2);
		locations.Should().SatisfyRespectively(
			first =>
			{
				first.Kind.Should().NotBe(LocationKind.SourceFile);
				first.IsInSource.Should().BeFalse();
				first.IsInMetadata.Should().BeFalse();
				first.SourceTree.Should().BeNull();
				first.MetadataModule.Should().BeNull();
				first.SourceSpan.Should().Be(TextSpan.FromBounds(108, 123));
				first.GetLineSpan().Should().Be(new FileLinePositionSpan("FilePath", new LinePosition(8, 13), new LinePosition(8, 28)));
				first.GetMappedLineSpan().Should().Be(new FileLinePositionSpan("FilePath", new LinePosition(8, 13), new LinePosition(8, 28)));
			},
			second =>
			{
				second.Kind.Should().NotBe(LocationKind.SourceFile);
				second.IsInSource.Should().BeFalse();
				second.IsInMetadata.Should().BeFalse();
				second.SourceTree.Should().BeNull();
				second.MetadataModule.Should().BeNull();
				second.SourceSpan.Should().Be(TextSpan.FromBounds(170, 209));
				second.GetLineSpan().Should().Be(new FileLinePositionSpan("FilePath", new LinePosition(13, 13), new LinePosition(13, 52)));
				second.GetMappedLineSpan().Should().Be(new FileLinePositionSpan("FilePath", new LinePosition(13, 13), new LinePosition(13, 52)));
			}
		);
	}

	[Fact]
	public void Parse_Overlapping()
	{
		string markup = @"
using System;
using Xunit;

public class xUnit
{
	[Fact]
	public void Given_When_Then()
	{
	}

	[Theory]
	[InlineData(0x_F0)]
	public void MethodUnderTest_Scenario_ExpectedResult(int value)
	{
	}
}
";

		string sanitized = MarkupParser.Parse("FilePath", markup, out ImmutableArray<Location> locations);

		sanitized.Should().Be(markup);
		locations.Should().HaveCount(0);
	}
}
