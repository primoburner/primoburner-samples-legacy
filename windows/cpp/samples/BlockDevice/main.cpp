#define _CRT_NON_CONFORMING_SWPRINTFS
#define _CRT_SECURE_NO_WARNINGS
#define _CRT_SECURE_CPP_OVERLOAD_SECURE_NAMES 1

#define _BIND_TO_CURRENT_CRT_VERSION 1

#include <tchar.h>
#include <conio.h>
#include <stdio.h>
#include <assert.h>

#include "PrimoBurner.h"
using namespace primo::burner;

#include <vector>
#include <string>
using namespace std;

#ifdef _UNICODE
	typedef std::wstring tstring;
#else
	typedef std::string tstring;
#endif

// Structure to keep the file information
struct TFile
{
	// full path to the file
	tstring sPath;

	// address on the CD/DVD
	DWORD dwDiscAddress;

	// size on the CD/DVD (in blocks)
	DWORD64 ddwDiscSize;
};

// Array of files
typedef vector<TFile> TFileVector;

/////////////////////////////////////////////
// Burning Options
//
enum EBurnOption
{
	BO_UNKNOWN	= 0,
	BO_WRITE,			// write
	BO_READ_DISC_ID,	// read
	BO_ERASE			// erase
};

/////////////////////////////////////////////
// Global variables
//
TFileVector g_SourceFiles;					// Source files
EBurnOption g_eBurnOption = BO_UNKNOWN;		// Burning option

/////////////////////////////////////////////
// Command line handlers 
//
void Usage()
{
   _tprintf(_T("BlockDevice [-e] [-i <sourcefile>] [-i <sourcefile>] [-i <sourcefile>]\n"));
   _tprintf(_T("    -i sourcefile = file to burn, multiple files can be specified.\n"));
   _tprintf(_T("    -e            = erase RW disc. \n"));
   _tprintf(_T("    -d            = read and display temporary disc ID. \n"));
}

