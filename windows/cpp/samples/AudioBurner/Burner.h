#pragma once

#include "stdafx.h"

#include "BurnerSettings.h"
#include "BurnerException.h"

struct DeviceInfo
{
	int32_t Index;
	tstring Title;
	bool IsWriter;
};

typedef stl::vector<DeviceInfo> DeviceVector;

struct SpeedInfo
{
	int32_t TransferRateKB;
	int32_t TransferRate1xKB;
};

typedef stl::vector<SpeedInfo> SpeedVector;

typedef stl::vector<int> IntVect;

class Burner
{
public:
	Burner(void);
	virtual ~Burner(void);

public:
	bool get_IsOpen() const;
	
	void set_Callback(AudioCDCallback* value);

	void CloseTray();
	void Eject();

	const DeviceVector& EnumerateDevices();
	const SpeedVector& EnumerateWriteSpeeds();

	void Open();
	void Close();

	uint32_t get_DeviceCacheSize() const;
	uint32_t get_DeviceCacheUsedSize() const;
	uint32_t get_WriteTransferKB() const;

	void SelectDevice(uint32_t deviceIndex, bool exclusive);
	void ReleaseDevice();
	void ReleaseAudio();
	tstring TranslateAudioCDStatus(AudioCDStatus::Enum status);

	uint32_t get_MediaFreeSpace() const;
	bool get_MediaIsBlank() const;
	int32_t get_MaxWriteSpeedKB() const;

	bool get_CDTextSupport() const;

	bool get_ReWritePossible() const;
	bool get_MediaIsReWritable() const;

	bool get_SaoPossible() const;
	bool get_TaoPossible() const;

	void Burn(const BurnSettings& settings);
	void Erase(const EraseSettings& settings);

private:
	void SetCDText(CDText* pCDText, const CDTextEntry &cdTextEntry, int nItem);
	void BurnInternal(const BurnSettings& settings);
	void CheckSuccess(AudioCD* audioCD);

protected:
	bool isWritePossible(Device *device) const;
	bool isRewritePossible(Device *device) const;

private:
	DeviceVector m_deviceVector;
	SpeedVector m_speedVector;


	Engine* m_pEngine;
	Device* m_pDevice;
	AudioCD* m_pAudio;

	AudioCDCallback* m_pCallback;
};

