using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace F0.CodeAnalysis.CSharp.Examples.Benchmarking;

// https://github.com/dotnet/roslyn-sdk/blob/cd89910e5a9d3da3ee5e326618024c4aba68ff11/src/VisualStudio.Roslyn.SDK/Roslyn.SDK/ProjectTemplates/CSharp/Diagnostic/CodeFix/CodeFixProvider.cs
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CSharpCodeFixProvider))]
[Shared]
internal sealed class CSharpCodeFixProvider : CodeFixProvider
{
	private const string CodeFixTitle = "Make uppercase";

	public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CSharpDiagnosticAnalyzer.DiagnosticId);

	public override FixAllProvider? GetFixAllProvider()
		=> WellKnownFixAllProviders.BatchFixer;

	public override async Task RegisterCodeFixesAsync(CodeFixContext context)
	{
		SyntaxNode? root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

		Diagnostic diagnostic = context.Diagnostics.Single();
		TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

		TypeDeclarationSyntax declaration = root!.FindToken(diagnosticSpan.Start).Parent!.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().First();

		context.RegisterCodeFix(
			CodeAction.Create(
				CodeFixTitle,
				cancellationToken => MakeUppercaseAsync(context.Document, declaration, cancellationToken),
				CodeFixTitle),
			diagnostic);
	}

	private static async Task<Solution> MakeUppercaseAsync(Document document, TypeDeclarationSyntax typeDecl, CancellationToken cancellationToken)
	{
		SyntaxToken identifierToken = typeDecl.Identifier;
		string newName = identifierToken.Text.ToUpperInvariant();

		SemanticModel? semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
		ISymbol? typeSymbol = semanticModel!.GetDeclaredSymbol(typeDecl, cancellationToken);

		Solution originalSolution = document.Project.Solution;
		OptionSet optionSet = originalSolution.Workspace.Options;
		Solution newSolution = await Renamer.RenameSymbolAsync(originalSolution, typeSymbol!, newName, optionSet, cancellationToken).ConfigureAwait(false);

		return newSolution;
	}
}
