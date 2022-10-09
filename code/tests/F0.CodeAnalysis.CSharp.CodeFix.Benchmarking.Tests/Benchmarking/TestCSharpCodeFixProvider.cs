using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Reflection;
using System.Xml.Linq;
using FluentAssertions.Equivalency;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace F0.CodeAnalysis.CSharp.Tests.Benchmarking;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TestCSharpCodeFixProvider))]
[Shared]
internal sealed class TestCSharpCodeFixProvider : CodeFixProvider
{
	private const string CodeFixTitle = "Make uppercase";
	private const string TriviaCodeFixTitle = "Make comment uppercase";

	internal const string TriviaId = "ID0002";
	internal const string TriviaCategory = "Trivia-Category";

	public int Registrations { get; private set; }
	public int Invocations { get; private set; }

	public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(TestCSharpDiagnosticAnalyzer.DiagnosticId, TriviaId);

	public override FixAllProvider? GetFixAllProvider()
		=> WellKnownFixAllProviders.BatchFixer;

	public override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		Registrations++;

		SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

		Diagnostic diagnostic = context.Diagnostics.Single();
		TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

		if (diagnostic.Id == TestCSharpDiagnosticAnalyzer.DiagnosticId)
		{
			CSharpSyntaxNode declaration = root!.FindToken(diagnosticSpan.Start).Parent!.AncestorsAndSelf().OfType<CSharpSyntaxNode>().Where(HasIdentifier).First();

			context.RegisterCodeFix(
				CodeAction.Create(
					CodeFixTitle,
					cancellationToken => MakeUppercaseAsync(diagnostic, context.Document, declaration, cancellationToken),
					CodeFixTitle),
				diagnostic);
		}
		else
		{
			Debug.Assert(diagnostic.Id == TriviaId, $"{nameof(diagnostic.Id)} {diagnostic.Id} is not supported.");

			SyntaxTrivia trivia = root!.FindTrivia(diagnosticSpan.Start);

			context.RegisterCodeFix(
				CodeAction.Create(
					TriviaCodeFixTitle,
					cancellationToken => MakeUppercaseAsync(context.Document, trivia, cancellationToken),
					TriviaCodeFixTitle),
				diagnostic);
		}
	}

	private async Task<Solution> MakeUppercaseAsync(Diagnostic diagnostic, Document document, CSharpSyntaxNode node, CancellationToken cancellationToken)
	{
		Invocations++;

		if (!diagnostic.Properties.IsEmpty)
		{
			IEnumerable<string> properties = diagnostic.Properties.OrderBy(static property => property.Key).Select(static property => $"// {property.Key}: {property.Value}");
			string text = String.Join(Environment.NewLine, properties) + Environment.NewLine;
			SyntaxTrivia comment = SyntaxFactory.Comment(text);

			CSharpSyntaxNode newNode = node.HasLeadingTrivia
				? node.WithLeadingTrivia(node.GetLeadingTrivia().Insert(0, comment))
				: node.WithLeadingTrivia(comment);

			SyntaxAnnotation annotation = new();
			newNode = newNode.WithAdditionalAnnotations(annotation);

			DocumentEditor editor = await DocumentEditor.CreateAsync(document, cancellationToken);

			editor.ReplaceNode(node, newNode);

			document = editor.GetChangedDocument();
			SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken);
			node = root!.DescendantNodes().OfType<CSharpSyntaxNode>().Single(node => node.HasAnnotation(annotation));
		}

		SyntaxToken identifierToken = GetIdentifier(node);
		string newName = identifierToken.Text.ToUpperInvariant();

		SemanticModel? semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
		ISymbol? typeSymbol = semanticModel!.GetDeclaredSymbol(node, cancellationToken);

		Solution originalSolution = document.Project.Solution;
		OptionSet optionSet = originalSolution.Workspace.Options;
		Solution newSolution = await Renamer.RenameSymbolAsync(originalSolution, typeSymbol!, newName, optionSet, cancellationToken).ConfigureAwait(false);

		return newSolution;
	}

	private async Task<Document> MakeUppercaseAsync(Document document, SyntaxTrivia trivia, CancellationToken cancellationToken)
	{
		Invocations++;

		Debug.Assert(trivia.IsKind(SyntaxKind.SingleLineCommentTrivia), $"{nameof(SyntaxKind)} {trivia.Kind()} is not supported.");

		string text = trivia.ToString();
		string newText = text.ToUpperInvariant();

		SyntaxTrivia newTrivia = SyntaxFactory.Comment(newText);

		SyntaxNode? root = await document.GetSyntaxRootAsync(cancellationToken);
		Debug.Assert(root is not null, $"{nameof(document.SupportsSyntaxTree)} returns false.");
		SyntaxNode newRoot = root.ReplaceTrivia(trivia, newTrivia);

		Document newDocument = document.WithSyntaxRoot(newRoot);

		return newDocument;
	}

	private static bool HasIdentifier(CSharpSyntaxNode node)
	{
		return node
			is TypeDeclarationSyntax
			or VariableDeclaratorSyntax
			or PropertyDeclarationSyntax;
	}

	private static SyntaxToken GetIdentifier(CSharpSyntaxNode node)
	{
		return node switch
		{
			TypeDeclarationSyntax typeDecl => typeDecl.Identifier,
			VariableDeclaratorSyntax variableDecl => variableDecl.Identifier,
			PropertyDeclarationSyntax propertyDecl => propertyDecl.Identifier,
			_ => throw new NotSupportedException($"{node.GetType().Name} is not supported."),
		};
	}
}
