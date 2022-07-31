using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace F0.CodeAnalysis.CSharp.Diagnostics;

internal sealed class AdhocAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
{
	private readonly AnalyzerConfigOptions options;

	public AdhocAnalyzerConfigOptionsProvider(ICollection<(string Key, string Value)> options)
		=> this.options = new AdhocAnalyzerConfigOptions(options);

	public override AnalyzerConfigOptions GlobalOptions => options;

	public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
		=> throw new NotImplementedException($"The method {nameof(AdhocAnalyzerConfigOptionsProvider)}.{nameof(GetOptions)}({nameof(SyntaxTree)}) is not implemented.");

	public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
		=> throw new NotImplementedException($"The method {nameof(AdhocAnalyzerConfigOptionsProvider)}.{nameof(GetOptions)}({nameof(AdditionalText)}) is not implemented.");
}
