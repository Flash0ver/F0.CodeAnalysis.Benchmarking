using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using F0.CodeAnalysis.CSharp.Collections.Generic;
using F0.CodeAnalysis.CSharp.Diagnostics;
using F0.CodeAnalysis.CSharp.Inspection;
using F0.CodeAnalysis.CSharp.Markup;
using F0.CodeAnalysis.CSharp.Syntax;
using F0.CodeAnalysis.CSharp.Workspaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using static System.Net.Mime.MediaTypeNames;

namespace F0.CodeAnalysis.CSharp.Benchmarking;

public sealed class CSharpCodeFixProviderBenchmark<TCodeFixProvider>
	where TCodeFixProvider : CodeFixProvider, new()
{
	private ImmutableArray<Diagnostic> diagnostics;
	private ImmutableArray<DiagnosticAnalyzer> analyzers;
	private Project project;
	private ImmutableArray<CodeFixContext> codeFixContexts;

	private readonly ImmutableArray<CodeAction>.Builder codeActions = ImmutableArray.CreateBuilder<CodeAction>();

	public CSharpCodeFixProviderBenchmark()
	{
		Fixer = new TCodeFixProvider();

		project = null!;
	}

	internal TCodeFixProvider Fixer { get; }

	public Task InitializeAsync(CSharpCodeFixProviderBenchmarkInitializationContext context)
	{
		_ = context ?? throw new ArgumentNullException(nameof(context));

		return Initialize();

		async Task Initialize()
		{
			this.analyzers = ImmutableArray.CreateRange(context.Analyzers);

			project = CreateProject(context, out diagnostics);

			CSharpDiagnosticAnalyzerReporter reporter = new(diagnostics);
			ImmutableArray<DiagnosticAnalyzer> analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(reporter).AddRange(context.Analyzers);

			AnalyzerConfigOptionsProvider? optionsProvider = context.AnalyzerConfigOptions.Count > 0 ? new AdhocAnalyzerConfigOptionsProvider(context.AnalyzerConfigOptions) : null;
			AnalyzerOptions? options = CreateAnalyzerOptions(ImmutableArray<AdditionalText>.Empty, optionsProvider);

			Compilation? compilation = await project.GetCompilationAsync(CancellationToken.None).ConfigureAwait(false);
			Debug.Assert(compilation is not null, $"{nameof(project.SupportsCompilation)} returned {project.SupportsCompilation}.");
			CompilationWithAnalyzers compilationWithAnalyzers = compilation.WithAnalyzers(analyzers, options, CancellationToken.None);

			IEnumerable<Document> documents = project.Documents;
			AnalysisResult analysisResult = await compilationWithAnalyzers.GetAnalysisResultAsync(CancellationToken.None).ConfigureAwait(false);
			codeFixContexts = ImmutableArray.CreateRange(analysisResult.GetAllDiagnostics(), (Diagnostic diagnostic) =>
			{
				string path = diagnostic.Location.GetLineSpan().Path;
				Document document = documents.Single((Document document) => document.FilePath == path);
				return new CodeFixContext(document, diagnostic, RegisterCodeFix, CancellationToken.None);
			});
		}
	}

	public async Task InvokeAsync()
	{
		codeActions.Clear();

		foreach (CodeFixContext codeFixContext in codeFixContexts)
		{
			await Fixer.RegisterCodeFixesAsync(codeFixContext).ConfigureAwait(false);
		}

		foreach (CodeAction codeAction in codeActions)
		{
			_ = await codeAction.GetOperationsAsync(CancellationToken.None).ConfigureAwait(false);
		}
	}

	public Task InspectAsync(CSharpCodeFixProviderBenchmarkInspectionContext context)
	{
		_ = context ?? throw new ArgumentNullException(nameof(context));

		return Inspect();

		async Task Inspect()
		{
			var compilationOptions = project.CompilationOptions as CSharpCompilationOptions;
			var parseOptions = project.ParseOptions as CSharpParseOptions;
			IEnumerable<MetadataReference> metadataReferences = project.MetadataReferences;
			AnalyzerOptions options = project.AnalyzerOptions;

			Document[] documents = project.Documents.ToArray();
			ImmutableArray<Document>.Builder fixedDocuments = ImmutableArray.CreateBuilder<Document>(documents.Length);

			for (int i = 0; i < documents.Length; i++)
			{
				Document document = documents[i];

				string? path = document.FilePath;
				Debug.Assert(path is not null, $"There is no document file.");

				var diagnostics = this.diagnostics.Where((Diagnostic diagnostic) => diagnostic.Location.GetLineSpan().Path == path).ToImmutableArray();
				var codeFixContexts = this.codeFixContexts.Where((CodeFixContext context) => context.Document == document).ToImmutableArray();

				SourceText text = await document.GetTextAsync().ConfigureAwait(false);
				document = Adhoc.CreateDocument(text.ToString(), compilationOptions, parseOptions, metadataReferences);

				Document fixedDocument = await InspectDocument(document, options, diagnostics, codeFixContexts).ConfigureAwait(false);
				fixedDocuments.Add(fixedDocument);
			}

			var expected = context.Source.ToEnumerable().Concat(context.AdditionalSources);
			await CodeFixInspector.DocumentsAsync(expected.ToImmutableArray(), fixedDocuments.ToImmutable()).ConfigureAwait(false);
		}

		async Task<Document> InspectDocument(Document document, AnalyzerOptions options, ImmutableArray<Diagnostic> documentDiagnostics, ImmutableArray<CodeFixContext> documentCodeFixContexts)
		{
			SyntaxNode? root = await document.GetSyntaxRootAsync(CancellationToken.None).ConfigureAwait(false);
			Debug.Assert(root is not null, $"{nameof(document.SupportsSyntaxTree)} returned {document.SupportsSyntaxTree}.");
			CSharpDiagnosticSyntaxRewriter syntaxRewriter = new(documentDiagnostics);
			SyntaxNode result = syntaxRewriter.Visit(root);
			document = document.WithSyntaxRoot(result);

			if (documentDiagnostics.Length != syntaxRewriter.DiagnosticAnnotations.Count)
			{
				throw new InvalidOperationException($"Expected {documentDiagnostics.Length} diagnostics from input markup, but annotated {syntaxRewriter.DiagnosticAnnotations.Count}.");
			}

			foreach (DiagnosticAnnotation diagnosticAnnotation in syntaxRewriter.DiagnosticAnnotations)
			{
				codeActions.Clear();

				CSharpDiagnosticAnnotationReporter analyzer = new(diagnosticAnnotation);
				var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(analyzer);

				Compilation? compilation = await document.Project.GetCompilationAsync(CancellationToken.None).ConfigureAwait(false);
				Debug.Assert(compilation is not null);
				CompilationWithAnalyzers compilationWithAnalyzers = compilation.WithAnalyzers(analyzers, options, CancellationToken.None);
				AnalysisResult analysisResult = await compilationWithAnalyzers.GetAnalysisResultAsync(CancellationToken.None).ConfigureAwait(false);
				ImmutableArray<Diagnostic> diagnostics = analysisResult.GetAllDiagnostics();

				Debug.Assert(diagnostics.Length == 1, $"Expected {typeof(CSharpDiagnosticAnnotationReporter)} to report one diagnostics, but actually reported {diagnostics.Length}.");

				Diagnostic diagnostic = diagnostics[0];

				CodeFixContext fixerContext = new(document, diagnostic, RegisterCodeFix, CancellationToken.None);
				await Fixer.RegisterCodeFixesAsync(fixerContext).ConfigureAwait(false);

				if (TryGetCodeAction(out CodeAction? codeAction))
				{
					ImmutableArray<CodeActionOperation> operations = await codeAction.GetOperationsAsync(CancellationToken.None).ConfigureAwait(false);
					ApplyChangesOperation edit = operations.OfType<ApplyChangesOperation>().Single();
					Document? changedDocument = edit.ChangedSolution.GetDocument(document.Id);
					Debug.Assert(changedDocument is not null);
					document = changedDocument;
				}
			}

			Debug.Assert(documentCodeFixContexts.Length >= documentDiagnostics.Length, "documentCodeFixContexts.Length >= documentDiagnostics.Length", $"{documentCodeFixContexts.Length} >= {documentDiagnostics.Length}");

			if (!analyzers.IsEmpty)
			{
				for (int i = documentDiagnostics.Length; i < documentCodeFixContexts.Length; i++)
				{
					codeActions.Clear();

					Compilation? compilation = await document.Project.GetCompilationAsync(CancellationToken.None).ConfigureAwait(false);
					Debug.Assert(compilation is not null);
					CompilationWithAnalyzers compilationWithAnalyzers = compilation.WithAnalyzers(analyzers, options, CancellationToken.None);
					AnalysisResult analysisResult = await compilationWithAnalyzers.GetAnalysisResultAsync(CancellationToken.None).ConfigureAwait(false);
					ImmutableArray<Diagnostic> diagnostics = analysisResult.GetAllDiagnostics();

					if (diagnostics.Length != documentCodeFixContexts.Length - i)
					{
						throw new InvalidOperationException($"Unexpected number of unfixed diagnostics in {nameof(Document)} {document.FilePath}: expected {1 + i}/{documentCodeFixContexts.Length}, actual {1 + documentCodeFixContexts.Length - diagnostics.Length}/{documentCodeFixContexts.Length}");
					}

					Diagnostic diagnostic = diagnostics[0];

					CodeFixContext fixerContext = new(document, diagnostic, RegisterCodeFix, CancellationToken.None);
					await Fixer.RegisterCodeFixesAsync(fixerContext).ConfigureAwait(false);

					if (TryGetCodeAction(out CodeAction? codeAction))
					{
						ImmutableArray<CodeActionOperation> operations = await codeAction.GetOperationsAsync(CancellationToken.None).ConfigureAwait(false);
						ApplyChangesOperation edit = operations.OfType<ApplyChangesOperation>().Single();
						Document? changedDocument = edit.ChangedSolution.GetDocument(document.Id);
						Debug.Assert(changedDocument is not null);
						document = changedDocument;
					}
				}
			}

			return document;
		}

		bool TryGetCodeAction([NotNullWhen(true)] out CodeAction? codeAction)
		{
			if (codeActions.Count > 1)
			{
				throw new InvalidOperationException($"More than one {nameof(CodeAction)} is not supported: {codeActions.Count}");
			}

			if (codeActions.Count == 1)
			{
				codeAction = codeActions.Single();
				return true;
			}

			Debug.Assert(codeActions.Count == 0);
			codeAction = null;
			return false;
		}
	}

	private void RegisterCodeFix(CodeAction codeAction, ImmutableArray<Diagnostic> diagnostics)
		=> codeActions.Add(codeAction);

	private static Project CreateProject(CSharpCodeFixProviderBenchmarkInitializationContext context, out ImmutableArray<Diagnostic> diagnostics)
	{
		ImmutableArray<Location>.Builder builder = ImmutableArray.CreateBuilder<Location>(context.Diagnostics.Count);

		IEnumerable<string> sources = context.Source.ToEnumerable().Concat(context.AdditionalSources);
		IEnumerable<string> parsed = sources.Select((source, index) =>
		{
			string sanitized = MarkupParser.Parse(CreateFilePath(index), source, out ImmutableArray<Location> locations);
			builder.AddRange(locations);
			return sanitized;
		});
		IEnumerable<(string Path, string Source)> additionalTexts = context.AdditionalTexts.Select((source, index) =>
		{
			string sanitized = MarkupParser.Parse(CreateFilePath(index), source.Text, out ImmutableArray<Location> locations);
			builder.AddRange(locations);
			return (source.Path, sanitized);
		});

		var texts = parsed.ToImmutableArray();
		ImmutableArray<Location> locations = builder.ToImmutable();

		diagnostics = context.Diagnostics
			.Select((diagnostic, index) => CreateDiagnostic(diagnostic, index, locations))
			.ToImmutableArray();

		return Adhoc.CreateProject(texts, context.CompilationOptions, context.ParseOptions, context.MetadataReferences);
	}

	private static Diagnostic CreateDiagnostic(AdhocDiagnostic diagnostic, int index, ImmutableArray<Location> locations)
	{
		return Diagnostic.Create(
			diagnostic.Id ?? throw new InvalidOperationException($"Diagnostic with markup location {diagnostic.MarkupLocation} requires an {nameof(diagnostic.Id)}."),
			diagnostic.Category ?? throw new InvalidOperationException($"Diagnostic with markup location {diagnostic.MarkupLocation} requires a {nameof(diagnostic.Category)}."),
			diagnostic.Message,
			diagnostic.Severity ?? DiagnosticSeverity.Warning,
			diagnostic.DefaultSeverity ?? DiagnosticSeverity.Warning,
			diagnostic.IsEnabledByDefault.GetValueOrDefault(true),
			diagnostic.WarningLevel.GetValueOrDefault(1),
			diagnostic.Title,
			diagnostic.Description,
			diagnostic.HelpLink,
			diagnostic.Location ?? locations[index],
			diagnostic.AdditionalLocations,
			diagnostic.CustomTags,
			diagnostic.Properties.ToImmutableDictionary()
		);
	}

	private static AnalyzerOptions? CreateAnalyzerOptions(ImmutableArray<AdditionalText> additionalFiles, AnalyzerConfigOptionsProvider? optionsProvider)
	{
		return optionsProvider is null
			? additionalFiles.IsDefaultOrEmpty
				? null
				: new AnalyzerOptions(additionalFiles)
			: new AnalyzerOptions(additionalFiles, optionsProvider);
	}

	private static string CreateFilePath(int index)
	{
		const string fileName = "Benchmark";
		const string fileExtension = ".cs";

		return $"/0/{fileName}{index}{fileExtension}";
	}
}
