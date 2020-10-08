#include "stdafx.h"
#include "Burner.h"

Burner::Burner(void)
{
	m_pEngine = NULL;
	m_pSrcDevice = NULL;
	m_pDstDevice = NULL;

	set_IsOpen(false);
	set_Callback(NULL);
	m_MediaType = MT_None;
}

Burner::~Burner(void)
{
	m_OriginalMediaProfile = MediaProfile::Unknown;
}

void Burner::set_Callback(IBurnerCallback* value)
{
	m_pCallback = value;
}

void Burner::Open()
{	
	if (get_IsOpen())
		return;

	// Enable trace log
	Library::enableTraceLog(NULL, TRUE);

	m_pEngine = Library::createEngine();
	if (!m_pEngine->initialize()) 
	{
		m_pEngine->release();
		m_pEngine = NULL;

		throw BurnerException(ENGINE_INITIALIZATION, ENGINE_INITIALIZATION_TEXT);
	}

	set_IsOpen(true);
}

void Burner::Close()
{
	m_MediaType = MT_None;
	ReleaseDevices();

	if (m_pEngine)
	{
		m_pEngine->shutdown();
		m_pEngine->release();
	}

	m_pEngine = NULL;

	Library::disableTraceLog();

	set_IsOpen(false);
}

const MediaProfile::Enum Burner::get_SrcMediaProfile() const
{
	if (NULL == m_pSrcDevice)
		throw BurnerException(NO_DEVICE, NO_DEVICE_TEXT);
	if (MediaReady::Present == m_pSrcDevice->mediaState())
	{
		return m_pSrcDevice->mediaProfile();
	}
	else
	{
		return MediaProfile::Unknown;
	}
}

const bool Burner::get_MediaIsBlank() const
{
	if (NULL == m_pSrcDevice)
		throw BurnerException(NO_DEVICE, NO_DEVICE_TEXT);

	return 1 == m_pSrcDevice->isMediaBlank();
}

const bool Burner::get_MediaIsFullyFormatted() const
{
	if (NULL == m_pSrcDevice)
		throw BurnerException(NO_DEVICE, NO_DEVICE_TEXT);

	// Get media profile
	MediaProfile::Enum mp = m_pSrcDevice->mediaProfile();

	// DVD+RW
	if (MediaProfile::DVDPlusRW == mp)
		return (BgFormatStatus::Completed == m_pSrcDevice->bgFormatStatus());

	// DVD-RW for Restricted Overwrite
	if (MediaProfile::DVDMinusRWRO == mp)
		return m_pSrcDevice->mediaCapacity() == m_pSrcDevice->mediaFreeSpace();

	return false;
}

const uint32_t Burner::get_DeviceCacheSize() const
{
	if (NULL == m_pSrcDevice)
		throw BurnerException(NO_DEVICE, NO_DEVICE_TEXT);

	return m_pSrcDevice->internalCacheCapacity();
}

const uint32_t Burner::get_DeviceCacheUsedSize() const
{
	if (NULL == m_pSrcDevice)
		throw BurnerException(NO_DEVICE, NO_DEVICE_TEXT);

	return m_pSrcDevice->internalCacheUsedSpace();
}

const uint32_t Burner::get_WriteTransferKB() const
{
	if (NULL == m_pSrcDevice)
		throw BurnerException(NO_DEVICE, NO_DEVICE_TEXT);

	return m_pSrcDevice->writeTransferRate();
}

const bool Burner::get_MediaIsValidProfile() const
{
	return get_MediaIsCD() || get_MediaIsDVD() || get_MediaIsBD();
}

const MediaProfile::Enum Burner::get_OriginalMediaProfile() const
{
	return m_OriginalMediaProfile;
}

const bool_t Burner::get_MediaIsCD() const
{
	if (NULL == m_pSrcDevice)
		throw BurnerException(NO_DEVICE, NO_DEVICE_TEXT);

	if (MediaReady::Present == m_pSrcDevice->mediaState())
	{
		return m_pSrcDevice->isMediaCD();
	}

	return FALSE; 
}

