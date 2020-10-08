using System;
using System.Collections.Generic;
using System.Text;

namespace DataBurnerCmd.NET
{
	public class BurnerEventProvider
	{
		public delegate void Status(string message);
		public delegate void Progress(long pos, long all);
		public delegate void FileProgress(int file, string fileName, int percentCompleted);
		public delegate void FormatProgress(double percentCompleted);
		public delegate void EraseProgress(double percentCompleted);
		public delegate bool Continue();
	}
}
