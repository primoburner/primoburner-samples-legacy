using System;
using PrimoSoftware.Burner;

namespace BluRayBurner.NET
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
        public PrimoSoftware.Burner.UdfRevision UdfRevision = PrimoSoftware.Burner.UdfRevision.Revision102;
        public bool BDVideo = false;
	};

	// BurnImage Settings
	public class BurnImageSettings
	{
		public string ImageFile = "";

		public int WriteSpeedKB = 0;

		public bool CloseDisc = true;
		public bool Eject = true;
	};

	// Burn Settings
	public class BurnSettings
	{
		public string SourceFolder;
		public string VolumeLabel;

		public PrimoSoftware.Burner.ImageType ImageType = ImageType.None;
        public PrimoSoftware.Burner.UdfRevision UdfRevision = PrimoSoftware.Burner.UdfRevision.Revision102;
        public bool BDVideo = false;

		public bool CacheSmallFiles = false;
		public long SmallFilesCacheLimit = (long)SmallFiles.SMALL_FILES_CACHE_LIMIT;
		public long SmallFileSize = (long)SmallFiles.SMALL_FILE_SECTORS;

		public int WriteSpeedKB = 0;
	
		public bool LoadLastTrack = false;
		public bool CloseDisc = true;
		public bool Eject = true;
	};

	// Format Settings
	public class FormatSettings
	{
		public bool Quick = true; 		// Quick format
		public bool Force = false;		// Format even if disc is already formatted
	};
}
