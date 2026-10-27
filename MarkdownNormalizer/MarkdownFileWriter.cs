using System.Security.Cryptography;

public static class MarkdownFileWriter
{
	public static void WriteFileSafely(
		string path,
		byte[] originalBytes,
		byte[] updatedBytes)
	{
		string directory = Path.GetDirectoryName(path)!;
		string tempPath = Path.Combine(
			directory, "." + Guid.NewGuid().ToString("N") + ".tmp");

		try
		{
			using (var stream = new FileStream(
				tempPath, FileMode.CreateNew, FileAccess.Write,
				FileShare.None, 4096, FileOptions.WriteThrough))
			{
				stream.Write(updatedBytes);
				stream.Flush(true);
			}

			// Detect modifications made since the original read.
			byte[] currentBytes = File.ReadAllBytes(path);

			if (!CryptographicOperations.FixedTimeEquals(
					SHA256.HashData(currentBytes),
					SHA256.HashData(originalBytes)))
				throw new IOException(
					"File changed during processing. Skipped.");

			// Atomic replacement on supported filesystems.
			File.Replace(tempPath, path, null);
		}
		finally
		{
			if (File.Exists(tempPath))
				File.Delete(tempPath);
		}
	}
}