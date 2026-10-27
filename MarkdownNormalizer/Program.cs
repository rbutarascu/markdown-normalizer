
using System.Text;

// Main execution
if (!TryParseArguments(args, out var directory,
		out var recursive, out var apply,
		out var titleOnly, out var removeH1Only))
	return;

Console.WriteLine("MarkdownNormalizer");
Console.WriteLine("------------------");
Console.WriteLine($"Directory:      {directory}");
Console.WriteLine($"Recursive:      {recursive}");
Console.WriteLine($"Mode:           {(apply ? "APPLY" : "DRY RUN")}");
Console.WriteLine($"Add YAML title: {!removeH1Only}");
Console.WriteLine($"Remove H1:      {!titleOnly}");
Console.WriteLine();

try
{
	var files = FindMarkdownFiles(directory!, recursive);
	ProcessFiles(files, directory!, apply,
		!removeH1Only, !titleOnly);
}
catch (Exception ex) when (
	ex is IOException or UnauthorizedAccessException)
{
	Console.Error.WriteLine(ex.Message);
	Environment.ExitCode = 1;
}

// ------------------------------------------------------------
// Command-line arguments
// ------------------------------------------------------------

static bool TryParseArguments(
	string[] args,
	out string? directory,
	out bool recursive,
	out bool apply,
	out bool titleOnly,
	out bool removeH1Only)
{
	directory = null;
	recursive = false;
	apply = false;
	titleOnly = false;
	removeH1Only = false;

	if (args.Length == 0 ||
		args.Contains("--help", StringComparer.OrdinalIgnoreCase))
	{
		ShowUsage();
		return false;
	}

	foreach (string arg in args)
	{
		switch (arg.ToLowerInvariant())
		{
			case "--recursive":
				recursive = true;
				break;
			case "--apply":
				apply = true;
				break;
			case "--dry-run":
				apply = false;
				break;
			case "--title-only":
				titleOnly = true;
				break;
			case "--remove-h1-only":
				removeH1Only = true;
				break;
			default:
				if (arg.StartsWith("--") || directory != null)
				{
					Console.Error.WriteLine($"Invalid argument: {arg}");
					Environment.ExitCode = 1;
					return false;
				}
				directory = arg;
				break;
		}
	}

	if (titleOnly && removeH1Only)
	{
		Console.Error.WriteLine("Conflicting operation flags.");
		Environment.ExitCode = 1;
		return false;
	}

	if (string.IsNullOrWhiteSpace(directory))
	{
		Console.Error.WriteLine("Directory required.");
		Environment.ExitCode = 1;
		return false;
	}

	directory = Path.GetFullPath(directory);

	if (!Directory.Exists(directory))
	{
		Console.Error.WriteLine($"Directory not found: {directory}");
		Environment.ExitCode = 1;
		return false;
	}

	return true;
}

// ------------------------------------------------------------
// File discovery and processing
// ------------------------------------------------------------

static List<string> FindMarkdownFiles(string directory, bool recursive)
{
	return Directory.EnumerateFiles(
			directory, "*",
			recursive ? SearchOption.AllDirectories
					  : SearchOption.TopDirectoryOnly)
		.Where(f => Path.GetExtension(f).Equals(
			".md", StringComparison.OrdinalIgnoreCase))
		.OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
		.ToList();
}

static void ProcessFiles(
	List<string> files,
	string directory,
	bool apply,
	bool addTitle,
	bool removeH1)
{
	int changed = 0, unchanged = 0, errors = 0;

	foreach (string file in files)
	{
		string relative = Path.GetRelativePath(directory, file);

		try
		{
			byte[] originalBytes = File.ReadAllBytes(file);
			var (original, encoding) = MarkdownFileEncoding.DecodeFile(originalBytes);

			string fileName = Path.GetFileNameWithoutExtension(file);
			string normalized = MarkdownNormalization.NormalizeMarkdown(
				original, fileName, addTitle, removeH1);

			// Idempotency check
			if (normalized != MarkdownNormalization.NormalizeMarkdown(
					normalized, fileName, addTitle, removeH1))
				throw new InvalidDataException(
					"Normalization is not idempotent.");

			if (original == normalized)
			{
				unchanged++;
				Console.WriteLine($"[UNCHANGED] {relative}");
				continue;
			}

			if (apply)
			{
				byte[] updatedBytes = MarkdownFileEncoding.EncodeFile(normalized, encoding);
				MarkdownFileWriter.WriteFileSafely(file, originalBytes, updatedBytes);
			}

			changed++;
			Console.WriteLine(
				$"[{(apply ? "UPDATED" : "CHANGE")}] {relative}");
		}
		catch (Exception ex) when (
			ex is IOException or InvalidDataException
			or UnauthorizedAccessException
			or DecoderFallbackException or EncoderFallbackException
			or ArgumentException or NotSupportedException)
		{
			errors++;
			Console.Error.WriteLine(
				$"[ERROR] {relative}: {ex.Message}");
		}
	}

	Console.WriteLine();
	Console.WriteLine("Summary");
	Console.WriteLine("------------------");
	Console.WriteLine($"Files scanned: {files.Count}");
	Console.WriteLine($"{(apply ? "Updated" : "Would change")}: {changed}");
	Console.WriteLine($"Unchanged:     {unchanged}");
	Console.WriteLine($"Errors:        {errors}");

	if (errors > 0)
		Environment.ExitCode = 1;

	if (!apply)
		Console.WriteLine("No files were modified.");
}


static void ShowUsage()
{
	Console.WriteLine("""
        MarkdownNormalizer

        Usage:
          MarkdownNormalizer <directory> [options]

        Options:
          --recursive        Include subdirectories
          --dry-run          Preview changes (default)
          --apply            Save changes to disk
          --title-only       Only add missing YAML titles
          --remove-h1-only   Only remove matching H1 headings
          --help             Show help
        """);
}
