# MarkdownNormalizer

A lightweight .NET console utility for standardizing Markdown document titles and YAML frontmatter without unnecessarily modifying the original content.

## Why I Built This

Over time, I accumulated a growing collection of technical notes, reference materials, and documentation written in Markdown and organized in Obsidian.

Markdown worked well for maintaining these documents, but I encountered a small problem when preparing them for use outside Obsidian.

Many documents contained two representations of the same title:

- The filename, which already identified the document.
- A first-level Markdown heading (`# Title`) repeating that filename.

At the same time, the YAML frontmatter often lacked an explicit `title` property.

This wasn't a problem while reading notes in Obsidian, but it made the documents less consistent when preparing them for conversion into formats such as PDF, Word, or EPUB using tools like Pandoc.

### Why YAML Titles Matter for Pandoc

Pandoc can use the `title` property in YAML frontmatter as document metadata when generating PDFs, Word documents, and other output formats. This allows the document title to be handled independently of the Markdown headings, making it easier to generate properly titled documents without relying on a redundant H1 heading. By automatically adding missing titles, MarkdownNormalizer helps prepare existing Obsidian notes for cleaner, more consistent document conversion.

I wanted to standardize the documents without manually editing hundreds of files or introducing unnecessary changes to their contents.

**MarkdownNormalizer was built to solve that specific problem.**

Rather than building a general-purpose Markdown formatter, I focused on two narrowly defined operations: adding missing YAML titles and removing redundant first-level headings.

The utility is intentionally conservative. It analyzes each document, makes only the requested changes, and avoids modifying files when their structure cannot be processed safely.

## Features

- Adds a missing YAML `title` property using the Markdown filename (without the `.md` extension).
- Preserves existing YAML titles, even when they differ from the filename.
- Removes an initial H1 heading when it matches the filename.
- Preserves H1 headings that do not match the filename.
- Preserves other Markdown content and existing YAML properties.
- Processes Markdown files in a specified directory.
- Supports optional recursive directory processing.
- Supports independent title insertion and H1 removal operations.
- Uses dry-run mode by default.
- Requires explicit `--apply` to modify files.
- Detects unclosed or unsupported frontmatter structures and reports errors.
- Attempts to preserve supported file encodings and line endings.
- Uses temporary files and replacement operations to reduce the risk of incomplete writes.
- Supports idempotent processing: already-normalized documents do not require further changes.

## Requirements

- .NET 10 SDK to build and run from source.
- Windows for the current file-replacement implementation.

## Usage

Run the utility from the project directory:

```powershell
dotnet run -- "C:\Documents\Markdown"
```

By default, the program runs in **dry-run mode**. It reports which documents would be changed without modifying them.

To apply the changes:

```powershell
dotnet run -- "C:\Documents\Markdown" --apply
```

### Command-Line Options

| Option | Description |
|---|---|
| `--recursive` | Include Markdown files in subdirectories. |
| `--title-only` | Add missing YAML titles without removing H1 headings. |
| `--remove-h1-only` | Remove matching H1 headings without adding YAML titles. |
| `--dry-run` | Preview changes without writing files. |
| `--apply` | Apply changes to files. |

When neither `--title-only` nor `--remove-h1-only` is specified, both normalization operations are enabled.

### Examples

Preview changes throughout a directory tree:

```powershell
dotnet run -- "C:\Documents\Markdown" --recursive
```

Add missing YAML titles only:

```powershell
dotnet run -- "C:\Documents\Markdown" --title-only --apply
```

Remove redundant H1 headings only:

```powershell
dotnet run -- "C:\Documents\Markdown" --remove-h1-only --apply
```

Apply both operations recursively:

```powershell
dotnet run -- "C:\Documents\Markdown" --recursive --apply
```

## How Normalization Works

Consider a Markdown file named:

`Database Performance Notes.md`

### Before

```markdown
---
created: 2026-10-27
tags:
  - database
  - performance
---

# Database Performance Notes

## Introduction

These notes cover database query optimization.
```

### After

```markdown
---
title: "Database Performance Notes"
created: 2026-10-27
tags:
  - database
  - performance
---

## Introduction

These notes cover database query optimization.
```

The utility adds the missing YAML title and removes the redundant H1 heading.

Other document content remains unchanged.

If the original document already contains a YAML `title`, that value is preserved. If the H1 heading does not match the filename, it is also preserved.

## Safety and File Handling

MarkdownNormalizer was designed with conservative file modification in mind.

**Dry-run by default:** The utility does not modify files unless `--apply` is explicitly specified.

**Minimal changes:** Only the requested normalization operations are performed. The utility does not reformat the entire document.

**Existing metadata preservation:** Existing YAML titles are not overwritten, and unrelated metadata is retained.

**Error isolation:** A file that cannot be processed is reported as an error without intentionally stopping processing of the remaining files.

**Idempotency:** The normalization result is checked to ensure that processing it again produces the same content.

**Safer writes:** Updated content is written to a temporary file before replacing the original. This reduces the risk of leaving a partially written document, although it does not eliminate every possible file-system failure.

### Recommended Precautions

Always create a backup or use version control before processing important documents, especially when running the utility against an entire documentation vault.

Preview the changes using dry-run mode before applying them.

## Limitations

MarkdownNormalizer is a focused document maintenance utility, not a complete Markdown or YAML parser.

- YAML validation is intentionally lightweight and does not support every valid YAML structure.
- Complex or unsupported frontmatter may be rejected rather than modified.
- H1 removal is limited to the initial heading that matches the document filename.
- It does not rewrite links, reorganize documents, or perform general Markdown formatting.
- It does not guarantee support for every text encoding.
- The current file-replacement implementation is intended for Windows.

These limitations are intentional. The goal is predictable behavior for a specific normalization task rather than broad document transformation.

## Design Approach

The implementation uses a straightforward .NET console application with focused methods for command-line processing, file discovery, decoding, normalization, validation, and file writing.

The project deliberately avoids unnecessary frameworks, external services, and architectural complexity.

The central design principle is simple:

**A document maintenance tool should change only what it understands and leave everything else alone.**

## Project Status

MarkdownNormalizer v1.0 is functionally complete and has been validated against a real-world Obsidian documentation vault containing 640 Markdown files.

### Automated Testing

MarkdownNormalizer includes a separate **xUnit test project with 16 passing tests** covering:

- YAML title insertion and preservation of existing metadata.
- Matching and nonmatching H1 headings.
- Malformed YAML frontmatter detection and support for common Obsidian YAML lists.
- Idempotency and Windows line-ending preservation.
- Filename punctuation and special-character handling.
- UTF-8 and UTF-16 encoding preservation.
- Safe filesystem replacement, external modification detection, and temporary-file cleanup.

Run the tests from the solution directory:

```powershell
dotnet test
```

See the [Unit Test Documentation](MarkdownNormalizer.Tests/README.md) for individual test descriptions and expected behavior.

## License

MarkdownNormalizer is released under the **MIT License**.

You are free to use, modify, and distribute this software under the terms of the license.

See the [LICENSE](LICENSE) file for the full license text.