using System;

namespace BluRayBurner.NET
{
	public class BurnerCallback
	{
		public delegate void Status(string message);
		public delegate void ImageProgress(long ddwPos, long ddwAll);
		public delegate void FileProgress(int file, string fileName, int percentCompleted);
		public delegate void FormatProgress(double percentCompleted);
		public delegate void EraseProgress(double percentCompleted);
		public delegate bool Continue();
	}

	public class ProgressInfo
	{
		public string Message = "";
		public int Percent = 0;
		public int UsedCachePercent = 0;
		public int ActualWriteSpeed = 0;
	}
}
