# MarkdownNormalizer — Unit Tests

This project contains automated unit tests for the core Markdown normalization functionality.

The tests use **xUnit** and run independently of the console application's command-line interface.

The test suite includes in-memory unit tests for Markdown normalization and encoding, along with filesystem integration tests that operate on disposable files in temporary directories. No documents in the user's actual Markdown collection are modified.

## Purpose

MarkdownNormalizer modifies Markdown documents by adding missing YAML titles and removing redundant first-level headings.

Because the utility may be used against entire directories of personal or business documentation, it is important to verify that these changes are predictable and that unrelated content is preserved.

The unit tests serve two purposes:

1. **Behavior verification:** Confirm that the normalizer performs the intended transformations.
2. **Regression protection:** Detect unintended changes to existing functionality when the implementation is modified.

The normalization tests call `MarkdownNormalization.NormalizeMarkdown()` directly using in-memory Markdown strings. No actual Markdown files are modified during these tests.

## Test Cases

### 1. NormalizeMarkdown_AddsMissingYamlTitle

**Purpose:** Verify that a missing YAML `title` property is added using the filename.

**Scenario:** A document contains YAML frontmatter with other metadata but no title.

**Expected behavior:**
- Add a `title` property matching the filename.
- Preserve existing metadata.
- Preserve the document's headings and content.

**Why it matters:** Provides consistent document metadata for applications such as Pandoc, which can use the title when generating PDFs and other document formats.

### 2. NormalizeMarkdown_PreservesExistingYamlTitle

**Purpose:** Verify that an existing YAML title is never overwritten.

**Scenario:** A document already contains a title different from its filename.

**Expected behavior:**
- Preserve the existing title.
- Leave the entire document unchanged when no other normalization is requested.

**Why it matters:** Prevents the utility from replacing deliberately customized document metadata.

### 3. NormalizeMarkdown_RemovesMatchingH1

**Purpose:** Verify that a redundant first-level heading is removed when it matches the filename.

**Scenario:** A document contains an H1 heading identical to its filename.

**Expected behavior:**
- Remove the matching H1.
- Preserve the YAML title.
- Preserve other headings and document content.

**Why it matters:** Avoids duplicate document titles when Markdown files are displayed or converted into other formats.

### 4. NormalizeMarkdown_PreservesNonMatchingH1

**Purpose:** Verify that legitimate first-level headings are not removed.

**Scenario:** The document's H1 differs from its filename.

**Expected behavior:**
- Preserve the H1.
- Leave the document unchanged.

**Why it matters:** Protects meaningful document structure and prevents accidental deletion of headings.

### 5. NormalizeMarkdown_ThrowsOnUnclosedYamlFrontmatter

**Purpose:** Verify that malformed frontmatter is detected.

**Scenario:** A document begins with a YAML opening delimiter (`---`) but does not contain a closing delimiter.

**Expected behavior:**
- Throw an `InvalidDataException`.
- Return the message `Unclosed YAML frontmatter.`
- Do not return partially normalized content.

**Why it matters:** Ensures the normalization logic rejects an unsupported document structure instead of making potentially incorrect changes.

The console application is responsible for catching this exception, reporting the affected file, and continuing with other documents.

### 6. NormalizeMarkdown_IsIdempotent

**Purpose:** Verify that repeated normalization does not produce additional changes.

**Scenario:** A document requires both YAML title insertion and matching H1 removal.

**Expected behavior:**
- The first normalization modifies the document.
- The second normalization produces exactly the same result as the first.

**Why it matters:** Allows the utility to be run repeatedly against the same documentation collection without introducing unnecessary changes.

### 7. NormalizeMarkdown_PreservesWindowsLineEndings

**Purpose:** Verify that normalization preserves Windows-style CRLF (`\r\n`) line endings.

**Expected behavior:**
- Add the missing YAML title.
- Remove the matching H1 heading.
- Preserve CRLF line endings throughout the resulting document.

**Why it matters:** Prevents unnecessary changes to every line of a document, particularly when files are tracked using Git.

### 8. NormalizeMarkdown_PreservesFilenamePunctuation

**Purpose:** Verify that punctuation and capitalization are preserved when generating YAML titles.

**Expected behavior:**
- Preserve characters such as `#`, `&`, periods, parentheses, and hyphens.
- Generate a readable YAML title without unnecessarily escaping ordinary punctuation.
- Preserve existing document content.

**Why it matters:** Technical documentation frequently contains punctuation in titles. Generated metadata should remain readable and accurately represent the original title.

### 9. NormalizeMarkdown_EscapesSpecialCharactersInTitle

**Purpose:** Verify that special characters requiring escaping are handled correctly.

**Expected behavior:**
- Escape embedded quotation marks.
- Escape backslashes.
- Produce a properly quoted YAML title.
- Preserve unrelated document content.

**Why it matters:** Ensures generated metadata remains syntactically valid even when titles contain characters requiring special handling.

### 10. EncodeDecode_PreservesUtf8Bom

**Purpose:** Verify that a UTF-8 document containing a Byte Order Mark (BOM) retains its original encoding and BOM.

**Expected behavior:**
- Correctly detect and decode UTF-8 with BOM.
- Re-encode the document without losing or duplicating the BOM.
- Produce a byte sequence identical to the original.

**Why it matters:** Prevents unintended encoding changes when processing existing Markdown documents.

### 11. EncodeDecode_PreservesUtf8WithoutBom

