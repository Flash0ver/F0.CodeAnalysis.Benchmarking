using Basic.Reference.Assemblies;
using F0.CodeAnalysis.CSharp.Benchmarking;
using F0.CodeAnalysis.CSharp.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace F0.CodeAnalysis.CSharp.Tests.Benchmarking;

public class CSharpSourceGeneratorBenchmarkTests
{
	private const string generatedExtension = ".g.cs";
	private readonly string defaultHintName = $"{typeof(TestCSharpSourceGenerator)}{generatedExtension}";

	[Fact]
	public void Initialize_Null_Throws()
	{
		CSharpSourceGeneratorBenchmark<NullCSharpSourceGenerator> benchmark = new();

		void initialize() => benchmark.Initialize(null!);

		Assert.Throws<ArgumentNullException>("context", initialize);
	}

	[Fact]
	public void Inspect_Null_Throws()
	{
		CSharpSourceGeneratorBenchmark<NullCSharpSourceGenerator> benchmark = new();

		void inspect() => benchmark.Inspect(null!);

		Assert.Throws<ArgumentNullException>("context", inspect);
	}

	[Fact]
	public void Invoke_Without_Initialize_Throws()
	{
		CSharpSourceGeneratorBenchmark<NullCSharpSourceGenerator> benchmark = new();

		object invoke() => benchmark.Invoke();

		Assert.Throws<NullReferenceException>(invoke);
	}

	[Fact]
	public void NullGenerator_III_DoesNotThrow()
	{
		CSharpSourceGeneratorBenchmark<NullCSharpSourceGenerator> benchmark = new();

		benchmark.Initialize(new CSharpSourceGeneratorBenchmarkInitializationContext());

		benchmark.Invoke();

		benchmark.Inspect(new CSharpSourceGeneratorBenchmarkInspectionContext());
	}

	[Fact]
	public void TestGenerator_Default_DoesNotThrow()
	{
		CSharpSourceGeneratorBenchmark<TestCSharpSourceGenerator> benchmark = new();

#if NET6_0
		const string metadataReference = "System.Private.CoreLib";
#elif NET472
		const string metadataReference = "mscorlib";
#else
		throw new InvalidOperationException("Unexpected Target Framework");
#endif

		string source =
$@"// <auto-generated/>
#nullable enable

/*
# Nodes: 0
Language: C#
AllowUnsafe: False
LanguageVersion: CSharp10
Analyzer Config Value of 'Analyzer_Config_Key' not found.
typeof(System.Type): {metadataReference}
*/
";

		benchmark.Initialize(new CSharpSourceGeneratorBenchmarkInitializationContext());

		benchmark.Invoke();

		benchmark.Inspect(new CSharpSourceGeneratorBenchmarkInspectionContext
		{
			Source = (defaultHintName, source),
		});
	}

	[Fact]
	public void TestGenerator_NonDefault_DoesNotThrow()
	{
		CSharpSourceGeneratorBenchmark<TestCSharpSourceGenerator> benchmark = new();

		string source =
@"// <auto-generated/>

/*
# Nodes: 2
Language: C#
AllowUnsafe: True
LanguageVersion: CSharp7_3
Analyzer Config Value of 'Analyzer_Config_Key': Analyzer_Config_Value
typeof(System.Type): netstandard
*/
";

		benchmark.Initialize(new CSharpSourceGeneratorBenchmarkInitializationContext
		{
			Source = "public class {|#0:MyClass0|} { }",
			AdditionalSources = { "public class {|#1:MyClass1|} { }", "public struct MyStruct { }" },
			AdditionalTexts = { ("Path1.txt", "Additional Text 1"), ("Path2.txt", "Additional Text 2"), ("Path3.txt", "Additional Text 3") },
			ParseOptions = new CSharpParseOptions(LanguageVersion.CSharp7_3),
			CompilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithAllowUnsafe(true),
			MetadataReferences = ReferenceAssemblies.NetStandard20,
			AnalyzerConfigOptions = { ("Analyzer_Config_Key", "Analyzer_Config_Value") },
		});

		benchmark.Invoke();

		benchmark.Inspect(new CSharpSourceGeneratorBenchmarkInspectionContext
		{
			Source = (defaultHintName, source),
			AdditionalSources =
			{
				($"Path1{generatedExtension}", GetExpectedAdditionalText("Additional Text 1")),
				($"Path2{generatedExtension}", GetExpectedAdditionalText("Additional Text 2")),
				($"Path3{generatedExtension}", GetExpectedAdditionalText("Additional Text 3")),
			},
			Diagnostics =
			{
				new AdhocDiagnostic(0)
				{
					Id = "ID0001",
					Category = "Test-Category",
					Message = "Test-Message: MyClass0",
					MessageFormat = "Test-Message: {0}",
					Severity = DiagnosticSeverity.Error,
					DefaultSeverity = DiagnosticSeverity.Warning,
					IsEnabledByDefault = true,
					WarningLevel = 0,
					IsSuppressed = false,
					Title = "Test-Title",
					Description = "Test-Description.",
					HelpLink = "Test-HelpLinkUri",
					AdditionalLocations = { Location.Create(String.Empty, TextSpan.FromBounds(13, 21), new LinePositionSpan()) },
					CustomTags = { "Test-Tag" },
					Properties = { { "Zero", "One" }, { "Two", "Three" }, { "Four", "Five" }, { "Six", "Seven" }, { "Eight", "Nine" } },
				},
				new AdhocDiagnostic(1)
				{
					Id = "ID0001",
					Message = "Test-Message: MyClass1",
					AdditionalLocations = { Location.Create(String.Empty, TextSpan.FromBounds(13, 21), new LinePositionSpan()) },
				},
			},
		});

		static string GetExpectedAdditionalText(string additionalText)
		{
			return $@"/*
{additionalText}
*/
";
		}
	}

	[Fact]
	public void Invoke_Without_Memoization()
	{
		CSharpSourceGeneratorBenchmark<TestCSharpSourceGenerator> benchmark = new();

		string source = @"namespace F0.Testing;

public struct MyStruct
{
}";

		benchmark.Initialize(new CSharpSourceGeneratorBenchmarkInitializationContext
		{
			Source = source,
		});

		benchmark.Generator.Initializations.Should().Be(0);
		benchmark.Generator.Executions.Should().Be(0);
		benchmark.Invoke();
		benchmark.Generator.Initializations.Should().Be(1);
		benchmark.Generator.Executions.Should().Be(1);
		benchmark.Invoke();
		benchmark.Generator.Initializations.Should().Be(2);
		benchmark.Generator.Executions.Should().Be(2);
	}
}
