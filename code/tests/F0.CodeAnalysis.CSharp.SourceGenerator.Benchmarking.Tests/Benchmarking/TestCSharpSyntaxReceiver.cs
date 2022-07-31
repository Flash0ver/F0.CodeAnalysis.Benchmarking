using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace F0.CodeAnalysis.CSharp.Tests.Benchmarking;

internal sealed class TestCSharpSyntaxReceiver : ISyntaxReceiver
{
	internal static ISyntaxReceiver Create()
		=> new TestCSharpSyntaxReceiver();

	private TestCSharpSyntaxReceiver()
	{
	}

	public List<SyntaxNode> Nodes { get; } = new();

	public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
	{
		if (syntaxNode is ClassDeclarationSyntax)
		{
			Nodes.Add(syntaxNode);
		}
	}
}
