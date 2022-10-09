using System.Collections.Immutable;
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

	public AdhocDiagnostic(int markupLocation, DiagnosticDescriptor descriptor)
	{
		_ = descriptor ?? throw new ArgumentNullException(nameof(descriptor));

		MarkupLocation = markupLocation;
		Id = descriptor.Id;
		Title = descriptor.Title;
		Description = descriptor.Description;
		HelpLink = descriptor.HelpLinkUri;
		MessageFormat = descriptor.MessageFormat;
		Category = descriptor.Category;
		DefaultSeverity = descriptor.DefaultSeverity;
		IsEnabledByDefault = descriptor.IsEnabledByDefault;
		CustomTags = descriptor.CustomTags.ToArray();
	}

	public AdhocDiagnostic(int markupLocation, string id, string category)
		=> (MarkupLocation, Id, Category) = (markupLocation, id, category);

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

	internal DiagnosticDescriptor GetDescriptor()
	{
		return new DiagnosticDescriptor(
			Id ?? throw new InvalidOperationException($"Diagnostic with markup location {MarkupLocation} requires an {nameof(Id)}."),
			Title ?? "TODO",
			MessageFormat ?? "TODO",
			Category ?? "TODO",
			DefaultSeverity ?? DiagnosticSeverity.Warning,
			IsEnabledByDefault.GetValueOrDefault(true),
			Description,
			HelpLink,
			CustomTags.ToArray()
		);
	}

	internal static Diagnostic CreateDiagnostic(AdhocDiagnostic diagnostic, Location location)
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
			location,
			diagnostic.AdditionalLocations,
			diagnostic.CustomTags,
			diagnostic.Properties.ToImmutableDictionary()
		);
	}
}
