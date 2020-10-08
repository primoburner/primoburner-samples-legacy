#pragma once

#include "stdafx.h"
#include "BurnerSettings.h"
#include "BurnerException.h"
#include "BurnerCallback.h"

#define IMAGE_DESCRIPTION_FILE_NAME _TEXT("/image.sdi")

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


class Burner : public DiscCopyCallback, public DeviceCallback
{
public:
	
	Burner(void);
	virtual ~Burner(void);

public:
	void set_Callback(IBurnerCallback* value);

	bool get_IsOpen() const;
	void set_IsOpen(bool isOpen);

	const MediaProfile::Enum get_SrcMediaProfile() const;

	const bool get_MediaIsBlank() const;
	const bool get_MediaIsFullyFormatted() const;

	const uint32_t get_DeviceCacheSize() const;
	const uint32_t get_DeviceCacheUsedSize() const;
	const uint32_t get_WriteTransferKB() const;

	const bool get_MediaIsValidProfile() const;
	const bool_t get_MediaIsCD() const;
	const bool_t get_MediaIsDVD() const;
	const bool_t get_MediaIsBD() const;
	const bool get_MediaCanReadSubChannel() const;
	const bool_t get_RawDaoPossible() const;
	const bool_t get_MediaIsRewritable() const;
	const bool_t get_BDRFormatAllowed() const;

	const MediaProfile::Enum get_OriginalMediaProfile() const;

	void Open();
	void Close();

	void SelectDevice(uint32_t deviceIndex, bool exclusive, bool dstDevice);

	void ReleaseDevices();

	const uint64_t CalculateImageSize(const tstring& sourceFolder, ImageTypeFlags::Enum imageType);

	const DeviceVector& EnumerateDevices();

	void CreateImage(const CreateImageSettings& settings);
	void BurnImage(const BurnImageSettings& settings);
	void DirectCopy(const DirectCopySettings& settings);
	
	void CleanMedia(const CleanMediaSettings& settings);
	void RefreshSrcDevice();

// DeviceCallback
public:
	void onFormatProgress(double fPercentCompleted);
	void onEraseProgress(double fPercentCompleted);

// IDiscCopyCallback
public:
	void onProgress(uint32_t dwPosition, uint32_t dwAll);
	void onStatus(DiscCopyStatus::Enum eStatus);
	void onTrackStatus(int nTrack, int nPercent);
	bool_t onContinueCopy();

protected:
	tstring GetDiscCopyStatusString(DiscCopyStatus::Enum eStatus);

	bool isWritePossible(Device *device) const;

private:
	bool m_isOpen;

	DeviceVector m_deviceVector;
	SpeedVector m_speedVector;

	Engine* m_pEngine;
	Device* m_pSrcDevice;
	Device* m_pDstDevice;

	IBurnerCallback* m_pCallback;

	EMediaType m_MediaType;
	MediaProfile::Enum m_OriginalMediaProfile;
};
