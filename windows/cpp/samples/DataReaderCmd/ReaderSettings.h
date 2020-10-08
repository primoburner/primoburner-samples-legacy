#pragma once
#include "stdafx.h"

enum EAppOption
{
	AO_UNKNOWN = 0,					// no burn option selected
	AO_DEVICE_LIST = 1,				// show device list	- option '-l'
	AO_DEVICE_TRACK_LIST = 2,		// show list of tracks available on the medium mounted in the specified device	- option '-t'
	AO_VIEW_CONTENT = 3,			// display content of track/image layout item - option '-v'
	AO_READ_CONTENT = 4,			// read content(track/file) from source(device/image layout) - option '-r'
};

enum ESourceType
{
	ST_UNKNOWN = 0,					// source is not selected
	ST_DISC_TRACK_USER_DATA = 1,	// source is a medium track - the user data content is to be read
	ST_IMAGE_LAYOUT = 2,			// source is an image file - load its layout in IDataDisc object
	ST_DISC_TRACK_LAYOUT = 3,		// source is a medium track - load its layout in IDataDisc object
};

class TrackRipSettings
{
public:
	TrackRipSettings(int32_t deviceIndex, uint16_t trackIndex, tstring destinationFolder)
	{
		m_DeviceIndex = deviceIndex;
		m_TrackIndex = trackIndex;
		m_DestinationFolder = destinationFolder;
		m_UserDataFileName = _T("data.bin");
		m_RawDataFileName = _T("data_raw.bin");
	}
	virtual ~TrackRipSettings()
	{
	}

public:
	int32_t get_DeviceIndex()
	{
		return m_DeviceIndex;
	}
	uint16_t get_TrackIndex()
	{
		return m_TrackIndex;
	}
	tstring get_DestinationFolder() const
	{
		return m_DestinationFolder;
	}
	tstring get_UserDataFileName() const
	{
		return m_UserDataFileName;
	}
	tstring get_RawDataFileName() const
	{
		return m_RawDataFileName;
	}

private:
	// Index of device to read from
	int32_t m_DeviceIndex;
	// Index of disc track to read from
	uint16_t m_TrackIndex;
	// Path to the folder to save the track data files to
	tstring m_DestinationFolder;
	// Name of the file to write track user data to
	tstring m_UserDataFileName;
	// Name of the file to write raw track data to (used for CDs only)
	tstring m_RawDataFileName;
};

struct AppFunctionality
{
	int32_t		DeviceIndex;
	uint16_t		TrackIndex;
	tstring		ImageSource;
	tstring		ItemPath;
	tstring		DestinationFolder;
	ESourceType	SourceType;

	EAppOption	AppOption;
};