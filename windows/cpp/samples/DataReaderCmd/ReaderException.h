#pragma once

#define RE_NO_DEVICES_TEXT				_T("No device were found on the machine")
#define RE_READER_NOT_OPEN_TEXT			_T("The Reader object is not initialized")
#define RE_DEVICE_NOT_SET_TEXT			_T("No device is selected")
#define RE_ITEM_NOT_FOUND_TEXT			_T("No such file or folder exists at the specified location")
#define RE_ITEM_NOT_FOLDER_TEXT			_T("The specified item is not a folder")
#define RE_ITEM_NOT_FILE_TEXT			_T("The specified item is not a file")
#define RE_NO_TRACKS_ON_DISC_TEXT		_T("No tracks found on the medium")
#define RE_TRACK_LRA_INVALID_TEXT		_T("Track's last recorded address is not valid")
#define RE_TRACK_IS_AUDIO_TEXT			_T("The specified track is an audio track - such tracks are not read from")

enum EReaderError
{
	RE_READER_NOT_OPEN = 0,
	RE_DEVICE_NOT_SET = 1,
	RE_NO_DEVICES = 2,
	RE_ITEM_NOT_FOUND = 3,
	RE_ITEM_NOT_FOLDER = 4,
	RE_ITEM_NOT_FILE = 5,
	RE_NO_TRACKS_ON_DISC = 6,
	RE_TRACK_LRA_INVALID = 7,
	RE_TRACK_IS_AUDIO = 8,

	RE_PRIMO_BURNER_ERROR = 10000, // see the primo::burner::ErrorInfo property for the error details
};


class ReaderException
{
public:
	ReaderException()
	{
		m_error			= 0;
		m_errorInfo = NULL;
	}

	~ReaderException()
	{
		if(NULL != m_errorInfo)
		{
			m_errorInfo->release();
			m_errorInfo = NULL;
		}
	}

	ReaderException(const ReaderException& other)
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

	ReaderException(const uint32_t error, const tstring message)
	{
		m_error = error;
		m_message = message;
	}

	ReaderException(Device *pDevice)
	{
		if (NULL != pDevice)
		{
			m_error = RE_PRIMO_BURNER_ERROR;
			m_errorInfo = pDevice->error()->clone();
			m_message = BuildErrorMessage(m_errorInfo);
		}
	}

	ReaderException(DeviceEnum *pDeviceEnum)
	{
		if (NULL != pDeviceEnum)
		{
			m_error = RE_PRIMO_BURNER_ERROR;
			m_errorInfo = pDeviceEnum->error()->clone();
			m_message = BuildErrorMessage(m_errorInfo);
		}
	}

	ReaderException(Engine *pEngine)
	{
		if (NULL != pEngine)
		{
			m_error = RE_PRIMO_BURNER_ERROR;
			m_errorInfo = pEngine->error()->clone();
			m_message = BuildErrorMessage(m_errorInfo);
		}
	}

	ReaderException(DataDisc* pDataDisc)
	{
		if (NULL != pDataDisc)
		{
			m_error = RE_PRIMO_BURNER_ERROR;
			m_errorInfo = pDataDisc->error()->clone();
			m_message = BuildErrorMessage(m_errorInfo);
		}
	}

	ReaderException & operator= (const ReaderException & other)
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

		case ErrorFacility::DataDisc:
			_sntprintf(tcsErrorMessage, bufSize, _T("DataDisc Error: 0x%06x - %s"), errorInfo->code(), errorInfo->message());
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
