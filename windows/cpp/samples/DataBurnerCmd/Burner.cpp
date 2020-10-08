#include "stdafx.h"
#include "Burner.h"
#include "DataStreamImpl.h"

Burner::Burner(void)
{
	m_pEngine = NULL;
	m_pEnumerator = NULL;
	m_pDevice = NULL;
	m_pDataDisc = NULL;

	m_isOpen = false;
}

Burner::~Burner(void)
{
	Close();
}

bool Burner::Open()
{	
	if (m_isOpen)
		return true;

	// Enable trace log
	Library::enableTraceLog(NULL, TRUE);

	m_pEngine = Library::createEngine();
	if (!m_pEngine->initialize()) 
	{
		throw BurnerException(m_pEngine);
	}

	m_isOpen = true;
	return true;
}

void Burner::Close()
{
	CleanupDataDisc();
	CleanupDevice();
	CleanupDeviceEnum();
	CleanupEngine();

	Library::disableTraceLog();

	m_isOpen = false;
}
void Burner::SelectDevice(int32_t deviceIndex, bool exclusive)
{
	CleanupDeviceEnum();
	CleanupDevice();
	m_pEnumerator = m_pEngine->createDeviceEnumerator();

	m_pDevice = m_pEnumerator->createDevice(deviceIndex, exclusive ? 1 : 0);

	if (NULL == m_pDevice)
	{
		throw BurnerException(m_pEnumerator);
	}
}
const DeviceVector& Burner::EnumerateDevices()
{
	if (!m_isOpen)
	{
		throw BurnerException(BE_BURNER_NOT_OPEN, BE_BURNER_NOT_OPEN_TEXT);
	}

	m_DeviceVector.clear();

	CleanupDeviceEnum();
	m_pEnumerator = m_pEngine->createDeviceEnumerator();

	int32_t nDevices = m_pEnumerator->count();
	if (0 == nDevices) 
	{
		throw BurnerException(BE_NO_DEVICES, BE_NO_DEVICES_TEXT);
	}

	for (int32_t i = 0; i < nDevices; i++) 
	{
		Device* pDevice = m_pEnumerator->createDevice(i);
		if (NULL != pDevice)
		{
			DeviceInfo dev;
			dev.Index = i;
			dev.Title = get_DeviceTitle(pDevice);
			dev.DriveLetter = pDevice->driveLetter();

			m_DeviceVector.push_back(dev);

			pDevice->release();
		}
		else
		{
			throw BurnerException(m_pEnumerator);
		}
	}

	return m_DeviceVector;
}

void Burner::set_Callback(IBurnerCallback* value)
{
	m_pCallback = value;
}
void Burner::BurnImage(const ImageBurnSettings& settings)
{
	if (NULL != m_pDevice)
	{
		PrepareMedium();

		CleanupDataDisc();
		m_pDataDisc = Library::createDataDisc();

		// Tell DataDisc which device it should use. Here we just pass the device the user selected.
		m_pDataDisc->setDevice(m_pDevice);

		// Set the write method
		if (m_pDevice->isMediaBD())
		{
			m_pDataDisc->setWriteMethod(WriteMethod::BluRay);
		}
		else if (m_pDevice->isMediaDVD())
		{
			// The best for DVD Media is DVD Incremental
			m_pDataDisc->setWriteMethod(WriteMethod::DVDIncremental);
		}
		else
		{
			// The best for CD Media is Track-At-Once (TAO). 
			m_pDataDisc->setWriteMethod(WriteMethod::Tao);
		}

		// Perform a real burn
		m_pDataDisc->setSimulateBurn(FALSE);

		// Set a callback object for the burning progress
		m_pDataDisc->setCallback(this);

		// Burn it
		CFileStream dataStream(settings.get_ImageFile().c_str());
		if (!m_pDataDisc->writeImageToDiscEx(&dataStream, NULL))
		{
			throw BurnerException(m_pDataDisc);
		}
	}
	else
	{
		throw BurnerException(BE_DEVICE_NOT_SET, BE_DEVICE_NOT_SET_TEXT);
	}
}

