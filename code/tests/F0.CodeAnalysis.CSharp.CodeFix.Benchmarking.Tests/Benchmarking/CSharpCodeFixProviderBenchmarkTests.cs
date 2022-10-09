using Basic.Reference.Assemblies;
using F0.CodeAnalysis.CSharp.Benchmarking;
using F0.CodeAnalysis.CSharp.Diagnostics;
using F0.Testing;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace F0.CodeAnalysis.CSharp.Tests.Benchmarking;

public class CSharpCodeFixProviderBenchmarkTests
{
	[Fact]
	public void Initialize_Null_Throws()
	{
		CSharpCodeFixProviderBenchmark<NullCodeFixProvider> benchmark = new();

		Task initialize() => benchmark.InitializeAsync(null!);

		Test.That(initialize).ThrowsSynchronously<ArgumentNullException>().ParamName.Should().Be("context");
	}

	[Fact]
	public void Inspect_Null_Throws()
	{
		CSharpCodeFixProviderBenchmark<NullCodeFixProvider> benchmark = new();

		Task inspect() => benchmark.InspectAsync(null!);

		Test.That(inspect).ThrowsSynchronously<ArgumentNullException>().ParamName.Should().Be("context");
	}

	[Fact]
	public async Task Invoke_Without_Initialize_Throws()
	{
		CSharpCodeFixProviderBenchmark<NullCodeFixProvider> benchmark = new();

		Task invoke() => benchmark.InvokeAsync();

		await Assert.ThrowsAsync<NullReferenceException>(invoke);
	}

	[Fact]
	public async Task NullFixer_III_DoesNotThrow()
	{
		CSharpCodeFixProviderBenchmark<NullCodeFixProvider> benchmark = new();

		await benchmark.InitializeAsync(new CSharpCodeFixProviderBenchmarkInitializationContext());

		await benchmark.InvokeAsync();

		await benchmark.InspectAsync(new CSharpCodeFixProviderBenchmarkInspectionContext());
	}

	[Fact]
	public async Task TestFixer_Default_DoesNotThrow()
	{
		CSharpCodeFixProviderBenchmark<TestCSharpCodeFixProvider> benchmark = new();

		TestCSharpDiagnosticAnalyzer analyzer = new();

#if NET6_0
		const string metadataReference = "System.Private.CoreLib";
#elif NET472
		const string metadataReference = "mscorlib";
#else
		throw new InvalidOperationException("Unexpected Target Framework");
#endif

		string test = @"
using System;

namespace MyNamespace
{
	class TypeName
	{
	}
}
";

		string fixtest = $@"
using System;

namespace MyNamespace
{{
// AllowUnsafe: False
// LanguageVersion: 10.0
// MetadataReference: {metadataReference}
	class TYPENAME
	{{
	}}
}}
";

		await benchmark.InitializeAsync(new CSharpCodeFixProviderBenchmarkInitializationContext
		{
			Source = test,
			Analyzers = { analyzer },
		});

		await benchmark.InvokeAsync();

		await benchmark.InspectAsync(new CSharpCodeFixProviderBenchmarkInspectionContext
		{
			Source = fixtest,
		});
	}

