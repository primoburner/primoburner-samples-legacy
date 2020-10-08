#define _BIND_TO_CURRENT_CRT_VERSION 1

#include <tchar.h>
#include <conio.h>
#include <stdio.h>

#include "PrimoBurner.h"
using namespace primo::burner;


void PrintError(const ErrorInfo *pErrInfo);

void main() 
{
	int ch;
	int nDevice;
	Device* pDevice;

	// Enable trace log
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
			pDevice = (Device*)pEnum->createDevice(i);

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

	pDevice = (Device*) pEnum->createDevice(nDevice);
	if (0 != pDevice)
	{
		SessionInfo *pSI  = pDevice->readSessionInfo();
		if(NULL != pSI)
		{
			for (int i = pSI->firstSession(); i <= pSI->lastSession(); i++) 
			{
				_tprintf(_T("\nSession: %d\n\n"), i);

				Toc *pToc = pDevice->readTocFromSession(i);
				if(NULL != pToc)
				{
					int j, nIndex, nAddr;
					
					for (j = pToc->firstTrack(); j <= pToc->lastTrack(); j++) 
					{
						nIndex = j - pToc->firstTrack();
						TocTrack *pTrack = pToc->tracks()->at(nIndex);

						nAddr = pTrack->address();

						if (pTrack->isData())
							_tprintf(_T("\t%02d Data  LBA: %05d. Time: (%02d:%02d)\n"), pTrack->trackNumber(), nAddr, nAddr / 4500, (nAddr % 4500) / 75);
						else
							_tprintf(_T("\t%02d Audio LBA: %05d. Time: (%02d:%02d)\n"), pTrack->trackNumber(), nAddr, nAddr / 4500, (nAddr % 4500) / 75);
					}

					nIndex = pToc->lastTrack() - pToc->firstTrack() + 1;
					nAddr = pToc->tracks()->at(nIndex)->address();
					_tprintf(_T("\tLead-out LBA: %05d. Time: (%02d:%02d)\n"), nAddr, nAddr / 4500, (nAddr % 4500) / 75);

					pToc->release();
				}
				else
				{
					_tprintf(_T("Could not read track info for session: %d.\n"), i);
				}
			}

			pSI->release();
		}
		else
		{
			_tprintf(_T("Could not read session info."));
			PrintError(pDevice->error());
		}

		_tprintf(_T("Press any key...\n"));
		ch = _getch();

		pDevice->release();
	}
	else
	{
		PrintError(pEnum->error());
	}

	// Disable trace log
	Library::disableTraceLog();

	pEnum->release();

	pEngine->shutdown();
	pEngine->release();
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
