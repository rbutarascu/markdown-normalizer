using System.Text;
using Xunit;

public class MarkdownFileWriterTests
{
	[Fact]
	public void WriteFileSafely_ReplacesOriginalFile()
	{
		// Arrange
		string directory = Path.Combine(
			Path.GetTempPath(),
			"MarkdownNormalizerTests",
			Guid.NewGuid().ToString("N"));

		Directory.CreateDirectory(directory);

		try
		{
			string filePath = Path.Combine(directory, "Test.md");

			byte[] originalBytes = Encoding.UTF8.GetBytes(
				"# Original Content");

			byte[] updatedBytes = Encoding.UTF8.GetBytes(
				"# Updated Content");

			File.WriteAllBytes(filePath, originalBytes);

			// Act
			MarkdownFileWriter.WriteFileSafely(
				filePath, originalBytes, updatedBytes);

			// Assert
			Assert.Equal(updatedBytes, File.ReadAllBytes(filePath));

			// Verify that the temporary file was removed.
			Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
		}
		finally
		{
			// Always clean up our disposable test directory.
			Directory.Delete(directory, recursive: true);
		}
	}

	[Fact]
	public void WriteFileSafely_DoesNotOverwriteModifiedFile()
	{
		// Arrange
		string directory = Path.Combine(
			Path.GetTempPath(),
			"MarkdownNormalizerTests",
			Guid.NewGuid().ToString("N"));

		Directory.CreateDirectory(directory);

		try
		{
			string filePath = Path.Combine(directory, "Test.md");

			byte[] originalBytes = Encoding.UTF8.GetBytes(
				"# Original Content");

			byte[] externallyModifiedBytes = Encoding.UTF8.GetBytes(
				"# Modified By Another Application");

			byte[] updatedBytes = Encoding.UTF8.GetBytes(
				"# MarkdownNormalizer Changes");

			File.WriteAllBytes(filePath, originalBytes);

			// Simulate another application modifying the file
			// after MarkdownNormalizer originally read it.
			File.WriteAllBytes(filePath, externallyModifiedBytes);

			// Act & Assert
			IOException exception = Assert.Throws<IOException>(() =>
				MarkdownFileWriter.WriteFileSafely(
					filePath, originalBytes, updatedBytes));

			Assert.Equal(
				"File changed during processing. Skipped.",
				exception.Message);

			// Verify the external modification was preserved.
			Assert.Equal(
				externallyModifiedBytes,
				File.ReadAllBytes(filePath));

			// Verify the temporary file was cleaned up.
			Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}
}