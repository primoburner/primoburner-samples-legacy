#pragma once

#include "stdafx.h"

#include "BurnerSettings.h"
#include "BurnerException.h"
#include "BurnerCallback.h"

struct DeviceInfo
{
	int32_t Index;
	tstring Title;
	bool IsWriter;
};
typedef std::vector<DeviceInfo> DeviceVector;

struct Speed
{
	int32_t TransferRateKB;
	int32_t TransferRate1xKB;
};
typedef std::vector<Speed> SpeedVector;


class Burner : public DataDiscCallback, public DeviceCallback
{
public:
	
	Burner(void);
	virtual ~Burner(void);

public:
	void set_Callback(IBurnerCallback* value);

	bool get_IsOpen() const;
	void set_IsOpen(bool isOpen);

	const int32_t get_MaxWriteSpeedKB() const;
	const uint32_t get_MediaFreeSpace() const;

	const MediaProfile::Enum get_MediaProfile() const;
	const tstring get_MediaProfileString() const;

	const bool get_MediaIsBlank() const;
	const bool get_MediaIsFullyFormatted() const;

	const uint32_t get_DeviceCacheSize() const;
	const uint32_t get_DeviceCacheUsedSize() const;
	const uint32_t get_WriteTransferKB() const;

	void Open();
	void Close();

	void SelectDevice(uint32_t deviceIndex, bool exclusive);
	void ReleaseDevice();

	const uint64_t CalculateImageSize(const tstring& sourceFolder, ImageTypeFlags::Enum imageType, UdfRevision::Enum udfRevision);

	const DeviceVector& EnumerateDevices();
	const SpeedVector& EnumerateWriteSpeeds();

	void CloseTray();
	void Eject();

	void CreateImage(const CreateImageSettings& settings);
	void BurnImage(const BurnImageSettings& settings);
	
	void Burn(const BurnSettings& settings);
	
	void Format(const FormatSettings& settings);

	void DismountMedia();

// DeviceCallback
public:
	void onFormatProgress(double progress);
	void onEraseProgress(double progress);

// DataDiscCallback
public:
	void onProgress(int64_t bytesWritten, int64_t all);
	void onStatus(DataDiscStatus::Enum eStatus);
	void onFileStatus(int32_t fileNumber, const char_t* filename, int32_t percentWritten);
	bool_t onContinueWrite();

protected:
	BOOL FormatMedia(Device * pDevice);
	int GetLastCompleteTrack(Device * pDevice);

	void ProcessInputTree(DataFile * pCurrentFile, tstring& currentPath);
	void SetImageLayoutFromFolder(DataDisc* pDataCD, LPCTSTR sourceFolder);

	void SetVolumeProperties(DataDisc* pDataDisc, const tstring& volumeLabel, primo::burner::ImageTypeFlags::Enum imageType);
	tstring GetDataDiscStatusString(DataDiscStatus::Enum eStatus);

	bool isWritePossible(Device *device) const;

private:
	bool m_isOpen;

	DeviceVector m_deviceVector;
	SpeedVector m_speedVector;

	Engine* m_pEngine;
	Device* m_pDevice;

	IBurnerCallback* m_pCallback;
};
