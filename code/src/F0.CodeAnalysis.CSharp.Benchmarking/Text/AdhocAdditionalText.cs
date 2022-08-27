using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace F0.CodeAnalysis.CSharp.Text;

internal sealed class AdhocAdditionalText : AdditionalText
{
	private readonly string path;
	private readonly string? text;
	private readonly Encoding? encoding;
	private readonly SourceHashAlgorithm checksumAlgorithm;

	public AdhocAdditionalText(string path, string? text = null, Encoding? encoding = null, SourceHashAlgorithm checksumAlgorithm = SourceHashAlgorithm.Sha1)
	{
		this.path = path;
		this.text = text;
		this.encoding = encoding;
		this.checksumAlgorithm = checksumAlgorithm;
	}

	public override string Path => path;

	public override SourceText? GetText(CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		return text is null
			? null
			: SourceText.From(text, encoding, checksumAlgorithm);
	}
}
