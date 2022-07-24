using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace F0.CodeAnalysis.CSharp.Diagnostics;

internal static class AdhocDiagnostics
{
	internal static ImmutableArray<AdhocDiagnostic> Empty { get; } = ImmutableArray<AdhocDiagnostic>.Empty;

	internal static void WithLocations(this ICollection<AdhocDiagnostic> diagnostics, ImmutableArray<Location> locations)
	{
		if (diagnostics.Count != locations.Length)
		{
			throw new ArgumentException($"Count of diagnostics does not match the Length of locations.", nameof(locations));
		}

		int i = 0;
		foreach (AdhocDiagnostic diagnostic in diagnostics)
		{
			Location location = locations[i];
			diagnostic.WithLocation(location);

			i++;
		}
	}
}
