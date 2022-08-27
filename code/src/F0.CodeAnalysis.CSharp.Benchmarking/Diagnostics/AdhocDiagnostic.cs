using System.Collections.ObjectModel;
using Microsoft.CodeAnalysis;

namespace F0.CodeAnalysis.CSharp.Diagnostics;

public sealed class AdhocDiagnostic
{
	internal AdhocDiagnostic()
	{
	}

	public AdhocDiagnostic(int markupLocation)
		=> MarkupLocation = markupLocation;

	public string? Id { get; init; }
	public string? Category { get; init; }
	public string? Message { get; init; }
	public LocalizableString? MessageFormat { get; init; }
	public DiagnosticSeverity? Severity { get; init; }
	public DiagnosticSeverity? DefaultSeverity { get; init; }
	public bool? IsEnabledByDefault { get; init; }
	public int? WarningLevel { get; init; }
	public bool? IsSuppressed { get; init; }
	public LocalizableString? Title { get; init; }
	public LocalizableString? Description { get; init; }
	public string? HelpLink { get; init; }
	internal Location? Location { get; private set; }
	public int? MarkupLocation { get; init; }
	public ICollection<Location> AdditionalLocations { get; init; } = new Collection<Location>();
	public ICollection<string> CustomTags { get; init; } = new Collection<string>();
	public IDictionary<string, string?> Properties { get; init; } = new Dictionary<string, string?>();

	internal void WithLocation(Location location)
		=> Location = location;
}
