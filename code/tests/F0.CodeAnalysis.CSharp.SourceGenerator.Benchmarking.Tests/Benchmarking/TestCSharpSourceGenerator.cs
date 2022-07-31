using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace F0.CodeAnalysis.CSharp.Tests.Benchmarking;

[Generator(LanguageNames.CSharp)]
internal sealed class TestCSharpSourceGenerator : ISourceGenerator
{
	private const string defaultFileExtension = ".g.cs";

	private static readonly DiagnosticDescriptor Rule = new(
		"ID0001",
		"Test-Title",
		"Test-Message: {0}",
		"Test-Category",
		DiagnosticSeverity.Warning,
		true,
		"Test-Description",
		"Test-HelpLinkUri",
		"Test-Tag"
	);

	internal int Initializations { get; private set; }
	internal int Executions { get; private set; }

	public void Initialize(GeneratorInitializationContext context)
	{
		Initializations++;

		context.RegisterForSyntaxNotifications(TestCSharpSyntaxReceiver.Create);
	}

	public void Execute(GeneratorExecutionContext context)
	{
		Executions++;

		Debug.Assert(context.SyntaxReceiver is TestCSharpSyntaxReceiver);
		Debug.Assert(context.SyntaxContextReceiver is null);
		var receiver = (TestCSharpSyntaxReceiver)context.SyntaxReceiver;

		var compilation = (CSharpCompilation)context.Compilation;
		var parseOptions = (CSharpParseOptions)context.ParseOptions;

		string hintName = $"{GetType()}{defaultFileExtension}";
		StringBuilder sourceText = new();

		sourceText.AppendLine("// <auto-generated/>");
		if (parseOptions.LanguageVersion >= LanguageVersion.CSharp8)
		{
			sourceText.AppendLine("#nullable enable");
		}
		sourceText.AppendLine();
		sourceText.AppendLine("/*");

		sourceText.AppendLine($"# {nameof(receiver.Nodes)}: {receiver.Nodes.Count}");

		sourceText.AppendLine($"{nameof(compilation.Language)}: {compilation.Language}");
		sourceText.AppendLine($"{nameof(compilation.Options.AllowUnsafe)}: {compilation.Options.AllowUnsafe}");

		sourceText.AppendLine($"{nameof(parseOptions.LanguageVersion)}: {parseOptions.LanguageVersion}");

		const string key = "Analyzer_Config_Key";
		_ = context.AnalyzerConfigOptions.GlobalOptions.TryGetValue(key, out string? value)
			? sourceText.AppendLine($"Analyzer Config Value of '{key}': {value}")
			: sourceText.AppendLine($"Analyzer Config Value of '{key}' not found.");

		INamedTypeSymbol? type = context.Compilation.GetTypeByMetadataName("System.Type");
		Debug.Assert(type is not null, "Type 'System.Type' not found.");
		sourceText.AppendLine($"typeof(System.Type): {type.ContainingAssembly.Name}");

		sourceText.AppendLine("*/");

		context.AddSource(hintName, sourceText.ToString());

		foreach (AdditionalText additionalFile in context.AdditionalFiles)
		{
			sourceText.Clear();
			sourceText.AppendLine("/*");
			sourceText.AppendLine(additionalFile.GetText(CancellationToken.None)?.ToString());
			sourceText.AppendLine("*/");

			hintName = $"{Path.GetFileNameWithoutExtension(additionalFile.Path)}{defaultFileExtension}";
			context.AddSource(hintName, sourceText.ToString());
		}

		ImmutableDictionary<string, string?>.Builder builder = ImmutableDictionary.CreateBuilder<string, string?>();
		builder.Add("Key", "Value");
		ImmutableDictionary<string, string?> properties = builder.ToImmutable();

		foreach (SyntaxNode node in receiver.Nodes)
		{
			if (node is ClassDeclarationSyntax @class)
			{
				Location location = @class.Identifier.GetLocation();
				var diagnostic = Diagnostic.Create(Rule, location, DiagnosticSeverity.Error, new[] { location }, properties, @class.Identifier.ValueText);
				context.ReportDiagnostic(diagnostic);
			}
		}
	}
}
