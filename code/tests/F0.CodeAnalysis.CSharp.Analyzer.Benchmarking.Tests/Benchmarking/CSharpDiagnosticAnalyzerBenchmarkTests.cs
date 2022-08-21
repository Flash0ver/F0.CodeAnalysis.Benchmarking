using System.Reflection;
using Basic.Reference.Assemblies;
using F0.CodeAnalysis.CSharp.Benchmarking;
using F0.CodeAnalysis.CSharp.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace F0.CodeAnalysis.CSharp.Tests.Benchmarking;

public class CSharpDiagnosticAnalyzerBenchmarkTests
{
	[Fact]
	public void Initialize_Null_Throws()
	{
		CSharpDiagnosticAnalyzerBenchmark<NullCSharpDiagnosticAnalyzer> benchmark = new();

		void initialize() => benchmark.Initialize(null!);

		Assert.Throws<ArgumentNullException>("context", initialize);
	}

	[Fact]
	public void Inspect_Null_Throws()
	{
		CSharpDiagnosticAnalyzerBenchmark<NullCSharpDiagnosticAnalyzer> benchmark = new();

		Task inspect() => benchmark.InspectAsync(null!);

		Assert.ThrowsAsync<ArgumentNullException>("context", inspect);
	}

	[Fact]
	public void Invoke_Without_Initialize_Throws()
	{
		CSharpDiagnosticAnalyzerBenchmark<NullCSharpDiagnosticAnalyzer> benchmark = new();

		Task invoke() => benchmark.InvokeAsync();

		Assert.ThrowsAsync<NullReferenceException>(invoke);
	}

	[Fact]
	public async Task NullAnalyzer_III_DoesNotThrow()
	{
		CSharpDiagnosticAnalyzerBenchmark<NullCSharpDiagnosticAnalyzer> benchmark = new();

		benchmark.Initialize(new CSharpDiagnosticAnalyzerBenchmarkInitializationContext());

		await benchmark.InvokeAsync();

		await benchmark.InspectAsync(new CSharpDiagnosticAnalyzerBenchmarkInspectionContext());
	}

