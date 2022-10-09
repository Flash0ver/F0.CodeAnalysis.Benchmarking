using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace F0.CodeAnalysis.CSharp.Benchmarking;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(NullCSharpCodeRefactoringProvider))]
[Shared]
internal sealed class NullCSharpCodeRefactoringProvider : CodeRefactoringProvider
{
	public override Task ComputeRefactoringsAsync(CodeRefactoringContext context)
		=> Task.CompletedTask;
}
