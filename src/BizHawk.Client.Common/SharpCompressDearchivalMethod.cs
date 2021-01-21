#nullable enable

using System;
using System.Collections.Generic;
using System.IO;

using BizHawk.Common;
using BizHawk.Common.BufferExtensions;

using SharpCompress.Archives;
using SharpCompress.Common;

namespace BizHawk.Client.Common
{
	/// <summary>A <see cref="IFileDearchivalMethod{T}">dearchival method</see> for <see cref="HawkFile"/> implemented using <c>SharpCompress</c> from NuGet.</summary>
	public class SharpCompressDearchivalMethod : IFileDearchivalMethod<SharpCompressArchiveFile>
	{
		private SharpCompressDearchivalMethod() {}

		public bool CheckSignature(string fileName, out int offset, out bool isExecutable)
		{
			offset = 0;
			isExecutable = false;

			try
			{
				using var arcTest = ArchiveFactory.Open(fileName); // should throw for non-archives
				if (arcTest.Type != ArchiveType.Tar) return true; // not expecting false positives from anything but .tar for now
			}
			catch
			{
				return false;
			}

			// SharpCompress seems to overzealously flag files it thinks are the in original .tar format, so we'll check for false positives. This affects 0.24.0, and the latest at time of writing, 0.27.1.
			// https://github.com/adamhathcock/sharpcompress/issues/390

			using FileStream fs = new(fileName, FileMode.Open, FileAccess.Read); // initialising and using a FileStream can throw all sorts of exceptions, but I think if we've gotten to this point and the file isn't readable, it makes sense to throw --yoshi
			if (!fs.CanRead || !fs.CanSeek || fs.Length < 512) return false;

			// looking for magic bytes
			fs.Seek(0x101, SeekOrigin.Begin);
			var buffer = new byte[8];
			fs.Read(buffer, 0, 8);
			var s = buffer.BytesToHexString();
			if (s == "7573746172003030" || s == "7573746172202000") return true; // "ustar\000" (libarchive's bsdtar) or "ustar  \0" (GNU Tar)

			Console.WriteLine($"SharpCompress identified file as original .tar format, probably a false positive, ignoring. Filename: {fileName}");
			return false;
		}

		public SharpCompressArchiveFile Construct(string path) => new(path);

		public static readonly SharpCompressDearchivalMethod Instance = new();

		public IReadOnlyCollection<string> AllowedArchiveExtensions { get; } = new[] { ".zip", ".7z", ".rar", ".gz" }; // don't try any .tar.* formats, they don't work
	}
}
