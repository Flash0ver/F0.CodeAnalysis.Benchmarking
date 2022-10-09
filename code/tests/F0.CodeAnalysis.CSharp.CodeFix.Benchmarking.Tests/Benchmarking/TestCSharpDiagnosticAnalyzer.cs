using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace F0.CodeAnalysis.CSharp.Tests.Benchmarking;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class TestCSharpDiagnosticAnalyzer : DiagnosticAnalyzer
{
	public const string DiagnosticId = "ID0001";

	internal static readonly DiagnosticDescriptor Rule = new(
		DiagnosticId,
		"Test-Title",
		"Test-MessageFormat: {0}",
		"Test-Category",
		DiagnosticSeverity.Warning,
		true,
		"Test-Description",
		"Test-HelpLinkUri",
		"Test-Tag"
	);

	private int executions;

	internal int Initializations { get; private set; }
	internal int Executions => executions;

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

	public override void Initialize(AnalysisContext context)
	{
		Initializations++;

		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
	}

	private void AnalyzeSymbol(SymbolAnalysisContext context)
	{
		Interlocked.Increment(ref executions);

		Debug.Assert(context.Symbol is INamedTypeSymbol);
		var namedTypeSymbol = Unsafe.As<INamedTypeSymbol>(context.Symbol);

		Debug.Assert(context.Compilation is CSharpCompilation);
		var compilation = Unsafe.As<CSharpCompilation>(context.Compilation);

		if (namedTypeSymbol.Name.ToCharArray().Any(Char.IsLower))
		{
			ImmutableDictionary<string, string?>? properties = namedTypeSymbol.ContainingNamespace.IsGlobalNamespace
				? null
				: CreateProperties(compilation, context.Options);

			var diagnostic = Diagnostic.Create(Rule, namedTypeSymbol.Locations[0], properties, namedTypeSymbol.Name);

			context.ReportDiagnostic(diagnostic);
		}
	}

	private static ImmutableDictionary<string, string?> CreateProperties(CSharpCompilation compilation, AnalyzerOptions options)
	{
		ImmutableDictionary<string, string?>.Builder properties = ImmutableDictionary.CreateBuilder<string, string?>();

		properties.Add(nameof(LanguageVersion), compilation.LanguageVersion.ToDisplayString());
		properties.Add(nameof(compilation.Options.AllowUnsafe), compilation.Options.AllowUnsafe.ToString());

		INamedTypeSymbol? symbol = compilation.GetTypeByMetadataName("System.Object");
		Debug.Assert(symbol is not null);
		properties.Add(nameof(MetadataReference), symbol.ContainingAssembly.Name);

		const string key = "Analyzer_Config_Key";
		if (options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(key, out string? value))
		{
			properties.Add(key, value);
		}

		return properties.ToImmutable();
	}

	internal void ClearCounters()
	{
		Initializations = 0;
		executions = 0;
	}
}
