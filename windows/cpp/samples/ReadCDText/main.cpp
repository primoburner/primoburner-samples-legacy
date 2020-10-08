// ReadCDText.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
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


int _tmain(int argc, _TCHAR* argv[])
{
	Library::enableTraceLog(NULL, TRUE);

	int i, ch;
	int nDevice;
	Device * pDevice;

	Engine *pEngine = Library::createEngine();
	pEngine->initialize();

	DeviceEnum* pEnum = pEngine->createDeviceEnumerator();
	int nCount=pEnum->count();
	if (nCount==0) 
	{
		_tprintf(_T("No devices available\n"));
		PrintError(pEnum->error());
		return -1;
	}

___again:
	_tprintf(_T("Select device to read CD Text from :\n"));
	for (i = 0; i < nCount; i++) 
	{
		pDevice = pEnum->createDevice(i);
		if(pDevice)
		{
			_tprintf(_T("   %d. %s(%c:\\):\n"), i+1, pDevice->description(), pDevice->driveLetter());
			pDevice->release();
		}
	}

	ch =_getch();
	nDevice = ch - _T('0');

	if (nDevice < 1 || nDevice > nCount)
		goto ___again;

	pDevice = pEnum->createDevice(nDevice - 1);
	if (pDevice)
	{
		// Display the CD Text information	
		CDText * pCdt = pDevice->readCDText();
		
		if (pCdt)
		{
			// Track 0 contains the album information 
			_tprintf(_T("\nAlbum \n"));
			_tprintf(_T("------------------------------------\n"));
			_tprintf(_T("Name: %s\n"), pCdt->item(CDTextType::Title, 0));
			_tprintf(_T("Performer: %s\n"), pCdt->item(CDTextType::Performer, 0));
			_tprintf(_T("Song Writer: %s\n"), pCdt->item(CDTextType::Songwriter, 0));
			_tprintf(_T("Composer: %s\n"), pCdt->item(CDTextType::Composer, 0));
			_tprintf(_T("Arranger: %s\n"), pCdt->item(CDTextType::Arranger, 0));
			_tprintf(_T("Message: %s\n"), pCdt->item(CDTextType::Message, 0));
			_tprintf(_T("Disk ID: %s\n"), pCdt->item(CDTextType::DiscId, 0));
			_tprintf(_T("Genre: %s\n"), pCdt->item(CDTextType::Genre, 0));
			_tprintf(_T("Genre Text: %s\n"), pCdt->item(CDTextType::GenreText, 0));
			_tprintf(_T("UPC/EAN: %s\n"), pCdt->item(CDTextType::UpcIsrc, 0));

			_tprintf(_T("------------------------------------\n"));
			_tprintf(_T("Tracks \n"));
			_tprintf(_T("------------------------------------\n"));

			for (i = 1; i < pCdt->count(); i++) 
			{
				_tprintf(_T("\nTrack %0d\n\n"), i);
				_tprintf(_T("Title: %s\n"),		pCdt->item(CDTextType::Title, i));
				_tprintf(_T("Performer: %s\n"),	pCdt->item(CDTextType::Performer, i));
				_tprintf(_T("Song Writer: %s\n"), pCdt->item(CDTextType::Songwriter, i));
				_tprintf(_T("Composer: %s\n"),	pCdt->item(CDTextType::Composer, i));
				_tprintf(_T("Arranger: %s\n"),	pCdt->item(CDTextType::Arranger, i));
				_tprintf(_T("Message: %s\n"),		pCdt->item(CDTextType::Message, i));
				_tprintf(_T("ISRC: %s\n"),		pCdt->item(CDTextType::UpcIsrc, i));
				_tprintf(_T("------------------------------------\n"));
			}

			pCdt->release();
		}
		else
		{
			_tprintf(_T("\nNo CD-Text on the disk or CD-Text unaware drive.\n"));
			PrintError(pDevice->error());
		}
	}
	else
	{
		PrintError(pEnum->error());
	}

	pDevice->release();
	pEnum->release();

	pEngine->shutdown();
	pEngine->release();

	_tprintf(_T("Press any key.\n"));
	ch=_getch();

	Library::disableTraceLog();

	return 0;
}
