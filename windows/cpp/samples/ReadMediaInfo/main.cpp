#define _CRT_NON_CONFORMING_SWPRINTFS
#define _CRT_SECURE_NO_WARNINGS
#define _CRT_SECURE_CPP_OVERLOAD_SECURE_NAMES 1

#define _BIND_TO_CURRENT_CRT_VERSION 1

#include <tchar.h>
#include <conio.h>
#include <stdio.h>

#include "PrimoBurner.h"
using namespace primo::burner;

Device * SelectDevice(Engine *pEngine);
int GetLastCompleteTrack(Device * pDevice);

void PrintError(const ErrorInfo *pErrInfo);

void DisplayBDMediaInfo(MediaInfo* pMI);
void DisplayDVDMediaInfo(MediaInfo* pMI);
void DisplayDVDPlusMediaInfo(DVDPlusMediaInfo* pMI);
void DisplayDVDMinusMediaInfo(DVDMinusMediaInfo* pMI);

void main() 
{
	// Enable trace log
	Library::enableTraceLog(NULL, TRUE);

	//////////////////////////////////////////////////////////////////////////////////////////
	// 1) Create an engine object

	// Create an instance of IEngine. That is the main iterface that can be used to enumerate 
	// all devices in the system
	Engine *pEngine = Library::createEngine();

	///////////////////////////////////////////////////////////////////////////////////////////
	// 2) Inititialize the engine object
	pEngine->initialize();

	///////////////////////////////////////////////////////////////////////////////////////////
	// 3) Select a device (CD/DVD-RW drive)
	
	// The SelectDevice function lets the user select a device and then returns an instance of IDevice.
	Device* pDevice = SelectDevice(pEngine);
	if (!pDevice)
	{
		// Something went wrong. Shutdown the engine.
		pEngine->shutdown();

		// Release the engine instance. That will free any allocated resources
		pEngine->release();

		return;
	}

	///////////////////////////////////////////////////////////////////////////////////////////
	// 4) Print media information
	_tprintf(_T("\nMedia Information\n"));
	_tprintf(_T("\n Blank : %s"), pDevice->isMediaBlank() ? _T("yes") : _T("no"));
	
	MediaInfo * pmi = pDevice->readMediaInfo();
	if (NULL != pmi)
	{
		_tprintf(_T("\n DVD Video CSS (DVD Audio CPPM) : %s"), pmi->isDVDCssCppm() ? _T("yes") : _T("no"));
		_tprintf(_T("\n DVD Recordable with CPRM : %s"), pmi->isDVDCprm()  ? _T("yes") : _T("no"));
		if (pDevice->isMediaBD())
		{
			DisplayBDMediaInfo(pmi);
		}
		else if (pDevice->isMediaDVD())
		{
			DisplayDVDMediaInfo(pmi);
		}

		_tprintf(_T("\n"));

		pmi->release();
	}
	else
	{
		PrintError(pDevice->error());
	}

	///////////////////////////////////////////////////////////////////////////////////////////
	// 5) Print other disk information

	// Capacity
	DWORD dwBlocks = pDevice->mediaCapacity();
	
	_tprintf(_T("\nDisk Capacity\n"));
	_tprintf(_T("\n Blocks : %d"), dwBlocks);
	_tprintf(_T("\n Size   : %.02fGB %.02fGBytes"), (2048.0 * dwBlocks) / (1024 * 1024 * 1024) , 2048.0 * dwBlocks / 1e9);
	_tprintf(_T("\n"));

	// Available
	dwBlocks = pDevice->mediaFreeSpace();
	
	_tprintf(_T("\nDisk Free Space\n"));
	_tprintf(_T("\n Blocks : %d"), dwBlocks);
	_tprintf(_T("\n Size   : %.02fGB %.02fGBytes"), (2048.0 * dwBlocks) / (1024 * 1024 * 1024) , 2048.0 * dwBlocks / 1e9);
	_tprintf(_T("\n"));

	///////////////////////////////////////////////////////////////////////////////////////////
	// 6) Print track information
	int nLastTrack = GetLastCompleteTrack(pDevice);

	_tprintf(_T("\n Tracks: \n"));
	for (int nTrack = 1; nTrack <= nLastTrack; nTrack++) 
	{
		TrackInfoEx *pTI = pDevice->readTrackInfoEx(nTrack);
		if(NULL != pTI)
		{
			_tprintf(_T("\n  %02d Packet: %d Start: %06d Track Size: %06d Recorded Size: %06d"), 
				pTI->trackNumber(), pTI->isPacket() ? 1 : 0,  pTI->address(), pTI->trackSize(), pTI->recordedSize());

			pTI->release();
		}
		else
		{
			PrintError(pDevice->error());
		}
	}

	_tprintf(_T("\nPress any key...\n"));
	_getch();

	// Release IDevice object
	pDevice->release();

	// Shutdown the engine
	pEngine->shutdown();

	// Release the engine instance
	pEngine->release();

	Library::disableTraceLog();
}

