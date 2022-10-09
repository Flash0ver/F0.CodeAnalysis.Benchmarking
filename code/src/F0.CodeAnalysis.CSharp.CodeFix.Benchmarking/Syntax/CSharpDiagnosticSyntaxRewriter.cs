using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using F0.CodeAnalysis.CSharp.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace F0.CodeAnalysis.CSharp.Syntax;

internal sealed class CSharpDiagnosticSyntaxRewriter : CSharpSyntaxRewriter
{
	private readonly ImmutableArray<Diagnostic> diagnostics;

	public CSharpDiagnosticSyntaxRewriter(ImmutableArray<Diagnostic> diagnostics)
		: base(true)
	{
		this.diagnostics = diagnostics;

		DiagnosticAnnotations = new List<DiagnosticAnnotation>(diagnostics.Length);
	}

	public List<DiagnosticAnnotation> DiagnosticAnnotations { get; }

	[return: NotNullIfNotNull("node")]
	public override SyntaxNode? Visit(SyntaxNode? node)
	{
		if (node is null)
		{
			return null;
		}

		Location location = node.GetLocation();

		foreach (Diagnostic diagnostic in diagnostics)
		{
			if (diagnostic.Location.SourceSpan == location.SourceSpan
				&& diagnostic.Location.GetLineSpan() == location.GetLineSpan())
			{
				SyntaxAnnotation annotation = new();
				SyntaxNode newNode = node.WithAdditionalAnnotations(annotation);

				DiagnosticAnnotations.Add(new DiagnosticAnnotation(diagnostic, annotation));

				return base.Visit(newNode);
			}
		}

		return base.Visit(node);
	}

	public override SyntaxToken VisitToken(SyntaxToken token)
	{
		if (token.SyntaxTree is null)
		{
			return base.VisitToken(token);
		}

		Location location = token.GetLocation();

		foreach (Diagnostic diagnostic in diagnostics)
		{
			if (diagnostic.Location.SourceSpan == location.SourceSpan
				&& diagnostic.Location.GetLineSpan() == location.GetLineSpan())
			{
				SyntaxAnnotation annotation = new();
				SyntaxToken newToken = token.WithAdditionalAnnotations(annotation);

				DiagnosticAnnotations.Add(new DiagnosticAnnotation(diagnostic, annotation));

				return base.VisitToken(newToken);
			}
		}

		return base.VisitToken(token);
	}

	public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
	{
		if (trivia.SyntaxTree is null)
		{
			return base.VisitTrivia(trivia);
		}

		Location location = trivia.GetLocation();

		foreach (Diagnostic diagnostic in diagnostics)
		{
			if (diagnostic.Location.SourceSpan == location.SourceSpan
				&& diagnostic.Location.GetLineSpan() == location.GetLineSpan())
			{
				SyntaxAnnotation annotation = new();
				SyntaxTrivia newTrivia = trivia.WithAdditionalAnnotations(annotation);

				DiagnosticAnnotations.Add(new DiagnosticAnnotation(diagnostic, annotation));

				return base.VisitTrivia(newTrivia);
			}
		}

		return base.VisitTrivia(trivia);
	}
}