void Burner::BurnPacket(const PacketBurnSettings& settings)
{
	if (NULL != m_pDevice)
	{
		PrepareMedium();

		// Get next writable address
		int32_t lStartAddress = GetStartAddress(m_pDevice);
		if (-1 == lStartAddress)
		{
			throw BurnerException(m_pDevice);
		}

		// indicates whether we should load layout from the last track or not.
		BOOL fLoadLastTrack  = PBO_APPEND == settings.get_Option() || PBO_FINALIZE == settings.get_Option();

		CleanupDataDisc();
		// Create DataDisc object 
		m_pDataDisc = Library::createDataDisc();
		
		// Set device 
		m_pDataDisc->setDevice(m_pDevice);

		// Set other parameters
		SetPacketParameters(m_pDataDisc, settings);

		// Get the last track number from the last session if multi-session option was specified
		int nPrevTrackNumber = fLoadLastTrack ? GetLastTrackNumber(m_pDevice, false) : 0;
		m_pDataDisc->setLayoutLoadTrack(nPrevTrackNumber);

		// Set write speed
		int iMaxWriteSpeedKB = m_pDevice->maxWriteSpeedKB();
		//_tprintf(_T("Setting write speed to: %.02f\n"), (double)iMaxWriteSpeedKB / (pDevice->GetMediaIsDVD() ? SPEED_DVD_1X_KB : SPEED_CD_1X_KB));
		m_pDevice->setWriteSpeedKB(iMaxWriteSpeedKB);

		// Set callback
		m_pDataDisc->setCallback(this);

		// Set the session start address. Must do this before intializing the directory structure.
		m_pDataDisc->setSessionStartAddress(lStartAddress);

		// Set image layout
		BOOL bRes = m_pDataDisc->setImageLayoutFromFolder(settings.get_FolderSrc().c_str());
		if (!bRes) 
		{
			throw BurnerException(m_pDataDisc);
		}

		// Burn 
		while (true)
		{
			// Try to write the image
			bRes = m_pDataDisc->writeToDisc();
			if (!bRes)
			{
				// Check if the error is: Cannot load image layout.
				// If so most likely it is an empty formatted DVD+RW or empty formatted DVD-RW RO with one track. 
				if((m_pDataDisc->error()->facility() == ErrorFacility::DataDisc) &&
					(m_pDataDisc->error()->code() == DataDiscError::CannotLoadImageLayout))
				{
					// Set to 0 to disable previous data session loading
					m_pDataDisc->setLayoutLoadTrack(0);

					// try to write it again
					continue;
				}
			}

			break;
		}

		if (!bRes) 
		{
			throw BurnerException(m_pDataDisc);
		}
	}
	else
	{
		throw BurnerException(BE_DEVICE_NOT_SET, BE_DEVICE_NOT_SET_TEXT);
	}
}



void Burner::BurnSimple(const SimpleBurnSettings& settings)
{
	if (NULL != m_pDevice)
	{
		PrepareMedium();

		// Clean previous recordings
		if (SBO_MERGE != settings.get_Option())
			Clean();

		// Tell DataDisc which device it should use. Here we just pass the device the user selected.
		CleanupDataDisc();
		m_pDataDisc = Library::createDataDisc();
		m_pDataDisc->setDevice(m_pDevice);

		// Set data image volume label
		SetVolumeProperties(m_pDataDisc, settings.get_VolumeLabel(), settings.get_ImageType());

		// Set image type. Must be done before calling SetImageLayoutFromFolder.
		m_pDataDisc->setImageType(settings.get_ImageType());

		// Set the session start address. Must do this before intializing the directory structure.
		m_pDataDisc->setSessionStartAddress(m_pDevice->newSessionStartAddress());

		// Load last complete track multi-session
		if (SBO_MERGE == settings.get_Option())
		{
			int nLastCompleteTrack = GetLastTrackNumber(m_pDevice, true);
			m_pDataDisc->setLayoutLoadTrack(nLastCompleteTrack);
		}

		// The easiest is to use the SetImageLayoutFromFolder method to specify a folder the content of which should be recorded to the disc.
		if (!m_pDataDisc->setImageLayoutFromFolder(settings.get_FolderSrc().c_str()))
		{
			throw BurnerException(m_pDataDisc);
		}

		// Set the write method
		if (m_pDevice->isMediaBD())
		{
			m_pDataDisc->setWriteMethod(WriteMethod::BluRay);
		}
		else if (m_pDevice->isMediaDVD())
		{
			// The best for DVD Media is DVD Incremental
			m_pDataDisc->setWriteMethod(WriteMethod::DVDIncremental);
		}
		else
		{
			// The best for CD Media is Track-At-Once (TAO). 
			m_pDataDisc->setWriteMethod(WriteMethod::Tao);
		}

		// Perform a real burn
		m_pDataDisc->setSimulateBurn(FALSE);

		// Set a callback object for the burning progress
		m_pDataDisc->setCallback(this);

		// Do not close disc to allow multisession
		m_pDataDisc->setCloseDisc(FALSE);

		// Set write speed
		int iMaxWriteSpeedKB = m_pDevice->maxWriteSpeedKB();
		m_pDevice->setWriteSpeedKB(iMaxWriteSpeedKB);

		// Burn it
		if (!m_pDataDisc->writeToDisc())
		{
			throw BurnerException(m_pDataDisc);
		}
	}
	else
	{
		throw BurnerException(BE_DEVICE_NOT_SET, BE_DEVICE_NOT_SET_TEXT);
	}
}
void Burner::Erase()
{
	if (NULL != m_pDevice)
	{
		m_pDevice->setCallback(this);
		bool bCleanResult = true;
		MediaProfile::Enum mp = m_pDevice->mediaProfile();
		switch(mp)
		{
			// DVD-RW, Sequential Recording (default for new DVD-RW)
			case MediaProfile::DVDMinusRWSeq:
				bCleanResult = 1 == m_pDevice->erase(EraseType::Minimal);
			break;

			case MediaProfile::CDRW:
				bCleanResult = 1 == m_pDevice->erase(EraseType::Minimal);
			break;

		}
		if (!bCleanResult)
		{
			throw BurnerException(m_pDevice);
		}
	}
	else
	{
		throw BurnerException(BE_DEVICE_NOT_SET, BE_DEVICE_NOT_SET_TEXT);
	}
}


