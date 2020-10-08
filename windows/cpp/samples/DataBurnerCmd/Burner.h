#pragma once

#include "stdafx.h"
#include "BurnerCallback.h"
#include "BurnerSettings.h"
#include "BurnerException.h"

struct DeviceInfo
{
	int32_t Index;
	tstring Title;
	char	DriveLetter;
};

typedef stl::vector<DeviceInfo> DeviceVector;


class Burner : public DataDiscCallback, public DeviceCallback
{
public:
	
	Burner(void);
	virtual ~Burner(void);

public:
	bool Open();
	void Close();

	void SelectDevice(int32_t deviceIndex, bool exclusive = false);

	const DeviceVector& EnumerateDevices();
	void set_Callback(IBurnerCallback* value);
	void BurnImage(const ImageBurnSettings& settings);
	void BurnPacket(const PacketBurnSettings& settings);
	void BurnSimple(const SimpleBurnSettings& settings);
	void Erase();
	void Format();
	bool FormatDVDPlusRW();
	void PrepareMedium();
	void Clean();

// DeviceCallback
public:
	void onFormatProgress(double fPercentCompleted);
	void onEraseProgress(double fPercentCompleted);

// DataDiscCallback
public:
	void onProgress(int64_t bytesWritten, int64_t all);
	void onStatus(DataDiscStatus::Enum eStatus);
	void onFileStatus(int32_t fileNumber, const char_t* filename, int32_t percentWritten);
	bool_t onContinueWrite();

protected:
	void CleanupEngine();
	void CleanupDeviceEnum();
	void CleanupDevice();
	void CleanupDataDisc();

	tstring get_DeviceTitle(Device* pDevice);

	void SetPacketParameters(DataDisc* pDataDisc, const PacketBurnSettings& settings);

	int32_t GetStartAddress(Device * pDevice);
	int GetLastTrackNumber(Device * pDevice, bool completeOnly);
	tstring GetDataDiscStatusString(DataDiscStatus::Enum eStatus);
	void SetVolumeProperties(DataDisc* pDataDisc, const tstring& volumeLabel, primo::burner::ImageTypeFlags::Enum imageType);

private:
	bool m_isOpen;

	DeviceVector m_DeviceVector;

	Engine* m_pEngine;
	DeviceEnum* m_pEnumerator;
	Device* m_pDevice;
	DataDisc* m_pDataDisc;

	IBurnerCallback* m_pCallback;
};