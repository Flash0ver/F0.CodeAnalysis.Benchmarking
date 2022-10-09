using Microsoft.CodeAnalysis;

namespace F0.CodeAnalysis.CSharp.Diagnostics;

internal sealed class DiagnosticAnnotation
{
	public DiagnosticAnnotation(Diagnostic diagnostic, SyntaxAnnotation annotation)
	{
		Diagnostic = diagnostic;
		Annotation = annotation;
	}

	public Diagnostic Diagnostic { get; }
	public SyntaxAnnotation Annotation { get; }
}