void Burner::Format()
{
	if (NULL != m_pDevice)
	{
		m_pDevice->setCallback(this);
		bool bCleanResult = true;
		MediaProfile::Enum mp = m_pDevice->mediaProfile();
		switch(mp)
		{
			// DVD+RW (needs to be formatted before the disc can be used)
			case MediaProfile::DVDPlusRW:
				bCleanResult = FormatDVDPlusRW();
			break;

			// DVD-RW, Restricted Overwrite (DVD-RW was formatted initially)
			case MediaProfile::DVDMinusRWRO:
				bCleanResult = 1 == m_pDevice->format(FormatType::DVDMinusRWQuick);
			break;

			// BD-RE
			case MediaProfile::BDRE:
				bCleanResult = 1 == m_pDevice->formatBD(BDFormatType::BDFull, BDFormatSubType::BDREQuickReformat);
			break;

			// Format for Pseudo-Overwrite (POW)
			case MediaProfile::BDRSrm:
				bCleanResult = 1 == m_pDevice->formatBD(BDFormatType::BDFull, BDFormatSubType::BDRSrmPow);
			break;

		}
		if (!bCleanResult)
		{
			throw BurnerException(m_pDevice);
		}
	}
	else
	{
		throw BurnerException(BE_DEVICE_NOT_SET, BE_DEVICE_NOT_SET_TEXT);
	}
}

bool Burner::FormatDVDPlusRW()
{
	bool bCleanResult = true;
	if (NULL != m_pDevice)
	{
		BgFormatStatus::Enum fmt = m_pDevice->bgFormatStatus();
		switch(fmt)
		{
			case BgFormatStatus::NotFormatted:
				bCleanResult = 1 == m_pDevice->format(FormatType::DVDPlusRWFull);
				break;
			case BgFormatStatus::Partial:
				bCleanResult = 1 == m_pDevice->format(FormatType::DVDPlusRWRestart);
				break;
		}
	}
	else
	{
		throw BurnerException(BE_DEVICE_NOT_SET, BE_DEVICE_NOT_SET_TEXT);
	}

	return bCleanResult;
}

void Burner::PrepareMedium()
{
	if (NULL != m_pDevice)
	{
		MediaProfile::Enum mp = m_pDevice->mediaProfile();
		switch(mp)
		{
			// DVD+RW (needs to be formatted before the disc can be used)
			case MediaProfile::DVDPlusRW:
				m_pDevice->setCallback(this);
				FormatDVDPlusRW();
				break;
		}
	}
	else
	{
		throw BurnerException(BE_DEVICE_NOT_SET, BE_DEVICE_NOT_SET_TEXT);
	}
}

void Burner::Clean()
{
	if (NULL != m_pDevice)
	{
		MediaProfile::Enum mp = m_pDevice->mediaProfile();
		switch(mp)
		{
			case MediaProfile::DVDPlusRW:
			case MediaProfile::DVDMinusRWRO:
			case MediaProfile::BDRE:
			case MediaProfile::BDRSrm:
				Format();
				break;

			case MediaProfile::DVDMinusRWSeq:
			case MediaProfile::CDRW:
				Erase();
			break;
		}
	}
	else
	{
		throw BurnerException(BE_DEVICE_NOT_SET, BE_DEVICE_NOT_SET_TEXT);
	}
}

