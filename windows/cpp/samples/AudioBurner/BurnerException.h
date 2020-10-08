#pragma once

#include "PrimoBurner.h"


#define ENGINE_INITIALIZATION			((uint32_t)-1)
#define ENGINE_INITIALIZATION_TEXT		_TEXT("PrimoBurner engine initialization error.")

#define BURNER_NOT_OPEN					((uint32_t)-2)
#define BURNER_NOT_OPEN_TEXT			_TEXT("Burner not open.")

#define NO_DEVICES						((uint32_t)-3)
#define NO_DEVICES_TEXT					_TEXT("No CD/DVD/BD devices are available.")

#define NO_DEVICE						((uint32_t)-4)
#define NO_DEVICE_TEXT					_TEXT("No device selected.")

// User Errors

#define DEVICE_ALREADY_SELECTED			((uint32_t)-5)
#define DEVICE_ALREADY_SELECTED_TEXT	_TEXT("Device already selected.") 

#define INVALID_DEVICE_INDEX			((uint32_t)-6)
#define INVALID_DEVICE_INDEX_TEXT		_TEXT("Invalid device index.")

#define ERASE_NOT_SUPPORTED				((uint32_t)-7)
#define ERASE_NOT_SUPPORTED_TEXT		_TEXT("Erasing is supported only for CD-RW and DVD-RW media.")

#define FORMAT_NOT_SUPPORTED			((uint32_t)-8)
#define FORMAT_NOT_SUPPORTED_TEXT		_TEXT("Format is supported only for DVD-RW and DVD+RW media.")

#define FILE_NOT_FOUND					((uint32_t)-9)
#define FILE_NOT_FOUND_TEXT				_TEXT("File not found while processing source folder.")

#define NO_WRITER_DEVICES				((uint32_t)-10)
#define NO_WRITER_DEVICES_TEXT			_TEXT("No CD/DVD/BD writers are available.")

#define NO_AUDIO_TRACKS					((uint32_t)-11)
#define NO_AUDIO_TRACKS_TEXT			_TEXT("No Audio tracks detected.")

#define PLAYLIST_TOO_BIG				((uint32_t)-12)
#define PLAYLIST_TOO_BIG_TEXT			_TEXT("The playlist size is too big to fit it on the CD.")

#define INITIALIZE_DEVICE_FAILED		((uint32_t)-13)
#define INITIALIZE_DEVICE_FAILED_TEXT	_TEXT("Unable to initialize device.")

#define BUFFER_UNDERRUN_DETECTED		((uint32_t)-14)
#define BUFFER_UNDERRUN_DETECTED_TEXT	_TEXT("Buffer underrun detected! The CD media was damaged.")

#define LOAD_PLUGINS_ERROR				((uint32_t)-15)
#define LOAD_PLUGINS_ERROR_TEXT			_TEXT("One or more of the default plugins cannot be loaded.")

// Special Errors
#define DEVICE_ERROR					((uint32_t)-100)
#define DATADISC_ERROR					((uint32_t)-200)
#define VIDEODVD_ERROR					((uint32_t)-300)
#define AUDIOCD_ERROR					((uint32_t)-400)


class BurnerException
{
public:
	BurnerException()
	{
		m_error			= 0;
		m_errorInfo = NULL;
	}

	~BurnerException()
	{
		if(NULL != m_errorInfo)
		{
			m_errorInfo->release();
			m_errorInfo = NULL;
		}
	}

	BurnerException(const BurnerException& other)
	{
		m_message		= other.m_message;
		m_error			= other.m_error;

		if(other.m_errorInfo)
		{
			m_errorInfo	= other.m_errorInfo->clone();
		}
		else
		{
			m_errorInfo	= NULL;
		}
	}

	BurnerException(const uint32_t error, const tstring message)
	{
		m_error = error;
		m_message = message;
		m_errorInfo = NULL;
	}

	BurnerException(Device *pDevice)
	{
		m_errorInfo	= NULL;

		if (NULL != pDevice)
		{
			m_error = DEVICE_ERROR;
			m_errorInfo = pDevice->error()->clone();
			m_message = BuildErrorMessage(m_errorInfo);
		}
	}

	BurnerException(AudioCD* pAudioCD)
	{
		m_errorInfo	= NULL;

		if (NULL != pAudioCD)
		{
			m_error = AUDIOCD_ERROR;
			m_errorInfo = pAudioCD->error()->clone();
			m_message = BuildErrorMessage(m_errorInfo);
		}
	}

	BurnerException & operator= (const BurnerException & other)
	{
		if (this == &other) 
			return *this;

		if(NULL != m_errorInfo)
		{
			m_errorInfo->release();
			m_errorInfo = NULL;
		}

		m_message		= other.m_message;
		m_error			= other.m_error;

		if(other.m_errorInfo)
		{
			m_errorInfo	= other.m_errorInfo->clone();
		}

		return *this;
	}

	const tstring& get_Message() const
	{
		return m_message;
	}

	const uint32_t get_Error() const
	{
		return m_error;
	}

protected:

	tstring m_message;

	uint32_t m_error;
	const primo::burner::ErrorInfo *m_errorInfo;

protected:
	tstring BuildSystemErrorMessage(uint32_t systemError)
	{
		TCHAR tcsErrorMessage[1024];
		::FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM, NULL, systemError,
	 		MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), tcsErrorMessage, 1024, NULL);
		return tcsErrorMessage;
	}

	tstring BuildErrorMessage(const primo::burner::ErrorInfo *errorInfo)
	{
		tstring errMsg;
		const int bufSize = 1024;
		TCHAR tcsErrorMessage[bufSize];

		switch(errorInfo->facility())
		{
		case ErrorFacility::SystemWindows:
			{
				errMsg = BuildSystemErrorMessage(errorInfo->code());
			}
			break;

		case ErrorFacility::Device:
			_sntprintf(tcsErrorMessage, bufSize, _T("Device Error: 0x%06x - %s"), errorInfo->code(), errorInfo->message());
			errMsg = tcsErrorMessage;
			break;

		case ErrorFacility::DeviceEnumerator:
			_sntprintf(tcsErrorMessage, bufSize, _T("DeviceEnumerator Error: 0x%06x - %s"), errorInfo->code(), errorInfo->message());
			errMsg = tcsErrorMessage;
			break;

		case ErrorFacility::AudioCD:
			_sntprintf(tcsErrorMessage, bufSize, _T("AudioCD Error: 0x%06x - %s"), errorInfo->code(), errorInfo->message());
			errMsg = tcsErrorMessage;
			break;

		default:
			_sntprintf(tcsErrorMessage, bufSize, _T("Error Facility: 0x%06x   Code: 0x%06x - %s"), errorInfo->facility(), errorInfo->code(), errorInfo->message());
			errMsg = tcsErrorMessage;
			break;
		}

		return errMsg;
	}
};