const bool_t Burner::get_MediaIsDVD() const
{
	if (NULL == m_pSrcDevice)
		throw BurnerException(NO_DEVICE, NO_DEVICE_TEXT);

	if (MediaReady::Present == m_pSrcDevice->mediaState())
	{
		return m_pSrcDevice->isMediaDVD() ;
	}

	return FALSE; 
}

const bool_t Burner::get_MediaIsBD() const
{
	if (NULL == m_pSrcDevice)
		throw BurnerException(NO_DEVICE, NO_DEVICE_TEXT);

	if (MediaReady::Present == m_pSrcDevice->mediaState())
	{
		return m_pSrcDevice->isMediaBD() ;
	}

	return FALSE; 
}

const bool Burner::get_MediaCanReadSubChannel() const
{
	if (NULL == m_pSrcDevice)
		throw BurnerException(NO_DEVICE, NO_DEVICE_TEXT);

	if (get_MediaIsCD())
	{
		CDFeatures *cdFeatures = m_pSrcDevice->cdFeatures();
		return cdFeatures->canReadRawRWChannel() || cdFeatures->canReadPackedRWChannel();
	}

	return false;
}

const bool_t Burner::get_RawDaoPossible() const
{
	if (NULL == m_pSrcDevice)
		throw BurnerException(NO_DEVICE, NO_DEVICE_TEXT);

	return m_pSrcDevice->cdFeatures()->canWriteRawDao();
}

const bool_t Burner::get_MediaIsRewritable() const
{
	if (NULL == m_pSrcDevice)
		throw BurnerException(NO_DEVICE, NO_DEVICE_TEXT);

	return m_pSrcDevice->isMediaRewritable();
}

const bool_t Burner::get_BDRFormatAllowed() const
{
	if (NULL == m_pSrcDevice)
		throw BurnerException(NO_DEVICE, NO_DEVICE_TEXT);

	return (MediaProfile::BDRSrmPow == m_OriginalMediaProfile) && (MediaProfile::BDRSrm == m_pSrcDevice->mediaProfile());
}

tstring get_DeviceTitle(Device* pDevice)
{
	TCHAR tcsName[200] = {0};
	_stprintf(tcsName, _TEXT("(%c:) - %s"), pDevice->driveLetter(), UTF_TO_TSTR(pDevice->description()).c_str());

	return tcsName;
}

const DeviceVector& Burner::EnumerateDevices()
{
	if (!get_IsOpen())
		throw BurnerException(BURNER_NOT_OPEN, BURNER_NOT_OPEN_TEXT);

	m_deviceVector.clear();

	DeviceEnum* pEnumerator = m_pEngine->createDeviceEnumerator();

		int32_t nDevices = pEnumerator->count();
		if (0 == nDevices) 
		{
			pEnumerator->release();
			throw BurnerException(NO_DEVICES, NO_DEVICES_TEXT);
		}

		for (int32_t i = 0; i < nDevices; i++) 
		{
			Device* pDevice = pEnumerator->createDevice(i);
			if (NULL != pDevice)
			{
				DeviceInfo dev;
				dev.Index = i;
				dev.Title = get_DeviceTitle(pDevice);
				dev.IsWriter = isWritePossible(pDevice);

				m_deviceVector.push_back(dev);

				pDevice->release();
			}
		}

	pEnumerator->release();
	return m_deviceVector;
}

void Burner::SelectDevice(uint32_t deviceIndex, bool exclusive, bool dstDevice)
{
	Device** pDevice = dstDevice ? &m_pDstDevice : &m_pSrcDevice;
	if (NULL != *pDevice)
		throw BurnerException(DEVICE_ALREADY_SELECTED, DEVICE_ALREADY_SELECTED_TEXT);

	DeviceEnum* pEnumerator = m_pEngine->createDeviceEnumerator();

	*pDevice = pEnumerator->createDevice(deviceIndex, exclusive ? 1 : 0);
	if (NULL == *pDevice)
	{
		pEnumerator->release();
		throw BurnerException(INVALID_DEVICE_INDEX, INVALID_DEVICE_INDEX_TEXT);
	}

	pEnumerator->release();
}

