using System;

namespace DataReaderCmd.NET
{
	public class ReaderEventProvider
	{
		public delegate void ReadProgress(int startFrame, int numberOfFrames, int frameSize);
		public delegate void BlockSizeChange(int oldBlockSize, int newBlockSize, int startFrame);
		public delegate void ReaderNotificationMessage(string message);
	}
}
