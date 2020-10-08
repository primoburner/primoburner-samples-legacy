#include "stdafx.h"

#include "Burner.h"
#include <assert.h>
#include <algorithm>
#include "FileAudioStream.h"

Burner::Burner(void)
{
	m_pEngine = NULL;
	m_pDevice = NULL;
	m_pAudio = NULL;

	set_Callback(NULL);
}

Burner::~Burner(void)
{
	Close();
}

void Burner::set_Callback(AudioCDCallback* value)
{
	m_pCallback = value;
}

bool Burner::get_IsOpen() const
{
	return (NULL != m_pEngine);
}

void Burner::CloseTray()
{
	if (NULL == m_pDevice)
		throw BurnerException(NO_DEVICE, NO_DEVICE_TEXT);

	m_pDevice->eject(0);
}

void Burner::Eject()
{
	if (NULL == m_pDevice)
		throw BurnerException(NO_DEVICE, NO_DEVICE_TEXT);

	m_pDevice->eject(1);
}

tstring get_DeviceTitle(Device* pDevice)
{
	char chLetter = pDevice->driveLetter();

	TCHAR tcsName[200] = {0};
	_stprintf(tcsName, _TEXT("(%c:) - %s"), chLetter, pDevice->description());

	return tcsName;
}

const DeviceVector& Burner::EnumerateDevices()
{
	if (!get_IsOpen())
		throw BurnerException(BURNER_NOT_OPEN, BURNER_NOT_OPEN_TEXT);

	m_deviceVector.clear();

	DeviceEnum* pDevices = m_pEngine->createDeviceEnumerator();

	int32_t nDevices = pDevices->count();
	if (0 == nDevices) 
	{
		pDevices->release();
		throw BurnerException(NO_DEVICES, NO_DEVICES_TEXT);
	}

	for (int32_t i = 0; i < nDevices; i++) 
	{
		Device* pDevice = pDevices->createDevice(i);
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

	pDevices->release();
	return m_deviceVector;
}

const SpeedVector& Burner::EnumerateWriteSpeeds()
{
	if (NULL == m_pDevice)
		throw BurnerException(NO_DEVICE, NO_DEVICE_TEXT);

	m_speedVector.clear();

	SpeedEnum* pSpeeds = m_pDevice->createWriteSpeedEnumerator();

	for (int32_t i = 0; i < pSpeeds->count(); i++) 
	{
		SpeedDescriptor* pSpeed = pSpeeds->at(i);
		if (NULL != pSpeed)
		{
			SpeedInfo speed;
			speed.TransferRateKB = pSpeed->transferRateKB();
			speed.TransferRate1xKB = (int32_t)(m_pDevice->isMediaDVD() ? Speed1xKB::DVD : Speed1xKB::CD);
			m_speedVector.push_back(speed);
		}
	}

	pSpeeds->release();
	return m_speedVector;
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
}

void Burner::Close()
{
	ReleaseAudio();
	ReleaseDevice();

	if (m_pEngine)
	{
		m_pEngine->shutdown();
		m_pEngine->release();
	}

	m_pEngine = NULL;

	Library::disableTraceLog();
}

uint32_t Burner::get_DeviceCacheSize() const
{
	if (NULL == m_pDevice)
		throw BurnerException(NO_DEVICE, NO_DEVICE_TEXT);

	return m_pDevice->internalCacheCapacity();
}

uint32_t Burner::get_DeviceCacheUsedSize() const
{
	if (NULL == m_pDevice)
		throw BurnerException(NO_DEVICE, NO_DEVICE_TEXT);

	return m_pDevice->internalCacheUsedSpace();
}

uint32_t Burner::get_WriteTransferKB() const
{
	if (NULL == m_pDevice)
		throw BurnerException(NO_DEVICE, NO_DEVICE_TEXT);

	return m_pDevice->writeTransferRate();
}

void Burner::SelectDevice(uint32_t deviceIndex, bool exclusive)
{
	if (NULL != m_pDevice)
		throw BurnerException(DEVICE_ALREADY_SELECTED, DEVICE_ALREADY_SELECTED_TEXT);

	DeviceEnum* pDevices = m_pEngine->createDeviceEnumerator();
	Device* pDevice = pDevices->createDevice(deviceIndex, exclusive ? 1 : 0);
	if (NULL == pDevice)
	{
		pDevices->release();
		throw BurnerException(INVALID_DEVICE_INDEX, INVALID_DEVICE_INDEX_TEXT);
	}

	m_pDevice = pDevice;
	pDevices->release();
}

void Burner::ReleaseDevice()
{
	if (NULL != m_pDevice)
		m_pDevice->release();

	m_pDevice = NULL;
}

void Burner::ReleaseAudio()
{
	if (NULL != m_pAudio)
		m_pAudio->release();

	m_pAudio = NULL;
}

uint32_t Burner::get_MediaFreeSpace() const
{
	if (NULL == m_pDevice)
		throw BurnerException(NO_DEVICE, NO_DEVICE_TEXT);

	return m_pDevice->mediaFreeSpace();
}

bool Burner::get_MediaIsBlank() const
{
	if (NULL == m_pDevice)
		throw BurnerException(NO_DEVICE, NO_DEVICE_TEXT);

	return 1 == m_pDevice->isMediaBlank();
}

int32_t Burner::get_MaxWriteSpeedKB() const
{
	if (NULL == m_pDevice)
		throw BurnerException(NO_DEVICE, NO_DEVICE_TEXT);

	return m_pDevice->maxWriteSpeedKB();
}

bool Burner::get_CDTextSupport() const
{
	if (NULL == m_pDevice)
		throw BurnerException(NO_DEVICE, NO_DEVICE_TEXT);

	return 1 == m_pDevice->cdFeatures()->canReadCDText();
}

bool Burner::get_ReWritePossible() const
{
	if (NULL == m_pDevice)
		throw BurnerException(NO_DEVICE, NO_DEVICE_TEXT);

	return isRewritePossible(m_pDevice);
}

bool Burner::get_MediaIsReWritable() const
{
	if (NULL == m_pDevice)
		throw BurnerException(NO_DEVICE, NO_DEVICE_TEXT);

	return 1 == m_pDevice->isMediaRewritable();
}

bool Burner::get_SaoPossible() const
{
	if (NULL == m_pDevice)
		throw BurnerException(NO_DEVICE, NO_DEVICE_TEXT);

	return 1 == m_pDevice->cdFeatures()->canWriteSao();
}

bool Burner::get_TaoPossible() const
{
	if (NULL == m_pDevice)
		throw BurnerException(NO_DEVICE, NO_DEVICE_TEXT);

	return 1 == m_pDevice->cdFeatures()->canWriteTao();
}

void Burner::Erase(const EraseSettings& settings) 
{
	if (NULL == m_pDevice)
		throw BurnerException(NO_DEVICE, NO_DEVICE_TEXT);

	MediaProfile::Enum mp = m_pDevice->mediaProfile();

	if ((MediaProfile::DVDMinusRWSeq != mp) && (MediaProfile::DVDMinusRWRO != mp) && (MediaProfile::CDRW != mp))
		throw BurnerException(ERASE_NOT_SUPPORTED, ERASE_NOT_SUPPORTED_TEXT);
	
	if (m_pDevice->isMediaBlank() && !settings.Force)
		return;
	
	m_pDevice->erase(settings.Quick ? EraseType::Minimal : EraseType::Disc);

	// Refresh to reload disc information
	m_pDevice->refresh();
}

void Burner::SetCDText(CDText* pCDText, const CDTextEntry &cdTextEntry, int nItem)
{
	assert(NULL != pCDText);
	pCDText->setItem(CDTextType::Title, nItem, cdTextEntry.Title.c_str());
	pCDText->setItem(CDTextType::Performer, nItem, cdTextEntry.Performer.c_str());
	pCDText->setItem(CDTextType::Songwriter, nItem, cdTextEntry.SongWriter.c_str());
	pCDText->setItem(CDTextType::Composer, nItem, cdTextEntry.Composer.c_str());
	pCDText->setItem(CDTextType::Arranger, nItem, cdTextEntry.Arranger.c_str());
	pCDText->setItem(CDTextType::Message, nItem, cdTextEntry.Message.c_str());

	if (0 == nItem) // album
	{
		pCDText->setItem(CDTextType::DiscId, nItem, cdTextEntry.DiskId.c_str());
		pCDText->setItem(CDTextType::Genre, nItem, cdTextEntry.Genre.c_str());
		pCDText->setItem(CDTextType::GenreText, nItem, cdTextEntry.GenreText.c_str());
	}

	pCDText->setItem(CDTextType::UpcIsrc, nItem, cdTextEntry.UpcIsrc.c_str());
}

void Burner::Burn(const BurnSettings& settings)
{
	BurnInternal(settings);
}

void Burner::BurnInternal(const BurnSettings& settings)
{
	if (NULL == m_pDevice)
		throw BurnerException(NO_DEVICE, NO_DEVICE_TEXT);

	m_pAudio = Library::createAudioCD();
	
	m_pAudio->setDevice(m_pDevice);

	if (settings.DecodeInTempFiles)
		m_pAudio->setAudioDecodingMethod(AudioDecodingMethod::Tempfile);
	else
		m_pAudio->setAudioDecodingMethod(AudioDecodingMethod::Memory);

	if (0 == settings.Files.size()) 
	{
		throw BurnerException(NO_AUDIO_TRACKS, NO_AUDIO_TRACKS_TEXT);
	}
	
	AudioInputList* audioInputs = m_pAudio->audioInputs();

	typedef std::list<CFileStream*> streamlist_t;
	struct cleanup_t
	{
		streamlist_t streams;
		~cleanup_t()
		{
			for (streamlist_t::iterator it = streams.begin(); it != streams.end(); it++)
			{
				if (*it) {  delete *it; }
			}
		}
	} cleanup;

	for(size_t i = 0; i < settings.Files.size(); i++)
	{
				
		AudioInput* pai = Library::createAudioInput();

		if (settings.UseAudioStream)
		{
			CFileStream* fileStream = new CFileStream(settings.Files[i].c_str());
			cleanup.streams.push_back(fileStream);
			pai->setStream(fileStream);
			pai->setStorageType(AudioStorage::Stream);
		}
		else
		{
			pai->setFilePath(settings.Files[i].c_str());
		}
		audioInputs->add(pai);
		pai->release(); // release the object created by the Library
	}

	bool willCreateHiddenTrack = false;

	// The following code will setup the IAudioCD object to write a hidden track using the entire data from the first audio
	// input as the hidden content.
	if (settings.CreateHiddenTrack)
	{
		// Until this moment on the target medium there would be an audio track for each audio input loaded in the IAudioCD
		// object. To create a hidden track first of all the ICDSession describing the current audio CD  will be retrieved.
		CDSession *pSession = m_pAudio->createCDSession();
	
		// Since the first audio input will be set in the hidden track it is necessary to ensure that there is at least one
		// other audio input
		if (1 < pSession->tracks()->count())
		{
			willCreateHiddenTrack = true;
		
			// A hidden track contains the sectors from the start of the audio session (0) till the start of the first track
			// in that session (ICDTrack::SetStart). When these are equal there is no hidden track. Now the first track in
			// the ICDSession object will be removed - thus the second track will become first track for the AudioCD.
			pSession->tracks()->remove(0);
			
			// The pregap start of the new first track must be set to 0 (the pregap start value of the original first track)
			// thus expanding the hidden section to the size fo the original first track
			pSession->tracks()->at(0)->setPregapStart(-150);
			
			// After that all that is needed is to send the reconstructed ICDSession to the IAudioCD object
			m_pAudio->setCDSession(pSession);
		}

		pSession->release();
	}

	// Setup CDText Properties
	if (m_pDevice->cdFeatures()->canReadCDText() && settings.WriteCDText)
	{
		CDText* pCDText = Library::createCDText();
		SetCDText(pCDText, settings.CDText.Album, 0);

		int cdTextItemN = 0;
		for (size_t i = 0; i < stl::min((size_t)99, settings.Files.size()); i++)
		{
			//If hidden track will be written on the target medium then the CD text might neeed to be updated - in this case,
			// since the entire first audio input will become the hidden track its CD text entry is ignored
			if (willCreateHiddenTrack && 0 == i)
			{
				continue;
			}

			SetCDText(pCDText, settings.CDText.Songs[i], cdTextItemN + 1);
			cdTextItemN++;
		}

		m_pAudio->setCDText(pCDText);
		pCDText->release(); // release the object created by the Library
	}

	m_pDevice->setWriteSpeedKB(settings.WriteSpeedKB);
	m_pAudio->setSimulateBurn(settings.Simulate);
	m_pAudio->setCloseDisc(settings.CloseDisc);
	
	if (!m_pAudio->setWriteMethod(settings.WriteMethod))
	{
		throw BurnerException(AUDIOCD_ERROR, _TEXT("Invalid recording mode"));
	}

	m_pAudio->setCallback(m_pCallback);

	bool_t res = m_pAudio->writeToCD();
		
	CheckSuccess(m_pAudio);

	if (!res)
	{
		throw BurnerException(AUDIOCD_ERROR, _TEXT("CD Recording failed."));
	}
			
	m_pDevice->eject(settings.Eject);
}


tstring Burner::TranslateAudioCDStatus(AudioCDStatus::Enum eStatus)
{
	tstring str;
	switch (eStatus) 
	{
		case AudioCDStatus::Initializing:
			str = _T("Initializing...");
			break;
		case AudioCDStatus::InitializingDevice:
			str = _T("Initializing device...");
			break;
		case AudioCDStatus::DecodingAudio:
			str = _T("Decoding audio...");
			break;
		case AudioCDStatus::Writing:
			str = _T("Writing...");
			break;
		case AudioCDStatus::WritingLeadOut:
			str = _T("Writing lead-out...");
			break;
			
		default:
			str = _T("unexpected status");
			break;
	}

	return str;
}

void Burner::CheckSuccess(AudioCD* audioCD)
{
	if(audioCD->error()->facility() != ErrorFacility::Success)
	{
		throw BurnerException(audioCD);
	}
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

bool Burner::isRewritePossible(Device *device) const
{
	CDFeatures *cdfeatures = device->cdFeatures();
	DVDFeatures *dvdfeatures = device->dvdFeatures();
	BDFeatures *bdfeatures = device->bdFeatures();

	bool cdRewritePossible = cdfeatures->canWriteCDRW();
	bool dvdRewritePossible = dvdfeatures->canWriteDVDMinusRW() || dvdfeatures->canWriteDVDPlusRW() ||
		dvdfeatures->canWriteDVDRam();
	bool bdRewritePossible = bdfeatures->canWriteBDRE();
	return cdRewritePossible || dvdRewritePossible || bdRewritePossible;
}
