using System;
using PrimoSoftware.Burner;
using System.Collections.Generic;

namespace AudioBurner.NET
{

	// Burn Settings
	public class BurnSettings
	{
		public PrimoSoftware.Burner.WriteMethod WriteMethod = WriteMethod.Sao; 
		public int WriteSpeedKB = 0;
	
		public bool Simulate = false;
		public bool CloseDisk = true;
		public bool Eject = true;
        public bool WriteCDText = false;
		public bool CreateHiddenTrack = false;
        public bool DecodeInTempFiles = false;
        public List<string> Files = new List<string>(10);
        public CDTextSettings CDText = new CDTextSettings();
        public bool UseAudioStream = false;
	};

	// Erase Settings
	public class EraseSettings
	{
		public bool Quick = true; 		// Quick erase
		public bool Force = false;		// Erase even if disc is already blank
	};

    // CD-Text Record
    public class CDTextEntry
    {
        //NOTE: non-null strings must be passed to PrimoBurner if CD-TEXT is used
	    public string Title = string.Empty;
	    public string Performer = string.Empty;
	    public string SongWriter = string.Empty;
	    public string Composer = string.Empty;
	    public string Arranger = string.Empty;
	    public string Message = string.Empty;
	    public string DiskId = string.Empty;
	    public int Genre = -1;
		public string GenreText = string.Empty;
	    public string UpcIsrc = string.Empty;
    };

    // CD-Text Settings
    public class CDTextSettings
    {
	    public CDTextEntry Album = new CDTextEntry();
	    public CDTextEntry[] Songs = new CDTextEntry[99];
    }
}
