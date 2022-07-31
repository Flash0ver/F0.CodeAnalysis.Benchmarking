using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace F0.CodeAnalysis.CSharp.Diagnostics;

internal sealed class AdhocAnalyzerConfigOptions : AnalyzerConfigOptions
{
	private readonly ImmutableDictionary<string, string> options;

	public AdhocAnalyzerConfigOptions(ICollection<(string Key, string Value)> options)
	{
		ImmutableDictionary<string, string>.Builder builder = ImmutableDictionary.CreateBuilder<string, string>(KeyComparer);
		foreach ((string Key, string Value) in options)
		{
			builder.Add(Key, Value);
		}
		this.options = builder.ToImmutable();
	}

	public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
		=> options.TryGetValue(key, out value);
}