// IDeviceCallback13
void Burner::onFormatProgress(DOUBLE fPercentCompleted)
{
	if (m_pCallback)
		m_pCallback->OnFormatProgress(fPercentCompleted);
}

void Burner::onEraseProgress(DOUBLE fPercentCompleted)
{
	if (m_pCallback)
		m_pCallback->OnEraseProgress(fPercentCompleted);
}
// DataDiscCallback
void Burner::onProgress(int64_t ddwPos, int64_t ddwAll)
{
	if (m_pCallback)
		m_pCallback->OnProgress(ddwPos, ddwAll);
}

void Burner::onStatus(DataDiscStatus::Enum eStatus) 
{
	if (m_pCallback)
	{
		tstring status = GetDataDiscStatusString(eStatus);
		m_pCallback->OnStatus(status);
	}
}

void Burner::onFileStatus(int32_t fileIndex, const char_t* fileName, int32_t percentComplete) 
{
	if (m_pCallback)
	{
		tstring fileName1 = fileName;
		m_pCallback->OnFileProgress(fileIndex, fileName1, percentComplete);
	}
}

BOOL Burner::onContinueWrite() 
{
	if (m_pCallback)
		return m_pCallback->OnContinue() ? TRUE : FALSE;

	return TRUE;
}

//Protected methods
void Burner::CleanupEngine()
{
	if (NULL != m_pEngine)
	{
		m_pEngine->shutdown();
		m_pEngine->release();
	}
	m_pEngine = NULL;
}

void Burner::CleanupDeviceEnum()
{
	if (NULL != m_pEnumerator)
	{
		m_pEnumerator->release();
	}
	m_pEnumerator = NULL;
}

void Burner::CleanupDevice()
{
	if (NULL != m_pDevice)
	{
		m_pDevice->release();
	}
	m_pDevice = NULL;
}

void Burner::CleanupDataDisc()
{
	if (NULL != m_pDataDisc)
	{
		m_pDataDisc->release();
	}
	m_pDataDisc = NULL;
}

tstring Burner::get_DeviceTitle(Device* pDevice)
{
	char chLetter = pDevice->driveLetter();

	TCHAR tcsName[200] = {0};
	_stprintf(tcsName, _TEXT("(%c:) - %s"), chLetter, pDevice->description());

	return tcsName;
}

void Burner::SetPacketParameters(DataDisc* pDataDisc, const PacketBurnSettings& settings)
{
	pDataDisc->setImageType(settings.get_ImageType());

	SetVolumeProperties(pDataDisc, settings.get_VolumeLabel(), settings.get_ImageType());

	pDataDisc->setSimulateBurn(FALSE);

	// Packet mode
	pDataDisc->setWriteMethod(WriteMethod::Packet);

	// Reserve the path table
	DataWriteStrategy::Enum writeStrategy = DataWriteStrategy::None;

	switch (settings.get_Option())
	{
	case PBO_START:
		writeStrategy = DataWriteStrategy::ReserveFileTableTrack;
		break;
	case PBO_FINALIZE:
		writeStrategy = DataWriteStrategy::WriteFileTableTrack;
		break;
	}
	pDataDisc->setWriteStrategy(writeStrategy);

	pDataDisc->setCloseTrack(PBO_FINALIZE == settings.get_Option());
	pDataDisc->setCloseSession(PBO_FINALIZE == settings.get_Option());
	pDataDisc->setCloseDisc(PBO_FINALIZE == settings.get_Option());
}
int32_t Burner::GetStartAddress(Device * pDevice)
{
	assert(pDevice);

	DiscInfo *pDI = pDevice->readDiscInfo();
	if(NULL == pDI)
		return -1;

	const SessionState::Enum sessionState = pDI->sessionState();
	pDI->release();

	// Use newTrackStartAddress if the last session is open. That will give 
	// us next available address for writing in the open session.
	if (SessionState::Open == sessionState)
		return pDevice->newTrackStartAddress();

	return pDevice->newSessionStartAddress();
}

int Burner::GetLastTrackNumber(Device * pDevice, bool completeOnly)
{
	// Get the last track number from the last session if multisession option was specified
	int nLastTrack = 0;

	// Check for DVD+RW and DVD-RW RO random writable media. 
	MediaProfile::Enum mp = pDevice->mediaProfile();

	if ((MediaProfile::DVDPlusRW == mp) || (MediaProfile::DVDMinusRWRO == mp) || 
		(MediaProfile::BDRE == mp)  || (MediaProfile::BDRSrmPow == mp) || (MediaProfile::DVDRam == mp))
	{
		// DVD+RW and DVD-RW RO has only one session with one track
		if (pDevice->mediaFreeSpace() > 0)
			nLastTrack = 1;	
	}		
	else
	{
		// All other media is recorded using tracks and sessions and multi-session is no different 
		// than with the CD. 

		// Use the ReadDiskInfo method to get the last track number
		DiscInfo *pDI = pDevice->readDiscInfo();
		if(NULL != pDI)
		{
			nLastTrack = pDI->lastTrack();
			
			if (completeOnly)
			{
				// readDiskInfo reports the empty space as a track too
				// That's why we need to go back one track to get the last completed track
				if ((DiscStatus::Open == pDI->discStatus()) || (DiscStatus::Empty == pDI->discStatus()))
					nLastTrack--;
			}

			pDI->release();
		}
	}

	return nLastTrack;
}


