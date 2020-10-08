// ReadSubChannel.cpp : Defines the entry point for the console application.
//
#include "stdafx.h"

#include "PrimoBurner.h"
using namespace primo::burner;

class CDeviceCallback : public DeviceCallback
{
	void onRead(int32_t lBegin, int32_t lEnd, int32_t lCurrent, uint32_t dwBlocks)
	{
		// simple progress implementation
		int nPercentage = (int) ((double) (lCurrent - lBegin) * 100 / (double) (lEnd - lBegin)); 
		nPercentage /= 2;

		_tprintf(_T("\r"));
		while (nPercentage-- > 0)
			_tprintf(_T("|")); 
	}

	BOOL onContinueReading()
	{
		return TRUE;
	}
};

LPCTSTR TypeToMode(TrackType::Enum tt)
{
	switch(tt)
	{
		/** Audio Track */
		case TrackType::Audio:
			return _T("Audio");

		/** Mode 0 Track */
		case TrackType::Mode0: 
			return _T("Mode0");

		/** Mode 1, Data Track */
		case TrackType::Mode1:
			return _T("Mode1");
		
		/** Mode 2 Formless */
		case TrackType::Mode2Formless: 
			return _T("Mode2");

		/** Mode 2, Form 1 Data Track */
		case TrackType::Mode2Form1: 
			return _T("Mode2 Form1");

		/** Mode 2, Form 2 Data Track */
		case TrackType::Mode2Form2:
			return _T("Mode2 Form2");

		/** Mode 2, Mixed Form Data Track */
		case TrackType::Mode2Mixed:
			return _T("Mode2 Mixed");
	}

	return _T("");
}

void PrintError(const ErrorInfo *pErrInfo)
{
	switch(pErrInfo->facility())
	{
	case ErrorFacility::SystemWindows:
		{
			
			TCHAR tcsErrorMessage[1024];
			::FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM, NULL, pErrInfo->code(),
	 				MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), tcsErrorMessage, 1024, NULL);
			_tprintf(_T("System Error: 0x%06x - %s\n"), pErrInfo->code(), tcsErrorMessage);
		}
		break;

	case ErrorFacility::Device:
		_tprintf(_T("Device Error: 0x%06x - %s \n"), pErrInfo->code(), pErrInfo->message());
		break;

	case ErrorFacility::DeviceEnumerator:
		_tprintf(_T("DeviceEnumerator Error: 0x%06x - %s \n"), pErrInfo->code(), pErrInfo->message());
		break;

	default:
		_tprintf(_T("Error Facility: 0x%06x   Code: 0x%06x - %s \n"), pErrInfo->facility(), pErrInfo->code(), pErrInfo->message());
		break;
	}
}

void _tmain(int argc, _TCHAR* argv[])
{
	int ch;
	int nDevice;
	Device* pDevice;

	Library::enableTraceLog(NULL, TRUE);
	Engine *pEngine = Library::createEngine();
	pEngine->initialize();

	DeviceEnum* pEnum = pEngine->createDeviceEnumerator();
	int nCount = pEnum->count();
	if (0 == nCount) 
	{
		_tprintf(_T("No devices available\n"));
		PrintError(pEnum->error());
		return;
	}

	do
	{
		_tprintf(_T("Select a device:\n"));
		for (int i = 0; i < nCount; i++) 
		{
			pDevice = pEnum->createDevice(i);
			if (0 != pDevice)
			{
				_tprintf(_T("  (%C:\\) %s:\n"), pDevice->driveLetter(), pDevice->description());
				pDevice->release();
			}
		}

		_tprintf(_T("\nEnter drive letter:\n"));
		ch = _getch();

		nDevice = Library::driveLetterToDeviceIndex((char)ch);
	} 
	while (nDevice < 0 || nDevice > nCount - 1);

	// Get device
	pDevice = pEnum->createDevice(nDevice);
	if (0 == pDevice)
	{
		PrintError(pEnum->error());

		pEnum->release();
		pEngine->shutdown();
		pEngine->release();
		return;
	}

	_tprintf(_T("\nScanning first session.\nThis could take a few minutes ...\n"));
	_tprintf(_T("0%%                       50%%                  100%%\n"));
	
	// Set callback to capture progress
	CDeviceCallback callback;
	pDevice->setCallback(&callback);

	// select max read speed; 
	pDevice->setReadSpeedKB(pDevice->maxReadSpeedKB());

	// read the first session layout
	CDSession * ps = pDevice->readCDSessionLayout(1, TRUE, TRUE, 150);
	if (ps)
	{
		_tprintf(_T("\n\n"));
		_tprintf(_T("MCN (UPC): %S\n"), ps->mcn());

		// list tracks
		for (int nTrack = 0; nTrack < ps->tracks()->count(); nTrack++)
		{
			CDTrack* pte = ps->tracks()->at(nTrack);
			if (!pte)
				continue;

			// track
			_tprintf(_T("\nTNO: %02d, %11s ISRC: %S PS: %06d TS: %06d TE: %06d PE: %06d\n"), 
 					nTrack + 1, TypeToMode(pte->type()), pte->isrc(), pte->pregapStart(), pte->start(), pte->end(), pte->postgapEnd());

			// indices
			if (pte->indexes()->count() > 0)
			{
				for (int nIndex = 0; nIndex < pte->indexes()->count(); nIndex++)
					_tprintf(_T("Index: %02d, Position: %06d\n"), nIndex + 2, pte->indexes()->at(nIndex));
			}

			// Sub Types
			if (pte->modes()->count() > 0)
			{
				for (int nSubTypeIndex = 0; nSubTypeIndex < pte->modes()->count(); nSubTypeIndex++)
				{
					CDMode* ptti = pte->modes()->at(nSubTypeIndex);
						_tprintf(_T("Subtype: %11s, Position: %06d\n"), TypeToMode(ptti->type()), ptti->pos());
				}	
			}
		}

		ps->release();

		_tprintf(_T("Press any key...\n"));
		ch = _getch();
	}
	else
	{
		PrintError(pDevice->error());
	}

	pDevice->release();
	pEnum->release();

	pEngine->shutdown();
	pEngine->release();
	Library::disableTraceLog();

	return;
}
