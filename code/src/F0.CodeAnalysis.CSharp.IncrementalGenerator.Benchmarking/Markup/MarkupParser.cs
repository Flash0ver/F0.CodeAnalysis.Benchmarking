using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace F0.CodeAnalysis.CSharp.Markup;

internal static class MarkupParser
{
	private const char OpenBraceToken = '{';
	private const char CloseBraceToken = '}';
	private const char BarToken = '|';
	private const char HashToken = '#';
	private const char ColonToken = ':';
	private const char CarriageReturn = '\r';
	private const char LineFeed = '\n';

	internal static string Parse(string filePath, string markup, out ImmutableArray<Location> locations)
	{
		StringBuilder sanitized = new(markup.Length);
		List<Location> outLocations = new();

		List<int> keys = new();
		Dictionary<int, (int TextSpanStart, LinePosition LinePositionStart)> spans = new();

		int characters = 0;
		int lineNumber = 0;
		int column = 0;
		for (int i = 0; i < markup.Length; i++)
		{
			char character = markup[i];

			if (character is CarriageReturn or LineFeed)
			{
				if (character == CarriageReturn && i + 1 < markup.Length)
				{
					char peek = markup[i + 1];
					if (peek == LineFeed)
					{
						i++;

						characters++;
					}
				}

				characters++;
				lineNumber++;
				column = 0;
				_ = sanitized.AppendLine();
				continue;
			}

			if (markup.Length - i >= 3
				&& character.Equals(OpenBraceToken))
			{
				char character1 = markup[i + 1];
				if (character1.Equals(BarToken))
				{
					char character2 = markup[i + 2];
					if (character2.Equals(HashToken))
					{
						int indexOfColon = markup.IndexOf(ColonToken, i + 3);
						if (indexOfColon != -1)
						{
							string number = markup.Substring(i + 3, indexOfColon - i - 3);
							if (Int32.TryParse(number, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out int locationKey))
							{
								if (locationKey < 0)
								{
									throw new InvalidOperationException($"Invalid Markup Location: {locationKey.ToString(NumberFormatInfo.InvariantInfo)}");
								}

								keys.Add(locationKey);
								spans.Add(locationKey, (characters, new LinePosition(lineNumber, column)));

								i += 3 + number.Length;
								continue;
							}
							else
							{
								throw new InvalidOperationException($"Invalid Markup Location: {number}");
							}
						}
					}
				}
			}

			if (markup.Length - i >= 2
				&& character.Equals(BarToken))
			{
				char character1 = markup[i + 1];
				if (character1.Equals(CloseBraceToken))
				{
					int previousKey = keys[keys.Count - 1];
					keys.RemoveAt(keys.Count - 1);

					(int TextSpanStart, LinePosition LinePositionStart) = spans[previousKey];
					_ = spans.Remove(previousKey);

					var textSpan = TextSpan.FromBounds(TextSpanStart, characters);

					var end = new LinePosition(lineNumber, column);
					var lineSpan = new LinePositionSpan(LinePositionStart, end);

					var location = Location.Create(filePath, textSpan, lineSpan);
					outLocations.Add(location);

					i += 1;
					continue;
				}
				else if (character1.Equals(HashToken))
				{
					int indexOfColon = markup.IndexOf(CloseBraceToken, i + 2);
					if (indexOfColon != -1)
					{
						string number = markup.Substring(i + 2, indexOfColon - i - 2);
						if (Int32.TryParse(number, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out int locationKey))
						{
							if (locationKey < 0)
							{
								throw new InvalidOperationException($"Invalid Markup Location: {locationKey.ToString(NumberFormatInfo.InvariantInfo)}");
							}

							(int TextSpanStart, LinePosition LinePositionStart) = spans[locationKey];
							_ = spans.Remove(locationKey);
							_ = keys.Remove(locationKey);

							var textSpan = TextSpan.FromBounds(TextSpanStart, characters);

							var end = new LinePosition(lineNumber, column);
							var lineSpan = new LinePositionSpan(LinePositionStart, end);

							var location = Location.Create(filePath, textSpan, lineSpan);
							outLocations.Add(location);

							i += 2 + number.Length;
							continue;
						}
						else
						{
							throw new InvalidOperationException($"Invalid Markup Location: {number}");
						}
					}
				}
			}

			characters++;
			column++;
			_ = sanitized.Append(character);
		}

		if (keys.Count != 0
			|| spans.Count != 0)
		{
			throw new InvalidOperationException($"Invalid Markup Syntax");
		}

		Debug.Assert(characters == sanitized.Length, $"Unexpected number of characters {sanitized.Length}.");

		locations = outLocations.ToImmutableArray();
		return sanitized.ToString();
	}
}
