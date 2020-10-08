#pragma once

#include "stdafx.h"
#include "ReaderSettings.h"
#include "ReaderCallback.h"

struct DeviceInfo
{
	int32_t Index;
	tstring Title;
	char	DriveLetter;
};

struct TrackDetails
{
	int32_t TrackIndex;
	tstring DisplayTitle;


	TrackDetails(int32_t trackIndex, tstring displayTitle)
	{
		TrackIndex = trackIndex;
		DisplayTitle = displayTitle;
	}
};

struct TrackSegment
{
	uint32_t StartAddress;
	uint32_t RecordedSize;
	uint32_t EndAddress;

	TrackSegment(uint32_t startAddress, uint32_t recordedSize)
	{
		StartAddress = startAddress;
		RecordedSize = recordedSize;
		EndAddress = startAddress + recordedSize - 1;
	}
};

struct FileSegment
{
	uint32_t StartAddress;
	uint32_t EndAddress;
	uint64_t FileSize;

	FileSegment(uint32_t address, uint64_t fileSize)
	{
		StartAddress = address;
		FileSize = fileSize;
		EndAddress = StartAddress + (uint32_t)(fileSize / BlockSize::CDRom);
		if (fileSize % BlockSize::CDRom)
			EndAddress += 1;
	}
};

struct LayoutItem
{
	filetime_t	FileTime;
	tstring		FileName;
	bool		IsDirectory;
	uint32_t	Address;
	uint64_t	SizeInBytes;
};

typedef std::vector<DeviceInfo> DeviceVector;
typedef std::vector<TrackDetails> TrackVector;
typedef std::vector<LayoutItem> LayoutItemVector;
typedef std::vector<TrackSegment> TrackSegmentVector;

class Reader
{
public:
	Reader(void);
	virtual ~Reader(void);

public:
	bool Open();
	void Close();
	void set_Callback(ReaderCallback* callback);
	const DeviceVector& EnumerateDevices();
	const TrackVector& EnumerateTracks(int32_t deviceIndex);
	void ReadTrackUserData(TrackRipSettings& settings);
	void PrepareSource(int32_t deviceIndex, int32_t trackIndex);
	void PrepareSource(tstring& imageFile);
	void ReadFileFromSource(tstring filePath, tstring destinationFolder);
	void GetFolderContentFromLayout(tstring folderPath, LayoutItemVector& content);

protected:
	void CleanupEngine();
	void CleanupDeviceEnum();
	void CleanupDevice();
	void CleanupDataDisc();
	void SelectDevice(int32_t deviceIndex, bool exclusive);
	tstring get_DeviceTitle(Device* device);
	void BuildTrackSegmentList(Device* device, TrackInfoEx *pTrackInfo, TrackSegmentVector& segments);
	int GetLastCompleteTrack(Device * device);
	void PrepareCDTracks(Device* device);
	void PrepareDVDBDTracks(Device* device);
	bool ReadFileSectionFromDevice(FileSegment& segment, TrackBuffer *pBuffer, FILE* destinationFile);
	void ReadFileFromDisc(DataFile* dataFile, tstring destinationFolder);
	void ReadFileFromImage(DataFile* dataFile, tstring destinationFolder);
	DataFile* FindItem(tstring& path);
	void RipCDTrack(Device* device, TrackRipSettings& settings);
	void RipDVDBDTrack(Device * device, TrackRipSettings& settings);
	void WriteToFile(FILE * file, TrackBuffer *pBuffer);
	void WriteToRawFile(FILE * file, TrackType::Enum tt, TrackBuffer *pBuffer, int32_t startFrame);

	void CallOnReadProgress(uint32_t startFrame, uint32_t numberOfFrames, uint32_t frameSize);
	void CallOnBlockSizeChanged(uint32_t currentBlockSize, uint32_t newBlockSize, uint32_t startFrame);
	void CallShowMessage(tstring message);

private:
	bool m_isOpen;
	bool m_UseImage;
	int32_t m_TrackIndex;

	tstring m_ImageFile;

	DeviceVector m_DeviceVector;
	TrackVector m_TrackVector;

	Engine* m_Engine;
	DeviceEnum* m_Enumerator;
	Device* m_Device;
	DataDisc* m_DataDisc;

	ReaderCallback* m_Callback;
};
