using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace F0.CodeAnalysis.CSharp.Tests.Benchmarking;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class TestCSharpDiagnosticAnalyzer : DiagnosticAnalyzer
{
	private static readonly DiagnosticDescriptor Rule = new(
		"ID0001",
		"Test-Title",
		"Test-MessageFormat: {0}",
		"Test-Category",
		DiagnosticSeverity.Warning,
		true,
		"Test-Description",
		"Test-HelpLinkUri",
		"Test-Tag"
	);

	private static readonly DiagnosticDescriptor AdditionalRule = new(
		"ID0002",
		"Additional-Title",
		"Additional-MessageFormat: {0}",
		"Additional-Category",
		DiagnosticSeverity.Warning,
		true,
		"Additional-Description",
		"Additional-HelpLinkUri",
		"Additional-Tag"
	);

	private int executions;

	internal int Initializations { get; private set; }
	internal int Executions => executions;

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule, AdditionalRule);

	public override void Initialize(AnalysisContext context)
	{
		Initializations++;

		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterSymbolAction(AnalyzeSymbol, SymbolKind.NamedType);
		context.RegisterAdditionalFileAction(AnalyzeAdditionalFile);
	}

	private void AnalyzeSymbol(SymbolAnalysisContext context)
	{
		Interlocked.Increment(ref executions);

		Debug.Assert(context.Symbol is INamedTypeSymbol);
		var symbol = Unsafe.As<INamedTypeSymbol>(context.Symbol);

		Debug.Assert(context.Compilation is CSharpCompilation);
		var compilation = Unsafe.As<CSharpCompilation>(context.Compilation);

		ImmutableDictionary<string, string?> properties = CreateProperties(compilation, context.Options);

		if (symbol.Name.ToCharArray().Any(Char.IsLower))
		{
			Location location = symbol.Locations[0];

			var diagnostic = Diagnostic.Create(Rule, location, DiagnosticSeverity.Error, new[] { location }, properties, symbol.Name);

			context.ReportDiagnostic(diagnostic);
		}
	}

	private static void AnalyzeAdditionalFile(AdditionalFileAnalysisContext context)
	{
		Debug.Assert(context.Compilation is CSharpCompilation);
		var compilation = Unsafe.As<CSharpCompilation>(context.Compilation);

		SourceText? contents = context.AdditionalFile.GetText(context.CancellationToken);
		Debug.Assert(contents is not null);

		IEnumerable<(Location Location, string Text)> lines = GetLines(context.AdditionalFile.Path, contents);

		ImmutableDictionary<string, string?> properties = CreateProperties(compilation, context.Options);

		foreach ((Location Location, string Text) line in lines)
		{
			var diagnostic = Diagnostic.Create(AdditionalRule, line.Location, DiagnosticSeverity.Error, new[] { line.Location }, properties, line.Text);

			context.ReportDiagnostic(diagnostic);
		}
	}

	private static ImmutableDictionary<string, string?> CreateProperties(CSharpCompilation compilation, AnalyzerOptions options)
	{
		ImmutableDictionary<string, string?>.Builder builder = ImmutableDictionary.CreateBuilder<string, string?>();

		builder.Add(nameof(LanguageVersion), compilation.LanguageVersion.ToString());

		builder.Add(nameof(compilation.Options.AllowUnsafe), compilation.Options.AllowUnsafe.ToString());

		INamedTypeSymbol? symbol = compilation.GetTypeByMetadataName("System.String");
		Debug.Assert(symbol is not null);
		builder.Add("MetadataReference", symbol.ContainingAssembly.Name);

		const string key = "Analyzer_Config_Key";
		if (options.AnalyzerConfigOptionsProvider.GlobalOptions.TryGetValue(key, out string? value))
		{
			builder.Add(key, value);
		}

		return builder.ToImmutable();
	}

	private static IEnumerable<(Location Location, string Text)> GetLines(string filePath, SourceText source)
	{
		foreach (TextLine textLine in source.Lines)
		{
			string text = textLine.ToString();
			if (String.IsNullOrWhiteSpace(text))
			{
				continue;
			}

			LinePosition start = new(textLine.LineNumber, textLine.Start);
			LinePosition end = new(textLine.LineNumber, textLine.End);
			LinePositionSpan lineSpan = new(start, end);

			yield return (Location.Create(filePath, textLine.Span, lineSpan), text);
		}
	}
}
