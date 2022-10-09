using Microsoft.CodeAnalysis.CodeRefactorings;

namespace F0.CodeAnalysis.CSharp.Benchmarking;

public sealed class CSharpCodeRefactoringProviderBenchmark<TCodeRefactoringProvider>
	where TCodeRefactoringProvider : CodeRefactoringProvider, new()
{
	public CSharpCodeRefactoringProviderBenchmark()
	{
		Refactoring = new TCodeRefactoringProvider();
	}

	internal TCodeRefactoringProvider Refactoring { get; }

	public void Initialize(CSharpCodeRefactoringProviderBenchmarkInitializationContext context)
	{
	}

	public object Invoke()
	{
		return null!;
	}

	public void Inspect(CSharpCodeRefactoringProviderBenchmarkInspectionContext context)
	{
	}
}
