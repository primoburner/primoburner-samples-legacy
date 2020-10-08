#define _CRT_NON_CONFORMING_SWPRINTFS
#define _CRT_SECURE_NO_WARNINGS
#define _CRT_SECURE_CPP_OVERLOAD_SECURE_NAMES 1

#define _BIND_TO_CURRENT_CRT_VERSION 1

#include <tchar.h>
#include <conio.h>
#include <stdio.h>

#include "PrimoBurner.h"
using namespace primo::burner;


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

void _tmain() 
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

		pEnum->release();
		pEngine->shutdown();
		pEngine->release();

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
				_tprintf(_T("  (%c:\\) %s:\n"), pDevice->driveLetter(), pDevice->description());
				pDevice->release();
			}
		}

		_tprintf(_T("\nEnter drive letter:\n"));
		ch = _getch();

		nDevice = Library::driveLetterToDeviceIndex((char)ch);
	} 
	while (nDevice < 0 || nDevice > nCount - 1);

	pDevice = pEnum->createDevice(nDevice);
	if (0 != pDevice)
	{
		// Test for medium 
		if (MediaReady::NotPresent == pDevice->mediaState())
		{
			_tprintf(_T("Medium not present.\n"));
		}
		else
		{
			// Lock the device
			if (pDevice->lockMedia(TRUE))
			{
				_tprintf(_T("Media locked.\n"));
				_tprintf(_T("Press any key to unlock the medium.\n"));
				ch = _getch();
				
				// Unlock the device
				if (pDevice->lockMedia(FALSE))
					_tprintf(_T("Media unlocked.\n"));
				else
					PrintError(pDevice->error());
			}
			else
				PrintError(pDevice->error());

			// Print available blocks
			_tprintf(_T("%d blocks available on the disc.\n"), pDevice->mediaFreeSpace());

			// Eject 
			_tprintf(_T("Press any key to eject the device.\n"));
			ch = _getch();
			if (!pDevice->eject(TRUE))
				PrintError(pDevice->error());

			// Close tray
			_tprintf(_T("Insert a blank disc. Press any key to close the device tray.\n"));
			ch = _getch();
			if (!pDevice->eject(FALSE))
				PrintError(pDevice->error());

			// Refresh status 
			pDevice->refresh();

			// Print available blocks again.
			_tprintf(_T("%d blocks available on the disc.\n"), pDevice->mediaFreeSpace());
		}

		// Exit
		_tprintf(_T("Press any key to exit.\n"));
		ch = _getch();

		pDevice->release();
	}
	else
	{
		PrintError(pEnum->error());
	}

	pEnum->release();

	pEngine->shutdown();
	pEngine->release();
	Library::disableTraceLog();
}
