using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace F0.CodeAnalysis.CSharp.Diagnostics;

internal static class Diagnostics
{
	internal static ImmutableArray<Diagnostic> Empty { get; } = ImmutableArray<Diagnostic>.Empty;
}
