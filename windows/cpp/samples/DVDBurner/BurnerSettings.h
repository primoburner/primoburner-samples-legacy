#pragma once

enum
{
	SMALL_FILES_CACHE_LIMIT	= 20000,
	SMALL_FILE_SECTORS		= 10, 
	MAX_SMALL_FILE_SECTORS	= 1000
};

// CreateImage Settings
struct CreateImageSettings
{
	tstring ImageFile;
	tstring SourceFolder;
	
	tstring VolumeLabel;
	primo::burner::ImageTypeFlags::Enum ImageType;
	bool VideoDVD;

	CreateImageSettings()
	{
		ImageType = primo::burner::ImageTypeFlags::None;
		VideoDVD = false;
	}
};

// BurnImage Settings
struct BurnImageSettings
{
	tstring ImageFile;

	primo::burner::WriteMethod::Enum WriteMethod;
	uint32_t WriteSpeedKb;
	
	bool Simulate;
	bool CloseDisc;
	bool Eject;

	BurnImageSettings()
	{
		WriteMethod = primo::burner::WriteMethod::Packet;
		WriteSpeedKb = 0;
		
		Simulate = false;
		CloseDisc = true;
		Eject = true;
	}
};

// Burn Settings
struct BurnSettings
{
	tstring SourceFolder;
	tstring VolumeLabel;

	primo::burner::ImageTypeFlags::Enum ImageType;
	bool VideoDVD;

	bool CacheSmallFiles;
	uint32_t SmallFilesCacheLimit;
	uint32_t SmallFileSize;

	primo::burner::WriteMethod::Enum WriteMethod;
	uint32_t WriteSpeedKb;
	
	bool LoadLastTrack;
	bool Simulate;
	bool CloseDisc;
	bool Eject;

	BurnSettings()
	{
		ImageType = primo::burner::ImageTypeFlags::None;
		VideoDVD = false;

		CacheSmallFiles = false;
		SmallFilesCacheLimit = SMALL_FILES_CACHE_LIMIT;
		SmallFileSize = SMALL_FILE_SECTORS;

		WriteMethod = primo::burner::WriteMethod::Packet;
		WriteSpeedKb = 0;
		
		LoadLastTrack = false;
		Simulate = false;
		CloseDisc = true;
		Eject = true;
	}
};

// Format Settings
struct FormatSettings
{
	bool Quick; 		// Quick format
	bool Force;			// Format even if disc is already formatted

	// Constructor
	FormatSettings()	
	{
		Quick = true;
		Force = false;
	}
};

// Erase Settings
struct EraseSettings
{
	bool Quick; 		// Quick erase
	bool Force;			// Erase even if disc is already blank

	// Constructor
	EraseSettings()	
	{
		Quick = true;
		Force = false;
	}
};