**Purpose:** Verify that UTF-8 documents without a BOM remain BOM-free.

**Expected behavior:**
- Decode the document using UTF-8.
- Re-encode without introducing a BOM.
- Preserve the original byte sequence exactly.

**Why it matters:** Avoids unnecessary file changes and maintains compatibility with tools expecting UTF-8 without BOM.

### 12. EncodeDecode_PreservesUtf16LittleEndianBom

**Purpose:** Verify preservation of UTF-16 Little Endian encoding and its BOM.

**Expected behavior:**
- Detect the UTF-16 Little Endian BOM (`FF FE`).
- Decode the document correctly.
- Re-encode using the original byte order.
- Produce bytes identical to the original.

**Why it matters:** Ensures documents using UTF-16 Little Endian are not unintentionally converted to another encoding.

### 13. EncodeDecode_PreservesUtf16BigEndianBom

**Purpose:** Verify preservation of UTF-16 Big Endian encoding and its BOM.

**Expected behavior:**
- Detect the UTF-16 Big Endian BOM (`FE FF`).
- Decode the document correctly.
- Re-encode using the original byte order.
- Produce bytes identical to the original.

**Why it matters:** Protects documents using UTF-16 Big Endian from byte-order or encoding changes.

### 14. WriteFileSafely_ReplacesOriginalFile

**Purpose:** Verify that the file-writing operation successfully replaces an existing Markdown file.

**Scenario:** A temporary test directory contains a Markdown file that needs to be updated.

**Expected behavior:**
- Write the updated content to a temporary file.
- Replace the original file with the updated content.
- Verify that the resulting bytes match the expected output.
- Ensure no temporary `.tmp` files remain after successful replacement.

**Why it matters:** Confirms that the application's file-replacement mechanism works correctly without leaving unnecessary temporary files behind.

### 15. WriteFileSafely_DoesNotOverwriteModifiedFile

**Purpose:** Verify that MarkdownNormalizer detects external modifications before replacing a file.

**Scenario:** Another application changes a Markdown file after its original contents have been read but before MarkdownNormalizer attempts to write its updates.

**Expected behavior:**
- Detect that the file's contents no longer match the original bytes.
- Throw an `IOException` with the message `File changed during processing. Skipped.`
- Preserve the externally modified file.
- Remove the temporary file created during the attempted replacement.

**Why it matters:** Reduces the risk of accidentally overwriting newer changes made by another application or user.

This protection uses SHA-256 to compare the current file contents with the originally read contents. It does not guarantee protection against modifications occurring between the final comparison and the replacement operation.

### 16. NormalizeMarkdown_AcceptsYamlLists

**Purpose:** Verify that MarkdownNormalizer accepts YAML frontmatter containing unindented list entries commonly used by Obsidian.

**Scenario:** A document contains `aliases` and `tags` properties represented as YAML lists.

**Expected behavior:**
- Recognize list entries beginning with `- ` without reporting unsupported YAML property syntax.
- Add the missing YAML title.
- Preserve existing aliases, tags, and other metadata.
- Preserve the document's existing list entries.

**Why it matters:** Ensures compatibility with real-world Obsidian documents while retaining conservative frontmatter validation.

## Filesystem Test Summary

| Test | Result |
|---|---|
| Successful file replacement | Passed |
| External modification detection | Passed |
| Temporary-file cleanup after successful replacement | Passed |
| Temporary-file cleanup after detected modification | Passed |

These tests use disposable files in uniquely named temporary directories. They do not modify any documents in the user's actual Markdown collection.

## Encoding Test Summary

| Encoding | BOM | Result |
|---|---|---|
| UTF-8 | Present | Passed |
| UTF-8 | Absent | Passed |
| UTF-16 Little Endian | Present | Passed |
| UTF-16 Big Endian | Present | Passed |

These tests verify **byte-for-byte round-trip preservation** using the `MarkdownFileEncoding` class.


## Running the Tests

### Visual Studio

1. Open the MarkdownNormalizer solution.
2. Select **Test → Test Explorer**.
3. Click **Run All Tests**.
4. Review the results.

### .NET CLI

From the solution root:

```powershell
dotnet test
```

Or run the test project directly:

```powershell
dotnet test MarkdownNormalizer.Tests/MarkdownNormalizer.Tests.csproj
```

## Test Coverage and Limitations

The current test suite covers the primary in-memory normalization behaviors, including YAML title handling, H1 removal, malformed frontmatter detection, idempotency, Windows CRLF preservation, and special-character handling.

Additional tests verify byte-for-byte encoding round trips for UTF-8 (with and without BOM) and UTF-16 Little Endian and Big Endian (with BOM).

The following areas are not yet fully covered:

- Filesystem failures such as access-denied errors and interrupted writes.
- The small race window between file verification and replacement.
- Command-line argument parsing and directory discovery.
- Unsupported encodings and invalid byte sequences.
- Additional line-ending scenarios, including Unix LF and mixed line endings.
- Complex YAML structures and other frontmatter edge cases.
- End-to-end processing of multiple Markdown files through the console application.

These areas may be covered by additional unit or integration tests as the project evolves.

## Testing Philosophy

The goal is not simply to achieve a high number of passing tests. Each test should protect a meaningful behavior or requirement.

For a document-processing utility, preserving content that should not change is just as important as performing the requested transformation.

**The guiding principle is to verify both what the application changes and what it deliberately leaves untouched.**