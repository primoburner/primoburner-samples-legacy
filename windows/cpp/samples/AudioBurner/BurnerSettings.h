#pragma once

struct CDTextEntry
{
	tstring Title;
	tstring Performer;
	tstring SongWriter;
	tstring Composer;
	tstring Arranger;
	tstring Message;
	tstring DiskId;
	tstring Genre;
	tstring GenreText;
	tstring UpcIsrc;
};

struct CDTextSettings
{
	CDTextEntry Album;
	CDTextEntry Songs[99];
};

typedef stl::vector<tstring> TStringVect;

struct BurnSettings
{
	WriteMethod::Enum WriteMethod; 
	uint32_t WriteSpeedKB;
	
	bool Simulate;
	bool Eject;
	bool CloseDisc;
	bool WriteCDText;
	bool DecodeInTempFiles;
	bool CreateHiddenTrack;
	bool UseAudioStream;

	TStringVect Files;
	CDTextSettings CDText;
	
	BurnSettings()
	{
		WriteMethod = WriteMethod::Sao;
		WriteSpeedKB = 0;
		
		Simulate = false;
		CloseDisc = true;
		Eject = true;
		WriteCDText = false;
		DecodeInTempFiles = false;
		CreateHiddenTrack = false;
		UseAudioStream = false;
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
