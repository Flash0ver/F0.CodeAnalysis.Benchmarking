using Microsoft.CodeAnalysis;

namespace F0.CodeAnalysis.CSharp.Benchmarking;

[Generator(LanguageNames.CSharp)]
internal sealed class NullCSharpIncrementalGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
	}
}
