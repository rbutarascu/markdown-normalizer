using System.Text;
using Xunit;

public class MarkdownFileEncodingTests
{
	[Fact]
	public void EncodeDecode_PreservesUtf8Bom()
	{
		// Arrange
		string original = "# Database Notes\r\n\r\nSome documentation.\r\n";

		Encoding utf8WithBom = new UTF8Encoding(true, true);

		byte[] originalBytes = [
			.. utf8WithBom.GetPreamble(),
			.. utf8WithBom.GetBytes(original)
		];

		// Act
		var (decoded, detectedEncoding) =
			MarkdownFileEncoding.DecodeFile(originalBytes);

		byte[] resultBytes =
			MarkdownFileEncoding.EncodeFile(decoded, detectedEncoding);

		// Assert
		Assert.Equal(original, decoded);
		Assert.Equal(originalBytes, resultBytes);

		Assert.True(
			resultBytes.AsSpan().StartsWith(
				new byte[] { 0xEF, 0xBB, 0xBF }));
	}

	[Fact]
	public void EncodeDecode_PreservesUtf8WithoutBom()
	{
		// Arrange
		string original = "# Database Notes\r\n\r\nSome documentation.\r\n";

		Encoding utf8WithoutBom = new UTF8Encoding(false, true);
		byte[] originalBytes = utf8WithoutBom.GetBytes(original);

		// Act
		var (decoded, detectedEncoding) =
			MarkdownFileEncoding.DecodeFile(originalBytes);

		byte[] resultBytes =
			MarkdownFileEncoding.EncodeFile(decoded, detectedEncoding);

		// Assert
		Assert.Equal(original, decoded);
		Assert.Equal(originalBytes, resultBytes);

		Assert.False(
			resultBytes.AsSpan().StartsWith(
				new byte[] { 0xEF, 0xBB, 0xBF }));
	}

	[Fact]
	public void EncodeDecode_PreservesUtf16LittleEndianBom()
	{
		// Arrange
		string original = "# Database Notes\r\n\r\nSome documentation.\r\n";

		Encoding utf16LE = new UnicodeEncoding(false, true, true);

		byte[] originalBytes = [
			.. utf16LE.GetPreamble(),
		.. utf16LE.GetBytes(original)
		];

		// Act
		var (decoded, detectedEncoding) =
			MarkdownFileEncoding.DecodeFile(originalBytes);

		byte[] resultBytes =
			MarkdownFileEncoding.EncodeFile(decoded, detectedEncoding);

		// Assert
		Assert.Equal(original, decoded);
		Assert.Equal(originalBytes, resultBytes);

		Assert.True(
			resultBytes.AsSpan().StartsWith(
				new byte[] { 0xFF, 0xFE }));
	}

	[Fact]
	public void EncodeDecode_PreservesUtf16BigEndianBom()
	{
		// Arrange
		string original = "# Database Notes\r\n\r\nSome documentation.\r\n";

		Encoding utf16BE = new UnicodeEncoding(true, true, true);

		byte[] originalBytes = [
			.. utf16BE.GetPreamble(),
		.. utf16BE.GetBytes(original)
		];

		// Act
		var (decoded, detectedEncoding) =
			MarkdownFileEncoding.DecodeFile(originalBytes);

		byte[] resultBytes =
			MarkdownFileEncoding.EncodeFile(decoded, detectedEncoding);

		// Assert
		Assert.Equal(original, decoded);
		Assert.Equal(originalBytes, resultBytes);

		Assert.True(
			resultBytes.AsSpan().StartsWith(
				new byte[] { 0xFE, 0xFF }));
	}
}