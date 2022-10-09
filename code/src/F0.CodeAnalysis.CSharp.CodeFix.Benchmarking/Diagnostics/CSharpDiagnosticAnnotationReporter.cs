using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace F0.CodeAnalysis.CSharp.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpDiagnosticAnnotationReporter : DiagnosticAnalyzer
{
	private readonly DiagnosticAnnotation diagnosticAnnotation;

	public CSharpDiagnosticAnnotationReporter(DiagnosticAnnotation diagnosticAnnotation)
	{
		this.diagnosticAnnotation = diagnosticAnnotation;

		SupportedDiagnostics = ImmutableArray.Create(diagnosticAnnotation.Diagnostic.Descriptor);
	}

	public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

	public override void Initialize(AnalysisContext context)
	{
		context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
		context.EnableConcurrentExecution();

		context.RegisterCompilationAction(AnalyzeCompilation);
	}

	private void AnalyzeCompilation(CompilationAnalysisContext context)
	{
		int reportedDiagnostics = 0;

		foreach (SyntaxTree tree in context.Compilation.SyntaxTrees)
		{
			SyntaxNode root = tree.GetRoot(context.CancellationToken);

			if (root.ContainsAnnotations)
			{
				SyntaxNodeOrToken[] nodesOrTokens = root.GetAnnotatedNodesAndTokens(diagnosticAnnotation.Annotation).ToArray();
				Debug.Assert(nodesOrTokens.Length <= 1);

				if (nodesOrTokens.Length != 0)
				{
					SyntaxNodeOrToken nodeOrToken = nodesOrTokens.Single();
					Location? location = nodeOrToken.GetLocation();
					Debug.Assert(location is not null, $"Token {nodeOrToken} has no parent.");

					Diagnostic diagnostic = CreateDiagnostic(diagnosticAnnotation.Diagnostic, location);
					context.ReportDiagnostic(diagnostic);
					reportedDiagnostics++;
				}
				else
				{
					SyntaxTrivia[] trivias = root.GetAnnotatedTrivia(diagnosticAnnotation.Annotation).ToArray();
					Debug.Assert(trivias.Length == 1);
					SyntaxTrivia trivia = trivias.Single();

					if (trivia.SyntaxTree is not null)
					{
						Location location = trivia.GetLocation();

						Diagnostic diagnostic = CreateDiagnostic(diagnosticAnnotation.Diagnostic, location);
						context.ReportDiagnostic(diagnostic);
						reportedDiagnostics++;
					}
					else
					{
						throw new InvalidOperationException($"Trivia {trivia} is not supported.");
					}
				}
			}
		}

		Debug.Assert(reportedDiagnostics == 1, $"Expected one {nameof(Diagnostic)} to be reported, but actually reported {reportedDiagnostics}.");
	}

	private static Diagnostic CreateDiagnostic(Diagnostic diagnostic, Location location)
	{
		return Diagnostic.Create(
			diagnostic.Descriptor,
			location,
			diagnostic.Severity,
			diagnostic.AdditionalLocations,
			diagnostic.Properties,
			Array.Empty<object>());
	}
}