	[Fact]
	public async Task TestFixer_NonDefault_DoesNotThrow()
	{
		CSharpCodeFixProviderBenchmark<TestCSharpCodeFixProvider> benchmark = new();

		TestCSharpDiagnosticAnalyzer analyzer = new();

		string test = @"
using System;

namespace MyNamespace
{
	{|#0:// Comment|}
	class TypeName
	{
		int {|#1:myField|};
		int {|#2:MyProperty|} { get; set; }
	}
}
";

		string fixtest = @"
using System;

namespace MyNamespace
{
// AllowUnsafe: True
// LanguageVersion: 7.3
// MetadataReference: netstandard
	// COMMENT
	class TYPENAME
	{
		int MYFIELD;
		int MYPROPERTY { get; set; }
	}
}
";

		await benchmark.InitializeAsync(new CSharpCodeFixProviderBenchmarkInitializationContext
		{
			Source = test,
			AdditionalSources =
			{
				"class MyClass { }",
				"struct MyStruct { }",
			},
			AdditionalTexts = { },
			ParseOptions = new CSharpParseOptions(LanguageVersion.CSharp7_3),
			CompilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary).WithAllowUnsafe(true),
			MetadataReferences = ReferenceAssemblies.NetStandard20,
			AnalyzerConfigOptions = { },
			Diagnostics =
			{
				new AdhocDiagnostic(0, TestCSharpCodeFixProvider.TriviaId, TestCSharpCodeFixProvider.TriviaCategory),
				new AdhocDiagnostic(1, TestCSharpDiagnosticAnalyzer.Rule.Id, TestCSharpDiagnosticAnalyzer.Rule.Category),
				new AdhocDiagnostic(2, TestCSharpDiagnosticAnalyzer.Rule.Id, TestCSharpDiagnosticAnalyzer.Rule.Category),
			},
			Analyzers = { analyzer },
		});

		await benchmark.InvokeAsync();

		await benchmark.InspectAsync(new CSharpCodeFixProviderBenchmarkInspectionContext
		{
			Source = fixtest,
			AdditionalSources =
			{
				"class MYCLASS { }",
				"struct MYSTRUCT { }",
			},
			Diagnostics = { },
		});
	}

	[Theory]
	[InlineData("class MyClass { }", "class MYCLASS { }", 1)]
	[InlineData("class MyClass { } struct MyStruct { }", "class MYCLASS { } struct MYSTRUCT { }", 2)]
	public async Task Invoke_Multiple_Operations(string test, string fixtest, int numberOfTypes)
	{
		CSharpCodeFixProviderBenchmark<TestCSharpCodeFixProvider> benchmark = new();

		TestCSharpDiagnosticAnalyzer analyzer = new();

		analyzer.Initializations.Should().Be(0);
		analyzer.Executions.Should().Be(0 * numberOfTypes);
		benchmark.Fixer.Registrations.Should().Be(0 * numberOfTypes);
		benchmark.Fixer.Invocations.Should().Be(0 * numberOfTypes);

		await benchmark.InitializeAsync(new CSharpCodeFixProviderBenchmarkInitializationContext
		{
			Source = test,
			Analyzers = { analyzer },
		});

		analyzer.Initializations.Should().Be(1);
		analyzer.Executions.Should().Be(1 * numberOfTypes);
		benchmark.Fixer.Registrations.Should().Be(0 * numberOfTypes);
		benchmark.Fixer.Invocations.Should().Be(0 * numberOfTypes);

		await benchmark.InvokeAsync();

		analyzer.Initializations.Should().Be(1);
		analyzer.Executions.Should().Be(1 * numberOfTypes);
		benchmark.Fixer.Registrations.Should().Be(1 * numberOfTypes);
		benchmark.Fixer.Invocations.Should().Be(1 * numberOfTypes);

		await benchmark.InvokeAsync();

		analyzer.Initializations.Should().Be(1);
		analyzer.Executions.Should().Be(1 * numberOfTypes);
		benchmark.Fixer.Registrations.Should().Be(2 * numberOfTypes);
		benchmark.Fixer.Invocations.Should().Be(2 * numberOfTypes);

		analyzer.ClearCounters();
		await benchmark.InspectAsync(new CSharpCodeFixProviderBenchmarkInspectionContext
		{
			Source = fixtest,
		});

		analyzer.Initializations.Should().Be(numberOfTypes);
		analyzer.Executions.Should().Be(numberOfTypes * numberOfTypes);
		benchmark.Fixer.Registrations.Should().Be(3 * numberOfTypes);
		benchmark.Fixer.Invocations.Should().Be(3 * numberOfTypes);
	}

	private static AdhocDiagnostic CreateDiagnostic(int markupLocation, string id, string category)
	{
		AdhocDiagnostic diagnostic = new(markupLocation, id, category);
		return diagnostic;
	}
}
