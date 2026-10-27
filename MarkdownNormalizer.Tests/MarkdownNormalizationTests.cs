using Xunit;

public class MarkdownNormalizationTests
{
	[Fact]
	public void NormalizeMarkdown_AddsMissingYamlTitle()
	{
		// Arrange
		string input = """
            ---
            created: 2026-10-27
            ---

            ## Introduction

            Some documentation.
            """;

		// Act
		string result = MarkdownNormalization.NormalizeMarkdown(
			input,
			"Database Notes",
			addTitle: true,
			removeH1: false);

		// Assert
		Assert.Contains("title: \"Database Notes\"", result);
		Assert.Contains("created: 2026-10-27", result);
		Assert.Contains("## Introduction", result);
	}

	[Fact]
	public void NormalizeMarkdown_PreservesExistingYamlTitle()
	{
		// Arrange
		string input = """
        ---
        title: "My Custom Title"
        created: 2026-10-27
        ---

        ## Introduction

        Some documentation.
        """;

		// Act
		string result = MarkdownNormalization.NormalizeMarkdown(
			input,
			"Database Notes",
			addTitle: true,
			removeH1: false);

		// Assert
		Assert.Equal(input, result);
	}

	[Fact]
	public void NormalizeMarkdown_RemovesMatchingH1()
	{
		// Arrange
		string input = """
        ---
        title: "Database Notes"
        ---

        # Database Notes

        ## Introduction

        Some documentation.
        """;

		// Act
		string result = MarkdownNormalization.NormalizeMarkdown(
			input,
			"Database Notes",
			addTitle: false,
			removeH1: true);

		// Assert
		Assert.DoesNotContain("# Database Notes\n", result);
		Assert.Contains("## Introduction", result);
		Assert.Contains("title: \"Database Notes\"", result);
	}

	[Fact]
	public void NormalizeMarkdown_PreservesNonMatchingH1()
	{
		// Arrange
		string input = """
        ---
        title: "Database Notes"
        ---

        # SQL Server Performance

        ## Introduction

        Some documentation.
        """;

		// Act
		string result = MarkdownNormalization.NormalizeMarkdown(
			input,
			"Database Notes",
			addTitle: false,
			removeH1: true);

		// Assert
		Assert.Equal(input, result);
	}

	[Fact]
	public void NormalizeMarkdown_ThrowsOnUnclosedYamlFrontmatter()
	{
		// Arrange
		string input = """
        ---
        created: 2026-10-27
        tags:
          - testing

        # Database Notes

        Some documentation.
        """;

		// Act & Assert
		InvalidDataException exception =
			Assert.Throws<InvalidDataException>(() =>
				MarkdownNormalization.NormalizeMarkdown(
					input,
					"Database Notes",
					addTitle: true,
					removeH1: true));

		Assert.Equal(
			"Unclosed YAML frontmatter.",
			exception.Message);
	}

	[Fact]
	public void NormalizeMarkdown_IsIdempotent()
	{
		// Arrange
		string input = """
        ---
        created: 2026-10-27
        ---

        # Database Notes

        ## Introduction

        Some documentation.
        """;

		// Act
		string firstPass = MarkdownNormalization.NormalizeMarkdown(
			input,
			"Database Notes",
			addTitle: true,
			removeH1: true);

		string secondPass = MarkdownNormalization.NormalizeMarkdown(
			firstPass,
			"Database Notes",
			addTitle: true,
			removeH1: true);

		// Assert
		Assert.Equal(firstPass, secondPass);
		Assert.NotEqual(input, firstPass);
	}

	[Fact]
	public void NormalizeMarkdown_PreservesWindowsLineEndings()
	{
		// Arrange
		string input =
			"---\r\n" +
			"created: 2026-10-27\r\n" +
			"---\r\n" +
			"\r\n" +
			"# Database Notes\r\n" +
			"\r\n" +
			"## Introduction\r\n" +
			"\r\n" +
			"Some documentation.\r\n";

		// Act
		string result = MarkdownNormalization.NormalizeMarkdown(
			input,
			"Database Notes",
			addTitle: true,
			removeH1: true);

		// Assert
		Assert.Contains("title: \"Database Notes\"\r\n", result);
		Assert.Contains("## Introduction\r\n", result);
		Assert.DoesNotContain("# Database Notes\r\n", result);

		// Verify there are no standalone LF characters.
		Assert.DoesNotContain("\n", result.Replace("\r\n", ""));
	}

	[Fact]
	public void NormalizeMarkdown_PreservesFilenamePunctuation()
	{
		// Arrange
		string fileName = "C# & .NET - APIs: Design (2026)";

		string input = """
        ---
        created: 2026-10-27
        ---

        ## Introduction

        Some documentation.
        """;

		// Act
		string result = MarkdownNormalization.NormalizeMarkdown(
			input,
			fileName,
			addTitle: true,
			removeH1: false);

		// Assert
		Assert.Contains(
			"title: \"C# & .NET - APIs: Design (2026)\"",
			result);

		Assert.Contains("created: 2026-10-27", result);
		Assert.Contains("## Introduction", result);
	}

	[Fact]
	public void NormalizeMarkdown_EscapesSpecialCharactersInTitle()
	{
		// Arrange
		string fileName = "C# \"Advanced\" \\ Notes";

		string input = """
        ---
        created: 2026-10-27
        ---

        ## Introduction

        Some documentation.
        """;

		// Act
		string result = MarkdownNormalization.NormalizeMarkdown(
			input,
			fileName,
			addTitle: true,
			removeH1: false);

		// Assert
		Assert.Contains(
			"title: \"C# \\\"Advanced\\\" \\\\ Notes\"",
			result);

		Assert.Contains("created: 2026-10-27", result);
	}

	[Fact]
	public void NormalizeMarkdown_AcceptsYamlLists()
	{
		string input = """
        ---
        aliases:
        - Magazinul Unic
        - Unic Bucharest
        created: 2026-08-31 20:20:11
        tags:
        - romania
        - bucharest
        author: Example Author
        ---

        # Magazinul Unic
        """;

		string result = MarkdownNormalization.NormalizeMarkdown(
			input,
			"Magazinul Unic",
			addTitle: true,
			removeH1: true);

		Assert.Contains("title: \"Magazinul Unic\"", result);
		Assert.Contains("- Magazinul Unic", result);
		Assert.Contains("- Unic Bucharest", result);
		Assert.Contains("- romania", result);
		Assert.Contains("author: Example Author", result);
	}
}