void Burner::ReleaseDevices()
{
	if (NULL != m_pSrcDevice)
		m_pSrcDevice->release();

	m_pSrcDevice = NULL;
	if (NULL != m_pDstDevice)
		m_pDstDevice->release();

	m_pDstDevice = NULL;
}

void Burner::RefreshSrcDevice()
{
	if (NULL == m_pSrcDevice)
		throw BurnerException(NO_DEVICE, NO_DEVICE_TEXT);

	m_pSrcDevice->refresh();
}

bool Burner::get_IsOpen() const
{
	return m_isOpen;
}

void Burner::set_IsOpen(bool isOpen)
{
	m_isOpen = isOpen;
}

// IDiscCopyCallback
void Burner::onProgress(uint32_t dwPosition, uint32_t dwAll)
{
	if (m_pCallback)
		m_pCallback->OnCopyProgress(dwPosition, dwAll);
}

void Burner::onStatus(DiscCopyStatus::Enum eStatus) 
{
	if (m_pCallback)
	{
		tstring status = GetDiscCopyStatusString(eStatus);
		m_pCallback->OnStatus(status);
	}
}

void Burner::onTrackStatus(int nTrack, int nPercent) 
{
	if (m_pCallback)
	{
		m_pCallback->OnTrackStatus(nTrack, nPercent);
	}
}

BOOL Burner::onContinueCopy() 
{
	if (m_pCallback)
		return m_pCallback->OnContinue() ? TRUE : FALSE;

	return TRUE;
}

// DeviceCallback
void Burner::onFormatProgress(double fPercentCompleted)
{
	if (m_pCallback)
		m_pCallback->OnFormatProgress(fPercentCompleted);
}

void Burner::onEraseProgress(double fPercentCompleted)
{
	if (m_pCallback)
		m_pCallback->OnEraseProgress(fPercentCompleted);
}
///
void Burner::CreateImage(const CreateImageSettings& settings) 
{
	m_MediaType = MT_None;
	if (NULL == m_pSrcDevice)
		throw BurnerException(NO_DEVICE, NO_DEVICE_TEXT);

	DiscCopy* pDiscCopy = Library::createDiscCopy();

	try
	{
		pDiscCopy->setCallback(this);
		tstring imageFile = settings.ImageFolderPath + IMAGE_DESCRIPTION_FILE_NAME;

		if (MediaReady::Present == m_pSrcDevice->mediaState())
		{
			m_OriginalMediaProfile = m_pSrcDevice->mediaProfile();
			// Create an image for the selected medium
			if (m_pSrcDevice->isMediaCD())
			{
				m_MediaType = MT_CD;
				CDCopyReadMethod::Enum readMethod = settings.ReadSubChannel ? CDCopyReadMethod::FullRaw : CDCopyReadMethod::Raw2352;
				if (!pDiscCopy->createImageFromCD(TSTR_TO_UTF(imageFile), m_pSrcDevice, readMethod)) 
				{
					throw BurnerException(pDiscCopy);
				}
			}
			else if (m_pSrcDevice->isMediaDVD())
			{
				m_MediaType = MT_DVD;
				if (!pDiscCopy->createImageFromDVD(TSTR_TO_UTF(imageFile), m_pSrcDevice)) 
				{
					throw BurnerException(pDiscCopy);
				}
			}
			else if (m_pSrcDevice->isMediaBD())
			{
				m_MediaType = MT_BD;
				if (!pDiscCopy->createImageFromBD(TSTR_TO_UTF(imageFile), m_pSrcDevice)) 
				{
					throw BurnerException(pDiscCopy);
				}
			}
			else
			{
				m_OriginalMediaProfile = MediaProfile::Unknown;
				throw BurnerException(MEDIA_TYPE_NOT_SUPPORTED, MEDIA_TYPE_NOT_SUPPORTED_TEXT);
			}
		}
		else
		{
			throw BurnerException(DEVICE_NOT_READY, DEVICE_NOT_READY_TEXT);
		}

		pDiscCopy->release();
	}
	catch(...)
	{
		pDiscCopy->release();
		throw;
	}
}

