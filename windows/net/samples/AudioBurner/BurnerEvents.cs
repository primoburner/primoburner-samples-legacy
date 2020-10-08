using System;

namespace AudioBurner.NET
{
    
	public class BurnerCallback
	{
		public delegate void Status(string message);
        public delegate void Progress(int percentCompleted);
		public delegate void TrackProgress(int nTrack, int percentCompleted);
		public delegate bool Continue();
        public delegate void EraseProgress(int percentCompleted);
	}

	public class ProgressInfo
	{
		public string Message = string.Empty;
		public int Percent = 0;
		public int UsedCachePercent = 0;
		public int ActualWriteSpeed = 0;
	}
}
