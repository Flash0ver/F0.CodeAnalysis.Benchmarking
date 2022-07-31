using Microsoft.CodeAnalysis;

namespace F0.CodeAnalysis.CSharp.Inspection;

internal static class SyntaxTrees
{
	internal static IEnumerable<SyntaxTree> Empty { get; } = Array.Empty<SyntaxTree>();
}