void Burner::BurnImage(const BurnImageSettings& settings) 
{
	if (NULL == m_pSrcDevice)
		throw BurnerException(NO_DEVICE, NO_DEVICE_TEXT);

	DiscCopy *pDiscCopy = Library::createDiscCopy();

	try
	{
		pDiscCopy->setCallback(this);
		tstring imageFile = settings.ImageFolderPath + IMAGE_DESCRIPTION_FILE_NAME;

		if (MediaReady::Present == m_pSrcDevice->mediaState())
		{
			// Write the image to the CD/DVD/BD
			if (MT_CD == m_MediaType)
			{
				if (!pDiscCopy->writeImageToCD(m_pSrcDevice, TSTR_TO_UTF(imageFile), settings.WriteMethod)) 
				{
					throw BurnerException(pDiscCopy);
				}
			}
			else if (MT_DVD == m_MediaType)
			{
				if (!pDiscCopy->writeImageToDVD(m_pSrcDevice, TSTR_TO_UTF(imageFile))) 
				{
					throw BurnerException(pDiscCopy);
				}
			}
			else if (MT_BD == m_MediaType)
			{
				if (!pDiscCopy->writeImageToBD(m_pSrcDevice, TSTR_TO_UTF(imageFile))) 
				{
					throw BurnerException(pDiscCopy);
				}
			}

		}
		else
		{
			throw BurnerException(DEVICE_NOT_READY, DEVICE_NOT_READY_TEXT);
		}
		pDiscCopy->release();
	}
	catch(...)
	{
		pDiscCopy->release();
		throw;
	}
}

