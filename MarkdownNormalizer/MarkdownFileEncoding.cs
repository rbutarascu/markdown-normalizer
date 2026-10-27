using System.Text;

public static class MarkdownFileEncoding
{
	public static (string Content, Encoding Encoding) DecodeFile(byte[] bytes)
	{
		Encoding encoding;

		if (bytes.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }))
			encoding = new UTF8Encoding(true, true);
		else if (bytes.AsSpan().StartsWith(new byte[] { 0xFF, 0xFE }))
			encoding = new UnicodeEncoding(false, true, true);
		else if (bytes.AsSpan().StartsWith(new byte[] { 0xFE, 0xFF }))
			encoding = new UnicodeEncoding(true, true, true);
		else
			encoding = new UTF8Encoding(false, true);

		int preambleLength = encoding.GetPreamble().Length;

		// For files without BOM, assume strict UTF-8.
		return (
			encoding.GetString(
				bytes,
				preambleLength,
				bytes.Length - preambleLength),
			encoding);
	}

	public static byte[] EncodeFile(string content, Encoding encoding)
	{
		byte[] preamble = encoding.GetPreamble();
		byte[] body = encoding.GetBytes(content);

		byte[] result = new byte[preamble.Length + body.Length];

		preamble.CopyTo(result, 0);
		body.CopyTo(result, preamble.Length);

		return result;
	}
}