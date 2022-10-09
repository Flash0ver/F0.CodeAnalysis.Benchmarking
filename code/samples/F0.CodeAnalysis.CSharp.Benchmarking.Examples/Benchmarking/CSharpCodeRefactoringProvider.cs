using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace F0.CodeAnalysis.CSharp.Examples.Benchmarking;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpCodeRefactoringProvider))]
[Shared]
internal sealed class CSharpCodeRefactoringProvider : CodeRefactoringProvider
{
	public override Task ComputeRefactoringsAsync(CodeRefactoringContext context)
	{
		return Task.CompletedTask;
	}
}
