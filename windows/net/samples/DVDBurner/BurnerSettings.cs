using System;
using PrimoSoftware.Burner;

namespace DVDBurner.NET
{
	enum SmallFiles
	{
		SMALL_FILES_CACHE_LIMIT = 20000,
		SMALL_FILE_SECTORS		= 10, 
		MAX_SMALL_FILE_SECTORS	= 1000
	};

	// CreateImage Settings
	public class CreateImageSettings
	{
		public string ImageFile = "";
		public string SourceFolder = "";
	
		public string VolumeLabel = "";
		public PrimoSoftware.Burner.ImageType ImageType = PrimoSoftware.Burner.ImageType.None;
		public bool VideoDVD = false;
	};


	// BurnImage Settings
	public class BurnImageSettings
	{
		public string ImageFile = "";

		public PrimoSoftware.Burner.WriteMethod WriteMethod = WriteMethod.DvdIncremental; 
		public int WriteSpeedKB = 0;
	
		public bool Simulate = false;
		public bool CloseDisc = true;
		public bool Eject = true;
	};

	// Burn Settings
	public class BurnSettings
	{
		public string SourceFolder;
		public string VolumeLabel;

		public PrimoSoftware.Burner.ImageType ImageType = ImageType.None;
		public bool VideoDVD = false;

		public bool CacheSmallFiles = false;
		public long SmallFilesCacheLimit = (long)SmallFiles.SMALL_FILES_CACHE_LIMIT;
		public long SmallFileSize = (long)SmallFiles.SMALL_FILE_SECTORS;

		public PrimoSoftware.Burner.WriteMethod WriteMethod = WriteMethod.DvdIncremental; 
		public int WriteSpeedKB = 0;
	
		public bool LoadLastTrack = false;
		public bool Simulate = false;
		public bool CloseDisc = true;
		public bool Eject = true;
	};

	// Format Settings
	public class FormatSettings
	{
		public bool Quick = true; 		// Quick format
		public bool Force = false;		// Format even if disc is already formatted
	};

	// Erase Settings
	public class EraseSettings
	{
		public bool Quick = true; 		// Quick erase
		public bool Force = false;		// Erase even if disc is already blank
	};

}