	[Fact]
	public async Task TestAnalyzer_Default_DoesNotThrow()
	{
		CSharpDiagnosticAnalyzerBenchmark<TestCSharpDiagnosticAnalyzer> benchmark = new();

#if NET6_0
		const string metadataReference = "System.Private.CoreLib";
#elif NET472
		const string metadataReference = "mscorlib";
#else
		throw new InvalidOperationException("Unexpected Target Framework");
#endif

		string source = @"
using System;

namespace MyNamespace
{
	public class {|#0:MyClass|}
	{
	}
}
";

		benchmark.Initialize(new CSharpDiagnosticAnalyzerBenchmarkInitializationContext
		{
			Source = source,
		});

		await benchmark.InvokeAsync();

		await benchmark.InspectAsync(new CSharpDiagnosticAnalyzerBenchmarkInspectionContext()
		{
			Diagnostics = { CreateDiagnostic(0, "MyClass", TextSpan.FromBounds(59, 66), LanguageVersion.CSharp10, false, metadataReference, false) }
		});
	}

	[Fact]
	public async Task TestAnalyzer_NonDefault_DoesNotThrow()
	{
		CSharpDiagnosticAnalyzerBenchmark<TestCSharpDiagnosticAnalyzer> benchmark = new();

		const LanguageVersion langVersion = LanguageVersion.CSharp1;
		const bool allowUnsafe = true;
		string metadataReference = "netstandard";

		string source = @"
using System;

namespace MyNamespace
{
	public class {|#0:MyClass0|}
	{
	}

	public class {|#1:MyClass1|}
	{
	}

	public class {|#2:MyClass2|}
	{
	}
}
";

		benchmark.Initialize(new CSharpDiagnosticAnalyzerBenchmarkInitializationContext
		{
			Source = source,
			AdditionalSources =
			{
				"struct {|#3:MyStruct3|} { } struct {|#4:MyStruct4|} { }",
				"struct {|#5:MyStruct5|} { }"
			},
			AdditionalTexts =
			{
				("Path1.txt", "{|#6:Additional Text 6|}" + Environment.NewLine + "{|#7:Additional Text 7|}" + Environment.NewLine + "{|#8:Additional Text 8|}"),
				("Path2.txt", "{|#9:Additional Text 9|}"),
				("Path3.txt", " "),
			},
			ParseOptions = new CSharpParseOptions(langVersion),
			CompilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithAllowUnsafe(allowUnsafe),
			MetadataReferences = ReferenceAssemblies.NetStandard20,
			AnalyzerConfigOptions = { ("Analyzer_Config_Key", "Analyzer_Config_Value") },
		});

		await benchmark.InvokeAsync();

		await benchmark.InspectAsync(new CSharpDiagnosticAnalyzerBenchmarkInspectionContext
		{
			Diagnostics =
			{
				CreateDiagnostic(0, "MyClass0", TextSpan.FromBounds(59, 67), langVersion, allowUnsafe, metadataReference, true),
				CreateDiagnostic(1, "MyClass1", TextSpan.FromBounds(93, 101), langVersion, allowUnsafe, metadataReference, true),
				CreateDiagnostic(2, "MyClass2", TextSpan.FromBounds(127, 135), langVersion, allowUnsafe, metadataReference, true),
				CreateDiagnostic(3, "MyStruct3", TextSpan.FromBounds(7, 16), langVersion, allowUnsafe, metadataReference, true),
				CreateDiagnostic(4, "MyStruct4", TextSpan.FromBounds(28, 37), langVersion, allowUnsafe, metadataReference, true),
				CreateDiagnostic(5, "MyStruct5", TextSpan.FromBounds(7, 16), langVersion, allowUnsafe, metadataReference, true),
				CreateAdditionalDiagnostic(6, "Additional Text 6", TextSpan.FromBounds(0, 17), langVersion, allowUnsafe, metadataReference, true),
				CreateAdditionalDiagnostic(7, "Additional Text 7", TextSpan.FromBounds(19, 36), langVersion, allowUnsafe, metadataReference, true),
				CreateAdditionalDiagnostic(8, "Additional Text 8", TextSpan.FromBounds(38, 55), langVersion, allowUnsafe, metadataReference, true),
				CreateAdditionalDiagnostic(9, "Additional Text 9", TextSpan.FromBounds(0, 17), langVersion, allowUnsafe, metadataReference, true),
			},
		});
	}

	[Theory]
	[InlineData("struct TypeName { }", 1)]
	[InlineData("struct TypeName { } class NamedType { }", 2)]
	public async Task Invoke_Multiple_Operations(string source, int numberOfTypes)
	{
		CSharpDiagnosticAnalyzerBenchmark<TestCSharpDiagnosticAnalyzer> benchmark = new();

		benchmark.Initialize(new CSharpDiagnosticAnalyzerBenchmarkInitializationContext
		{
			Source = source,
		});

		benchmark.Analyzer.Initializations.Should().Be(0);
		benchmark.Analyzer.Executions.Should().Be(0 * numberOfTypes);
		await benchmark.InvokeAsync();
		benchmark.Analyzer.Initializations.Should().Be(1);
		benchmark.Analyzer.Executions.Should().Be(1 * numberOfTypes);
		await benchmark.InvokeAsync();
		benchmark.Analyzer.Initializations.Should().Be(2);
		benchmark.Analyzer.Executions.Should().Be(2 * numberOfTypes);
	}

	private static AdhocDiagnostic CreateDiagnostic(int markupLocation, string messageArgument, TextSpan additionalLocation, LanguageVersion langVersion, bool allowUnsafe, string metadataReference, bool hasOptions)
	{
		AdhocDiagnostic diagnostic = new(markupLocation)
		{
			Id = "ID0001",
			Category = "Test-Category",
			Message = $"Test-MessageFormat: {messageArgument}",
			MessageFormat = "Test-MessageFormat: {0}",
			Severity = DiagnosticSeverity.Error,
			DefaultSeverity = DiagnosticSeverity.Warning,
			IsEnabledByDefault = true,
			WarningLevel = 0,
			IsSuppressed = false,
			Title = "Test-Title",
			Description = "Test-Description",
			HelpLink = "Test-HelpLinkUri",
			AdditionalLocations = { Location.Create(String.Empty, additionalLocation, new LinePositionSpan()) },
			CustomTags = { "Test-Tag" },
		};

		AddProperties(diagnostic, langVersion, allowUnsafe, metadataReference, hasOptions);

		return diagnostic;
	}

	private static AdhocDiagnostic CreateAdditionalDiagnostic(int markupLocation, string messageArgument, TextSpan additionalLocation, LanguageVersion langVersion, bool allowUnsafe, string metadataReference, bool hasOptions)
	{
		AdhocDiagnostic diagnostic = new(markupLocation)
		{
			Id = "ID0002",
			Category = "Additional-Category",
			Message = $"Additional-MessageFormat: {messageArgument}",
			MessageFormat = "Additional-MessageFormat: {0}",
			Severity = DiagnosticSeverity.Error,
			DefaultSeverity = DiagnosticSeverity.Warning,
			IsEnabledByDefault = true,
			WarningLevel = 0,
			IsSuppressed = false,
			Title = "Additional-Title",
			Description = "Additional-Description",
			HelpLink = "Additional-HelpLinkUri",
			AdditionalLocations = { Location.Create(String.Empty, additionalLocation, new LinePositionSpan()) },
			CustomTags = { "Additional-Tag" },
		};

		AddProperties(diagnostic, langVersion, allowUnsafe, metadataReference, hasOptions);

		return diagnostic;
	}

	private static void AddProperties(AdhocDiagnostic diagnostic, LanguageVersion langVersion, bool allowUnsafe, string metadataReference, bool hasOptions)
	{
		diagnostic.Properties.Add(nameof(LanguageVersion), langVersion.ToString());
		diagnostic.Properties.Add("AllowUnsafe", allowUnsafe.ToString());
		diagnostic.Properties.Add("MetadataReference", metadataReference);

		if (hasOptions)
		{
			diagnostic.Properties.Add("Analyzer_Config_Key", "Analyzer_Config_Value");
		}
	}
}
