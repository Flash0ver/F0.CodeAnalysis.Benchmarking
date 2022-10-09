using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace F0.CodeAnalysis.CSharp.Tests.Benchmarking;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(TestCSharpCodeRefactoringProvider))]
[Shared]
internal sealed class TestCSharpCodeRefactoringProvider : CodeRefactoringProvider
{
	public override Task ComputeRefactoringsAsync(CodeRefactoringContext context) => Task.CompletedTask;
}
