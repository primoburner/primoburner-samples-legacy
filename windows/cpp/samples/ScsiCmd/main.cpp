// ReadSubChannel.cpp : Defines the entry point for the console application.
//
#include "stdafx.h"
#include "PrimoBurner.h"
using namespace primo::burner;

BOOL Inquiry(ScsiInterface * pScsi)
{
	// INQUIRY = 0x12
	BYTE cmd[12] = {0};
	
	cmd[0] = 0xBE;	
	cmd[1] = 4;

	cmd[2] = 0;
	cmd[3] = 0;
	cmd[4] = 0x1e;
	cmd[5] = 0xd8;

	cmd[6] = 0;
	cmd[7] = 0;
	cmd[8] = 16;
	
	cmd[9] = 0xf8;

	cmd[10] = 2;

	cmd[11] = 0;

	BYTE buffer[16* (2352+16) ] = {0};
	
	BOOL ret = pScsi->sendCommand(cmd, sizeof(cmd), ScsiCommandDirection::Read, buffer, sizeof(buffer));
	if (!ret)
	{
		_tprintf(_T("Inquiry failed.\n"));
		
		ScsiCommandSense sense = {0};
		pScsi->getSense(&sense);
		
		_tprintf(_T("SCSI Error -> 0x%06x\n"), pScsi->error());
		_tprintf(_T("SCSI Sense -> Key: 0x%02x ASC: 0x%02x ASCQ: 0x%02x - %s\n"), sense.Key, sense.ASC, sense.ASCQ, pScsi->getSenseMessage(&sense));

		return FALSE;
	}
	
	// Check if it is a CD/DVD/BD/HD-DVD
	/*if ((buffer[0] & 0x1F) != 5)
	{
		_tprintf(_T("Not an MMC unit!\n"));	
		return FALSE;
	}

	printf("INQUIRY: [%.8s][%.16s][%.4s]\n", &buffer[8], &buffer[16], &buffer[32]);*/

	return TRUE;
}

BOOL TestUnitReady(ScsiInterface * pScsi) 
{
	// TestUnitReady = 0x00
	BYTE cmd[6] = {0};

	BOOL ret = pScsi->sendCommand(cmd, sizeof(cmd));
	if (!ret)
	{
		_tprintf(_T("Unit is not ready.\n"));	
		
		ScsiCommandSense sense = {0};
		pScsi->getSense(&sense);
		
		_tprintf(_T("SCSI Error -> 0x%06x\n"), pScsi->error());
		_tprintf(_T("SCSI Sense -> Key: 0x%02x ASC: 0x%02x ASCQ: 0x%02x - %s\n"), sense.Key, sense.ASC, sense.ASCQ, pScsi->getSenseMessage(&sense));

		return FALSE;
	}
	
	_tprintf(_T("Unit is ready.\n"));	
	return TRUE;
}

BOOL StartStopUnit(ScsiInterface * pScsi, BOOL bEject)
{
	BYTE cmd[6] = {0};

	// START/STOP UNIT
	cmd[0] = 0x1B; 
	cmd[4] = bEject ? 0x02 : 0x03; // LoUnlo = 1, Start = 0 : LoUnlo = 1, Start = 1

	BOOL ret = pScsi->sendCommand(cmd, sizeof(cmd));
	if (!ret)
	{
		_tprintf(_T("StartStopUnit failed.\n"));	
		
		ScsiCommandSense sense = {0};
		pScsi->getSense(&sense);
		
		_tprintf(_T("SCSI Error -> 0x%06x\n"), pScsi->error());
		_tprintf(_T("SCSI Sense -> Key: 0x%02x ASC: 0x%02x ASCQ: 0x%02x - %s\n"), sense.Key, sense.ASC, sense.ASCQ, pScsi->getSenseMessage(&sense));

		return FALSE;
	}
	
	_tprintf(_T("StartStopUnit success.\n"));	
	return TRUE;
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

// Prototypes
Device * SelectDevice(Engine *pEngine);

int _tmain(int argc, _TCHAR* argv[])
{
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
	
	// The SelectDevice function allows the user to select a device and then 
	// returns an instance of IDevice.
	Device* pDevice = SelectDevice(pEngine);
	if (!pDevice)
	{
		// Something went wrong. Shutdown the engine.
		pEngine->shutdown();

		// Release the engine instance. That will free any allocated resources
		pEngine->release();

		// We are done.
		return -1;
	}

	///////////////////////////////////////////////////////////////////////////////////////////
	// 4) Create IScsiInterface instance and execute some commands.
	
	ScsiInterface *pScsi = pDevice->createScsiInterface();

	// SCSI inquiry
	Inquiry(pScsi);

	// Open the device tray 
	//StartStopUnit(pScsi, TRUE);

	// See if the unit is ready. If the tray has ejected unit will not be ready and 
	// IScsiInterface will report back SCSI sense data:
	//		Scsi Sense -> Key: 0x02 ASC: 0x3a ASCQ: 0x00 - MEDIUM NOT PRESENT Or
	//		Scsi Sense -> Key: 0x02 ASC: 0x3a ASCQ: 0x02 - MEDIUM NOT PRESENT - TRAY OPEN 
	//TestUnitReady(pScsi);

	/*_tprintf(_T("\nPlease load a disc and press any key.\n"));
	int ch = _getch();*/

	// Close the device tray 
	/*StartStopUnit(pScsi, FALSE);*/

	// See if the unit is ready
	/*TestUnitReady(pScsi);

	_tprintf(_T("\nPress any key.\n"));
	ch = _getch();*/

	// Release IScsiInterface object
	pScsi->release();

	// Release IDevice object
	pDevice->release();

	// Shutdown the engine
	pEngine->shutdown();

	// Release the engine instance
	pEngine->release();

	Library::disableTraceLog();

	return 0;
}

Device * SelectDevice(Engine *pEngine)
{
	int ch;
	int nDevice;

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

	// Now ask the user to choose a device
	do
	{
		_tprintf(_T("Select a device:\n"));

		// Loop through all the devices and show their name and description
		for (int i = 0; i < nCount; i++) 
		{
			// Get device instance from the enumerator
			pDevice = pEnum->createDevice(i);

			// GetItem may return NULL if the device was locked
			if (NULL != pDevice)
			{
				// When showing the devices, show also the drive letter
				_tprintf(_T("  (%c:\\) %s:\n"), pDevice->driveLetter(), pDevice->description());

				pDevice->release();
			}
		}

		_tprintf(_T("\nEnter drive letter: "));
		ch = _getch();
		
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
