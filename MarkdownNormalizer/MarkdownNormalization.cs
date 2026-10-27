using System.Text.RegularExpressions;

public static class MarkdownNormalization
{
	public static string NormalizeMarkdown(
		string content,
		string fileName,
		bool addTitle,
		bool removeH1)
	{
		string newline = DetectNewline(content);

		// Reject mixed line endings rather than rewrite them.
		string withoutCrLf = content.Replace("\r\n", "");
		if (withoutCrLf.Contains('\r') && withoutCrLf.Contains('\n'))
			throw new InvalidDataException("Mixed line endings.");

		// Identify frontmatter at the beginning.
		int bodyStart = 0;
		bool hasFrontmatter = false;

		if (content.StartsWith("---" + newline, StringComparison.Ordinal))
		{
			int pos = content.IndexOf(newline, StringComparison.Ordinal)
					  + newline.Length;

			bool closed = false;

			while (pos < content.Length)
			{
				int end = content.IndexOf(newline, pos,
					StringComparison.Ordinal);

				if (end < 0)
					end = content.Length;

				string line = content[pos..end];

				if (line is "---" or "...")
				{
					bodyStart = end < content.Length
						? end + newline.Length : end;
					closed = true;
					break;
				}

				pos = end < content.Length
					? end + newline.Length : content.Length;
			}

			if (!closed)
				throw new InvalidDataException("Unclosed YAML frontmatter.");

			hasFrontmatter = true;
		}
		else if (content == "---")
		{
			throw new InvalidDataException("Unclosed YAML frontmatter.");
		}

		if (hasFrontmatter)
		{
			string yaml = content[..bodyStart];
			ValidateFrontmatter(yaml, newline);
		}

		// Add missing YAML title.
		if (addTitle)
		{
			string yaml = hasFrontmatter ? content[..bodyStart] : "";

			bool hasTitle = Regex.IsMatch(
				yaml, @"(?im)^title\s*:",
				RegexOptions.CultureInvariant);

			if (!hasTitle)
			{
				// JSON quoted strings are valid YAML double-quoted scalars.
				string title = "title: " +
					System.Text.Json.JsonSerializer.Serialize(
						fileName,
						new System.Text.Json.JsonSerializerOptions
						{
							Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
						});

				if (hasFrontmatter)
				{
					int insertAt = content.IndexOf(newline,
						StringComparison.Ordinal) + newline.Length;

					content = content.Insert(insertAt, title + newline);
					bodyStart += title.Length + newline.Length;
				}
				else
				{
					string header = "---" + newline + title + newline +
									"---" + newline + newline;
					content = header + content;
					bodyStart = header.Length;
					hasFrontmatter = true;
				}
			}
		}

		// Remove only an initial matching ATX H1.
		if (removeH1)
		{
			string prefix = content[..bodyStart];
			string body = content[bodyStart..];

			// Allow leading blank lines, but no other content.
			var match = Regex.Match(
				body,
				@"\A(?<leading>(?:[ \t]*\r?\n)*)(?<heading>#[ \t]+[^\r\n]*)(?<eol>\r\n|\n|\r|$)");

			if (match.Success)
			{
				string heading = match.Groups["heading"].Value;
				string headingText = heading[1..].Trim();

				if (string.Equals(headingText, fileName,
						StringComparison.Ordinal))
				{
					string remainder = body[match.Length..];

					// Remove surplus leading blank lines.
					remainder = remainder.TrimStart('\r', '\n');

					body = hasFrontmatter
						? newline + remainder
						: remainder;

					content = prefix + body;
				}
			}
		}

		return content;
	}

	static void ValidateFrontmatter(string yaml, string newline)
	{
		string[] lines = yaml.Split(
			new[] { newline }, StringSplitOptions.None);

		var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		// Conservative validation, not a complete YAML parser.
		// Skip advanced YAML constructs we cannot safely inspect.
		foreach (string line in lines.Skip(1))
		{
			if (line is "---" or "..." or "")
				continue;

			if (line.StartsWith(' ') || line.StartsWith('\t'))
				continue;

			if (line.StartsWith('#'))
				continue;

			// Allow unindented YAML list items used by Obsidian.
			// Examples:
			// aliases:
			// - First Alias
			// - Second Alias
			if (line.StartsWith("- "))
				continue;

			var match = Regex.Match(line,
				@"^([A-Za-z_][A-Za-z0-9_-]*)\s*:");

			if (!match.Success)
				throw new InvalidDataException(
					"Unsupported YAML property syntax.");

			if (!keys.Add(match.Groups[1].Value))
				throw new InvalidDataException(
					"Duplicate YAML property.");
		}
	}

	static string DetectNewline(string content)
	{
		int index = content.IndexOfAny(['\r', '\n']);

		if (index < 0)
			return Environment.NewLine;

		if (content[index] == '\r' &&
			index + 1 < content.Length &&
			content[index + 1] == '\n')
			return "\r\n";

		return content[index] == '\r' ? "\r" : "\n";
	}

}