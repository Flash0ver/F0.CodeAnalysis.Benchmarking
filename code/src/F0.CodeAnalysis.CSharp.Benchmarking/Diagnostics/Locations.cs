using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Text;
using Microsoft.CodeAnalysis;

namespace F0.CodeAnalysis.CSharp.Diagnostics;

internal sealed class Locations : Collection<(string Path, ImmutableArray<Location> Location)>
{
}