tstring Burner::GetDataDiscStatusString(DataDiscStatus::Enum eStatus)
{
	switch(eStatus)
	{
		case DataDiscStatus::BuildingFileSystem:
			return tstring(_TEXT("Building filesystem..."));
		
		case DataDiscStatus::WritingFileSystem:
			return tstring(_TEXT("Writing filesystem..."));

		case DataDiscStatus::WritingImage:
			return tstring(_TEXT("Writing image..."));

		case DataDiscStatus::CachingSmallFiles:
			return tstring(_TEXT("Caching small files ..."));

		case DataDiscStatus::CachingNetworkFiles:
			return tstring(_TEXT("Caching network files ..."));

		case DataDiscStatus::CachingCDRomFiles:
			return tstring(_TEXT("Caching CD-ROM files ..."));

		case DataDiscStatus::Initializing:
			return tstring(_TEXT("Initializing..."));

		case DataDiscStatus::Writing:
			return tstring(_TEXT("Writing..."));

		case DataDiscStatus::WritingLeadOut:
			return tstring(_TEXT("Flushing device cache and writing lead-out..."));

		case DataDiscStatus::LoadingImageLayout:
			return tstring(_TEXT("Loading image layout from last track..."));
	}

	return tstring(_TEXT("Unknown status..."));
}

void Burner::SetVolumeProperties(DataDisc* pDataDisc, const tstring& volumeLabel, primo::burner::ImageTypeFlags::Enum imageType)
{
	// set volume times
	SYSTEMTIME st;
	GetSystemTime(&st);

	FILETIME ft;
	SystemTimeToFileTime(&st, &ft);
	
	if((ImageTypeFlags::Iso9660 & imageType) ||
		(ImageTypeFlags::Joliet & imageType))
	{
		IsoVolumeProps *iso = pDataDisc->isoVolumeProps();
		
		iso->setVolumeLabel(volumeLabel.c_str());

		// Sample settings. Replace with your own data or leave empty
		iso->setSystemID(_T("WINDOWS"));
		iso->setVolumeSet(_TEXT("SET"));
		iso->setPublisher(_T("PUBLISHER"));
		iso->setDataPreparer(_T("PREPARER"));
		iso->setApplication(_T("DVDBURNER"));
		iso->setCopyrightFile(_T("COPYRIGHT.TXT"));
		iso->setAbstractFile(_T("ABSTRACT.TXT"));
		iso->setBibliographicFile(_T("BIBLIO.TXT"));

		iso->setVolumeCreationTime(ft);
	}

	if(ImageTypeFlags::Joliet & imageType)
	{
		JolietVolumeProps *joliet = pDataDisc->jolietVolumeProps();
		
		joliet->setVolumeLabel(volumeLabel.c_str());

		// Sample settings. Replace with your own data or leave empty
		joliet->setSystemID(_T("WINDOWS"));
		joliet->setVolumeSet(_TEXT("SET"));
		joliet->setPublisher(_T("PUBLISHER"));
		joliet->setDataPreparer(_T("PREPARER"));
		joliet->setApplication(_T("DVDBURNER"));
		joliet->setCopyrightFile(_T("COPYRIGHT.TXT"));
		joliet->setAbstractFile(_T("ABSTRACT.TXT"));
		joliet->setBibliographicFile(_T("BIBLIO.TXT"));

		joliet->setVolumeCreationTime(ft);
	}

	if(ImageTypeFlags::Udf & imageType)
	{
		UdfVolumeProps *udf = pDataDisc->udfVolumeProps();
		
		udf->setVolumeLabel(volumeLabel.c_str());

		// Sample settings. Replace with your own data or leave empty
		udf->setVolumeSet(_TEXT("SET"));
		udf->setCopyrightFile(_T("COPYRIGHT.TXT"));
		udf->setAbstractFile(_T("ABSTRACT.TXT"));

		udf->setVolumeCreationTime(ft);
	}
}
