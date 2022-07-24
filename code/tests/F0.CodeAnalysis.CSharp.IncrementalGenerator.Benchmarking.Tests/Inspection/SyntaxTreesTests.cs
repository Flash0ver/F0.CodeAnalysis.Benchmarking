using F0.CodeAnalysis.CSharp.Inspection;
using Microsoft.CodeAnalysis;

namespace F0.CodeAnalysis.CSharp.Tests.Inspection;

public class SyntaxTreesTests
{
	[Fact]
	public void Empty()
	{
		IEnumerable<SyntaxTree> empty = SyntaxTrees.Empty;

		empty.Should().BeEmpty();
	}
}