Device * SelectDevice(Engine *pEngine)
{
	// A variable to keep the return value
	Device* pDevice = NULL;

	// Get a device enumerator object. At the time of the creation it will enumerate all CD and DVD Writers installed in the system
	DeviceEnum* pEnum = pEngine->createDeviceEnumerator();
	
	// Get the number of available devices
	int nCount = pEnum->count();

	// If nCount is 0 most likely there are not any CD/DVD writer drives installed in the system 
	if (0 == nCount) 
	{
		_tprintf(_T("No devices available.\n"));
		PrintError(pEnum->error());

		// Release the enumrator to free any allocated resources
		pEnum->release();
		return NULL;
	}

	// Choose a device
	int nDevice;
	do
	{
		_tprintf(_T("Select a device:\n"));

		// Loop through all the devices and show their name and description
		for (int i = 0; i < nCount; i++) 
		{
			// Get device instance from the enumerator
			pDevice = pEnum->createDevice(i);

			// GetItem may return NULL if the device is locked by another process
			if (NULL != pDevice)
			{
				// When showing the devices, show also the drive letter
				_tprintf(_T("  (%c:\\) %s:\n"), pDevice->driveLetter(), pDevice->description());

				pDevice->release();
			}
		}

		_tprintf(_T("\nEnter drive letter: "));
		int ch = _getch();
		
		_tprintf(_T("%c\n"), ch);
		nDevice = Library::driveLetterToDeviceIndex((char)ch);
	} 
	while (nDevice < 0 || nDevice > nCount - 1);

	pDevice = pEnum->createDevice(nDevice);
	if (0 == pDevice)
	{
		PrintError(pEnum->error());

		pEnum->release();
		return NULL;
	}

	pEnum->release();

	return pDevice;
}

int GetLastCompleteTrack(Device * pDevice)
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
			
			// readDiskInfo reports the empty space as a track too
			// That's why we need to go back one track to get the last completed track
			if ((DiscStatus::Open == pDI->discStatus()) || (DiscStatus::Empty == pDI->discStatus()))
				nLastTrack--;

			pDI->release();
		}
		else
		{
			PrintError(pDevice->error());
		}
	}

	return nLastTrack;
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

void DisplayBDMediaInfo(MediaInfo* pMI)
{
	BDMediaInfo* pBDInfo = pMI->bdInfo();

	if(pBDInfo)
	{
		_tprintf(_T("\n -----BD media info-----"));
		_tprintf(_T("\n Manufacturer ID: %ls"), pBDInfo->manufacturerID());
		_tprintf(_T("\n Media Type ID: %ls"), pBDInfo->mediaTypeID());
		_tprintf(_T("\n Product Revision: %.3d"), pBDInfo->productRevision());
	}
}

void DisplayDVDMediaInfo(MediaInfo* pMI)
{
	DVDMediaInfo* pDVDInfo = pMI->dvdInfo();

	if(pMI)
	{
		DisplayDVDPlusMediaInfo(pDVDInfo->plusInfo());
		DisplayDVDMinusMediaInfo(pDVDInfo->minusInfo());
	}
}

void DisplayDVDPlusMediaInfo(DVDPlusMediaInfo* pDVDPl)
{
	if(pDVDPl)
	{
		_tprintf(_T("\n -----DVD+ media info-----"));
		_tprintf(_T("\n Manufacturer ID: %ls"), pDVDPl->manufacturerID());
		_tprintf(_T("\n Media Type ID: %ls"), pDVDPl->mediaTypeID());
		_tprintf(_T("\n Product Revision: %.3d"), pDVDPl->productRevision());
	}
}

void DisplayDVDMinusMediaInfo(DVDMinusMediaInfo* pDVDMin)
{
	if(pDVDMin)
	{
		_tprintf(_T("\n -----DVD- media info-----"));
		_tprintf(_T("\n First Manufacturer ID: %ls"), pDVDMin->manufacturerID1());
		_tprintf(_T("\n Second Manufacturer ID: %ls"), pDVDMin->manufacturerID2());
	
		uint8_t buffer[6] = {0};
		uint32_t written = pDVDMin->manufacturerID3((uint8_t*)buffer, sizeof(buffer));

		_tprintf(_T("\n Third Manufacturer ID - bytes retrieved: %d"), written);
		for (size_t i = 0; i < written; i++)
			_tprintf(_T("\n   Byte# %d: 0x%.2X"), i, buffer[i]);
	}
}