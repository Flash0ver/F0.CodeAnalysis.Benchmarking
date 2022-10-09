using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace F0.CodeAnalysis.CSharp.Benchmarking;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NullCodeFixProvider))]
[Shared]
internal sealed class NullCodeFixProvider : CodeFixProvider
{
	public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray<string>.Empty;

	public override FixAllProvider? GetFixAllProvider()
		=> null;

	public override Task RegisterCodeFixesAsync(CodeFixContext context)
		=> Task.CompletedTask;
}
