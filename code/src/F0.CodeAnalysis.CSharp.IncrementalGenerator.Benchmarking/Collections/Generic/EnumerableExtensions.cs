namespace F0.CodeAnalysis.CSharp.Collections.Generic;

internal static class EnumerableExtensions
{
	internal static IEnumerable<T> ToEnumerable<T>(this T? element)
		where T : class
	{
		if (element is not null)
		{
			yield return element;
		}
	}

	internal static IEnumerable<T> ToEnumerable<T>(this T? element)
		where T : struct
	{
		if (element.HasValue)
		{
			yield return element.Value;
		}
	}
}