int ParseCommandLine(int argc, TCHAR* argv[])
{
    int             i = 0 ;
	int				iResult = 0 ;

    for( i = 1; i < argc; i++ )
    {
		// Input file
        if( 0 == _tcsicmp( argv[i], _T( "-i" ) ) )
        {
            i++;
            if( i == argc )
            {
                Usage();
                return E_INVALIDARG;
            }

			TFile f;
			f.sPath = argv[i];
            
			g_SourceFiles.push_back(f);

            continue;
        }

		// Erase
        if( 0 == _tcsicmp( argv[i], _T( "-e" ) ) )
        {
			if (BO_UNKNOWN != g_eBurnOption)
			{
                Usage();
			    return E_INVALIDARG;
			}

			g_eBurnOption = BO_ERASE;
            continue;
        }

		// Read Disc ID
        if( 0 == _tcsicmp( argv[i], _T( "-d" ) ) )
        {
			if (BO_UNKNOWN != g_eBurnOption)
			{
                Usage();
			    return E_INVALIDARG;
			}

			g_eBurnOption = BO_READ_DISC_ID;
            continue;
        }
	}        

	// Set the default for burn option
	if (BO_UNKNOWN == g_eBurnOption)
		g_eBurnOption = BO_WRITE;

	// source files must be specified, unless we erase the disk
    if (BO_ERASE != g_eBurnOption && BO_READ_DISC_ID != g_eBurnOption && 0 == g_SourceFiles.size())
    {
        Usage();
        return E_INVALIDARG;
    }

	return 0;
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

	case ErrorFacility::BlockDevice:
		_tprintf(_T("BlockDevice Error: 0x%06x - %s \n"), pErrInfo->code(), pErrInfo->message());
		break;

	default:
		_tprintf(_T("Error Facility: 0x%06x   Code: 0x%06x - %s \n"), pErrInfo->facility(), pErrInfo->code(), pErrInfo->message());
		break;
	}
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
		printf("No devices available.\n");
		PrintError(pEnum->error());

		// Release the enumrator to free any allocated resources
		pEnum->release();
		return NULL;
	}

	// Now ask the user to choose a device
	do
	{
		printf("Select a device:\n");

		// Loop through all the devices and show their name and description
		for (int i = 0; i < nCount; i++) 
		{
			// Get device instance from the enumerator
			pDevice = (Device*) pEnum->createDevice(i);

			// GetItem may return NULL if the device was locked
			if (NULL != pDevice)
			{
				// Get device description
				const TCHAR* name = pDevice->description();

				// When showing the devices, show also the drive letter
				printf("  (%c:\\) %S:\n", pDevice->driveLetter(), name);

				pDevice->release();
			}
		}

		printf("\nEnter drive letter: ");
		ch = _getch();
		
		printf("%c\n", ch);
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

void WaitUnitReady(Device * pDevice)
{
	// Use the new GetUnitReady method to detect when the device is ready.
	DWORD dwDeviceError = pDevice->unitReadyState();
	while (DeviceError::Success != dwDeviceError)
	{
		printf("Unit not ready: 0x%06X\n", dwDeviceError);

		Sleep(1000);
		dwDeviceError = pDevice->unitReadyState();
	}
	
	printf("Unit is ready.\n");
}

/////////////
// Erase
void Erase(Device * pDevice)
{
	MediaProfile::Enum mp = pDevice->mediaProfile();
	switch(mp)
	{
		// DVD+RW (needs to be formatted before the disc can be used)
	case MediaProfile::DVDPlusRW:
		{
			printf("Formatting...\n");

			BgFormatStatus::Enum fmt = pDevice->bgFormatStatus();
			switch(fmt)
			{
			case BgFormatStatus::NotFormatted:
				pDevice->format(FormatType::DVDPlusRWFull);
				break;
			case BgFormatStatus::Partial:
				pDevice->format(FormatType::DVDPlusRWRestart);
				break;
			}
		}
		break;

		// DVD-RW, Sequential Recording (default for new DVD-RW)
	case MediaProfile::DVDMinusRWSeq:
			printf("Erasing...\n");
			pDevice->eraseEx(EraseType::Minimal);
		break;

		// DVD-RW, Restricted Overwrite (DVD-RW was formatted initially)
	case MediaProfile::DVDMinusRWRO:
			printf("Formatting...\n");
			pDevice->format(FormatType::DVDMinusRWQuick);
		break;

	case MediaProfile::CDRW:
			printf("Erasing...\n");
			pDevice->eraseEx(EraseType::Minimal);
		break;
	}

	// Must be DVD-R, DVD+R or CD-R
}

/////////////////////////////////////////////////////////////
// Burn 
//

// Get file size in bytes
DWORD64 GetFileSizeEx(HANDLE hFile)
{
	DWORD dwFileSizeHigh = 0;
	DWORD dwFileSize = ::GetFileSize(hFile, &dwFileSizeHigh);
	
	ULARGE_INTEGER uliFileSize;
	uliFileSize.HighPart = dwFileSizeHigh; 
	uliFileSize.LowPart = dwFileSize;

	return uliFileSize.QuadPart;
}

// Size must be aligned to 16 blocks
#define BLOCKS_PER_WRITE (10 * 16)

BOOL BurnFile(BlockDevice* pBlockDevice, TFile & file)
{
	// Get the start address at which IBlockDevice will start writing.
	DWORD dwDiscAddress = pBlockDevice->writeAddress();

	// Open file
	HANDLE hFile = ::CreateFile(file.sPath.c_str(), GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
	if (INVALID_HANDLE_VALUE == hFile)
		return FALSE;

	// Set up write progress counters
	DWORD64 ddwCurrent = 0;
	DWORD64 ddwAll = GetFileSizeEx(hFile);

	// Allocate read buffer
	const DWORD dwBlocksAtOnce = BLOCKS_PER_WRITE;
	BYTE * pBuffer = new BYTE[BlockSize::DVD * dwBlocksAtOnce]; 

	// Write the data
	while(ddwCurrent < ddwAll)
	{
		DWORD dwRead = 0;
		if (!::ReadFile(hFile, pBuffer, BlockSize::DVD * dwBlocksAtOnce, &dwRead, NULL))
			break;
		
		if (0 == dwRead)
			break;

		// Align on 2048 bytes
		if (dwRead % BlockSize::DVD)
			dwRead += BlockSize::DVD - (dwRead % BlockSize::DVD);  

		// Write the data
		if(!pBlockDevice->write(pBuffer, dwRead))
			break;

		// Update current position (bytes)
		ddwCurrent += dwRead;
	}

	// free buffer
	delete [] pBuffer;

	// Close file
	::CloseHandle(hFile);

	// Update TFile structure
	file.dwDiscAddress = dwDiscAddress;
	file.ddwDiscSize = ddwAll;

	return TRUE;
}

tstring GetFileName(tstring sPath)
{
	TCHAR pName[_MAX_FNAME] = {0}, pExt[_MAX_EXT] = {0};
	_tsplitpath(sPath.c_str(), 0, 0, pName, pExt);

	TCHAR pFileName[MAX_PATH];
	_stprintf(pFileName, _T("%s%s"), pName, pExt);

	tstring sRet = pFileName;
	return sRet;
}

DataFile* CreateFileSystemTree(TFileVector & files)
{
	// Entry for the root of the image file system
	DataFile * pRoot = Library::createDataFile(); 

	pRoot->setDirectory(TRUE);
	pRoot->setPath(_T("\\"));			
	pRoot->setLongFilename(_T("\\"));

	// Add the files
	for(size_t i = 0; i < files.size(); i++)
	{
		TFile & file = files[i];


		DataFile * pFile = Library::createDataFile(); 
			
			pFile->setDirectory(FALSE);					// this is a file

			// Filename long and short
			pFile->setLongFilename(GetFileName(file.sPath).c_str());			
			pFile->setShortFilename(_T(""));

			pFile->setDataSource(DataSource::Disc);		// it is already on the cd/dvd	
			pFile->setDiscAddress(file.dwDiscAddress);	// set the disc address
			pFile->setSize(file.ddwDiscSize);		// and the size

			// If needed set other IDataFile properties here
			pRoot->children()->add(pFile);

		pFile->release();
	}

	return pRoot;
}

BOOL Burn(BlockDevice* pBlockDevice, TFileVector& files)
{
	// Open block device 
	if (!pBlockDevice->open())
		return FALSE;

	// Burn all files that the user specified
	for(size_t i = 0; i < files.size(); i++)
		BurnFile(pBlockDevice, files[i]);

	// Close block device
	if (!pBlockDevice->close())
		return FALSE;
	
	// Create file system
	DataFile * pFileSystem = CreateFileSystemTree(files);

	// Finalize disc
	if (!pBlockDevice->finalizeDisc(pFileSystem, _T("PRIMOBURNERDISC"), TRUE, TRUE, FALSE))
	{
		pFileSystem->release();
		return FALSE;
	}

	pFileSystem->release();
	return TRUE;
}

BOOL Burn(Device* pDevice)
{
	assert(pDevice);

	// Set write speed
	int iMaxWriteSpeedKB = pDevice->maxWriteSpeedKB();
	double speed1X = 0;
	if(pDevice->isMediaDVD())
	{
		speed1X = Speed1xKB::DVD;
	}
	else if(pDevice->isMediaBD())
	{
		speed1X = Speed1xKB::BD;
	}
	else
	{
		speed1X = Speed1xKB::CD;
	}

	_tprintf(_T("Setting write speed to: %.02f\n"), (double)iMaxWriteSpeedKB / speed1X);

	pDevice->setWriteSpeedKB(iMaxWriteSpeedKB);

	// Create BlockDevice object 
	BlockDevice* pBlockDevice = Library::createBlockDevice();
	
	// Set device 
	pBlockDevice->setDevice(pDevice);

	// Set temporary disc ID
	pBlockDevice->setTempDiscID(_T("DISCID"));

	// Burn 
	BOOL bRes = Burn(pBlockDevice, g_SourceFiles);

	// Check for errors
	if (!bRes) 
	{
		PrintError(pBlockDevice->error());
		
		pBlockDevice->release();
		return FALSE;
	}

	pBlockDevice->release();
	return TRUE;
}

void ReadDiscID(Device* pDevice)
{
	// Create BlockDevice object 
	BlockDevice* pBlockDevice = Library::createBlockDevice();
	
	// Set device 
	pBlockDevice->setDevice(pDevice);

	// Open block device 
	if (!pBlockDevice->open(BlockDeviceOpenFlags::Read))
		return;

	// Show information about the disc in the device
	printf("\nDisc Info\n");
	printf("---------\n");

	printf("Finalized: %d\n", pBlockDevice->isFinalized());
	printf("Temporary Disc ID: %s\n", pBlockDevice->tempDiscID());

	// Close block device
	pBlockDevice->close();
}

/////////////////////////////////////////////////////////////
// Main 
//
int _tmain( int argc, TCHAR *argv[])
{
	int iParseResult = ParseCommandLine(argc, argv);
	if (0 != iParseResult)
		return iParseResult;

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

	// Close the device tray and refresh disc information
	if (pDevice->eject(FALSE))
	{
		// Wait for the device to become ready
		WaitUnitReady(pDevice);

		// Refresh disc information. Need to call this method when media changes
		pDevice->refresh();
	}

	// Check if disc is present
	if (MediaReady::Present != pDevice->mediaState())
	{
		printf("Please insert a disc in the device and try again.\n");
		pDevice->release();

		pEngine->shutdown();
		pEngine->release();
		return -1;		
	}

	// Do the work now
	switch (g_eBurnOption)
	{
		case BO_ERASE:
			Erase(pDevice);
		break;
		case BO_READ_DISC_ID:
			ReadDiscID(pDevice);
		break;
		default:

			// Check if disc is blank
			MediaProfile::Enum mp = pDevice->mediaProfile();
			if ((MediaProfile::DVDPlusRW != mp) && 
				(MediaProfile::DVDRam != mp) &&
				(MediaProfile::BDRE != mp) && !pDevice->isMediaBlank())
			{
				printf("Please insert a blank disc in the device and try again.\n");
				pDevice->release();

				pEngine->shutdown();
				pEngine->release();
				return -1;		
			}

			Burn(pDevice);
		break;
	}

	// Dismount the device volume. This forces the operating system to refresh the CD file system.
	pDevice->dismount();

	// Release the IDevice object
	pDevice->release();

	// Shutdown the engine
	pEngine->shutdown();

	// Release the engine instance
	pEngine->release();

	Library::disableTraceLog();

	return 0;
}