void Burner::DirectCopy(const DirectCopySettings& settings)
{
	if (NULL == m_pSrcDevice || NULL == m_pDstDevice)
	{
		throw BurnerException(NO_DEVICE, NO_DEVICE_TEXT);
	}

	DiscCopy *pDiscCopy = Library::createDiscCopy();

	try
	{
		pDiscCopy->setCallback(this);

		if (MediaReady::Present == m_pSrcDevice->mediaState() &&
			MediaReady::Present == m_pDstDevice->mediaState())
		{
			// Copy a CD/DVD/BD medium
			if (m_pSrcDevice->isMediaCD())
			{
				CDCopyReadMethod::Enum readMethod = settings.ReadSubChannel ? CDCopyReadMethod::FullRaw : CDCopyReadMethod::Raw2352;
				if (!pDiscCopy->copyCD(m_pDstDevice, m_pSrcDevice, settings.UseTemporaryFiles, readMethod, settings.WriteMethod)) 
				{
					throw BurnerException(pDiscCopy);
				}
			}
			else if (m_pSrcDevice->isMediaDVD())
			{
				if (!pDiscCopy->copyDVD(m_pDstDevice, m_pSrcDevice, settings.UseTemporaryFiles)) 
				{
					throw BurnerException(pDiscCopy);
				}
			}
			else if (m_pSrcDevice->isMediaBD())
			{
				if (!pDiscCopy->copyBD(m_pDstDevice, m_pSrcDevice, settings.UseTemporaryFiles)) 
				{
					throw BurnerException(pDiscCopy);
				}
			}
			else
			{
				throw BurnerException(MEDIA_TYPE_NOT_SUPPORTED, MEDIA_TYPE_NOT_SUPPORTED_TEXT);
			}
		}
		else
		{
			throw BurnerException(DEVICE_NOT_READY, DEVICE_NOT_READY_TEXT);
		}
		pDiscCopy->release();
	}
	catch(...)
	{
		pDiscCopy->release();
		throw;
	}

}
void Burner::CleanMedia(const CleanMediaSettings& settings)
{
	if (NULL == m_pSrcDevice)
		throw BurnerException(NO_DEVICE, NO_DEVICE_TEXT);
	
	m_pSrcDevice->setCallback(this);
	BOOL bRes = TRUE;
	MediaProfile::Enum mp = m_pSrcDevice->mediaProfile();
	if (CM_Erase == settings.MediaCleanMethod)
	{
		if (MediaProfile::DVDMinusRWSeq != mp && MediaProfile::DVDMinusRWRO != mp && MediaProfile::CDRW != mp)
		{
			throw BurnerException(ERASE_NOT_SUPPORTED, ERASE_NOT_SUPPORTED_TEXT);
		}

		bRes = m_pSrcDevice->erase(settings.Quick ? EraseType::Minimal : EraseType::Disc);
	}
	else if (CM_Format == settings.MediaCleanMethod)
	{
		switch(mp)
		{
			case MediaProfile::DVDMinusRWSeq:
			case MediaProfile::DVDMinusRWRO:
				bRes = m_pSrcDevice->format(settings.Quick ? FormatType::DVDMinusRWQuick : FormatType::DVDMinusRWFull);
			break;

			case MediaProfile::DVDPlusRW:
			{
				BgFormatStatus::Enum fmt = m_pSrcDevice->bgFormatStatus();
				switch(fmt)
				{
					case BgFormatStatus::NotFormatted:
						bRes = m_pSrcDevice->format(FormatType::DVDPlusRWFull, 0, !settings.Quick);
					break;
					case BgFormatStatus::Partial:
						bRes = m_pSrcDevice->format(FormatType::DVDPlusRWRestart, 0, !settings.Quick);
					break;
					case BgFormatStatus::Completed:
						bRes = TRUE;
						break;
					case BgFormatStatus::Pending:
						bRes = FALSE;
						break;
				}
				break;
			}
			case MediaProfile::DVDRam:
			{
				if(!m_pSrcDevice->isMediaFormatted()) 
				{ 
					bRes = m_pSrcDevice->format(FormatType::DVDRamFull);
				} 
				break;
			}
			case MediaProfile::BDRE:
			{
				bRes = m_pSrcDevice->formatBD(BDFormatType::BDFull, BDFormatSubType::BDREQuickReformat);
				break;
			}
			case MediaProfile::BDRSrm:
			{
				bRes = m_pSrcDevice->formatBD(BDFormatType::BDFull, BDFormatSubType::BDRSrmPow);
				break;
			}
			default:
				throw BurnerException(FORMAT_NOT_SUPPORTED, FORMAT_NOT_SUPPORTED_TEXT);
		}
	}
	if (!bRes)
	{
		throw BurnerException(m_pSrcDevice);
	}
	// Refresh to reload disc information
	m_pSrcDevice->refresh();
}

tstring Burner::GetDiscCopyStatusString(DiscCopyStatus::Enum eStatus)
{
	switch(eStatus)
	{
		case DiscCopyStatus::ReadingData:
			return tstring(_TEXT("Reading disc..."));
		
		case DiscCopyStatus::ReadingToc:
			return tstring(_TEXT("Reading disc TOC..."));

		case DiscCopyStatus::WritingData:
			return tstring(_TEXT("Writing image..."));

		case DiscCopyStatus::WritingLeadOut:
			return tstring(_TEXT("Flushing device cache and writing lead-out..."));

	}

	return tstring(_TEXT("Unknown status..."));
}

bool Burner::isWritePossible(Device *device) const
{
	CDFeatures *cdfeatures = device->cdFeatures();
	DVDFeatures *dvdfeatures = device->dvdFeatures();
	BDFeatures *bdfeatures = device->bdFeatures();

	bool cdWritePossible = cdfeatures->canWriteCDR() || cdfeatures->canWriteCDRW();
	bool dvdWritePossible = dvdfeatures->canWriteDVDMinusR() || dvdfeatures->canWriteDVDMinusRDL() ||
		dvdfeatures->canWriteDVDPlusR() || dvdfeatures->canWriteDVDPlusRDL() ||
		dvdfeatures->canWriteDVDMinusRW() || dvdfeatures->canWriteDVDPlusRW() ||
		dvdfeatures->canWriteDVDRam();
	bool bdWritePossible = bdfeatures->canWriteBDR() || bdfeatures->canWriteBDRE();
	return cdWritePossible || dvdWritePossible || bdWritePossible;
}
