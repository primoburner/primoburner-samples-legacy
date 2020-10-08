// ReadSubChannel.cpp : Defines the entry point for the console application.
//
#include "stdafx.h"

#include "PrimoBurner.h"
using namespace primo::burner;


Device* SelectDevice(Engine *pEngine);
void ScanQSubChannel(long lStartFrame, long lEndFrame, Device * pDevice);

void PrintError(const ErrorInfo *pErrInfo);


void _tmain(int argc, _TCHAR* argv[])
{
	// Library::enableTraceLog(NULL, TRUE);

	Engine *pEngine = Library::createEngine();
	pEngine->initialize();

	// Select a device
	Device* pDevice = SelectDevice(pEngine);
	if (!pDevice)
	{
		pEngine->shutdown();
		pEngine->release();
		return;
	}

	// Select max read speed; 
	pDevice->setReadSpeedKB(pDevice->maxReadSpeedKB());

	// Read the toc
	Toc *pToc = pDevice->readToc();
	if (!pToc)
	{
		PrintError(pDevice->error());
	}
	else
	{
		_tprintf(_T("\nFirst track, Last Track: %d, %d\n\n"), pToc->firstTrack(), pToc->lastTrack());
		for (int i = pToc->firstTrack(); i <= pToc->lastTrack(); i++) 
		{
			int nIndex = i - pToc->firstTrack();
			int nAddr = pToc->tracks()->at(nIndex)->address();

			if (pToc->tracks()->at(nIndex)->isData())
				_tprintf(_T("\t%02d Data  LBA: %05d. Time: (%02d:%02d)\n"), nIndex + 1, nAddr, nAddr / 4500, (nAddr % 4500) / 75);
			else
				_tprintf(_T("\t%02d Audio LBA: %05d. Time: (%02d:%02d)\n"), nIndex + 1, nAddr, nAddr / 4500, (nAddr % 4500) / 75);
		}
	
		_tprintf(_T("Press any key to scan the disc.\n"));
		int ch = _getch();
	
		// Scan the track and get the q-subchannle information
		_tprintf(_T("Press any key to stop scanning.\n"));

		// It is better to scan the whole session so set the lEndAddr at the end of the session
		long lStartAddr = pToc->tracks()->at(0)->address();
		long lEndAddr = pToc->tracks()->at(pToc->lastTrack() - pToc->firstTrack() + 1)->address();

		ScanQSubChannel(lStartAddr, lEndAddr, pDevice);
		
		pToc->release();
	}

	pDevice->release();

	_tprintf(_T("Press any key to exit.\n"));
	int ch = _getch();

	pEngine->shutdown();
	pEngine->release();
	// Library::disableTraceLog();

	return;
}

int ToLba(int min, int sec, int frame)
{
	int lba = (min * 60 + sec) * 75 + frame;
	if (min < 90)
		lba = lba - 150;
	else 
		lba = lba - 450150;
	
	return lba;
}

void ScanQSubChannel(long lStartFrame, long lEndFrame, Device * pDevice)
{
	int nOldIndexNumber = -1, nOldTrackNumber = 0, nOldTrackIndex = -1;
	char szOldMcn[14], szOldIsrc[13];
	
	memset(szOldMcn, 0, sizeof(szOldMcn)); 
	memset(szOldIsrc, 0, sizeof(szOldIsrc)); 

	// Current frame
	long lCurrentFrame = lStartFrame;
	
	// Allocate a buffer for reading. The transfer length should not exceed 64KB buffer.
	// Particularly USB devices will not work if longer, FireWire and ATAPI (IDE) will work.
	uint32_t dwBufferBlocks = (1 << 16) /  BlockSize::CDRaw;
	BYTE * pBuf = new BYTE[dwBufferBlocks * BlockSize::CDRaw];

	// Read the whole session sector by sector and analyze the Q subchannel.
	BOOL bRes = TRUE;
	while (lCurrentFrame < lEndFrame && !_kbhit()) 
	{
		uint32_t dwBlocks = std::min(dwBufferBlocks, (uint32_t)(lEndFrame - lCurrentFrame)); 
		
		// read a raw block with subheaders
		uint32_t dwNumberOfBlocksRead = 0;
		BOOL bRes = pDevice->rawCDRead(lCurrentFrame, SubChannelFormat::PWDeinterleaved, BlockSize::CDRaw, pBuf, dwBlocks, &dwNumberOfBlocksRead);
		if (!bRes)
		{
			lCurrentFrame += dwBlocks;
			PrintError(pDevice->error());
			continue;
		}
	
		for (DWORD i = 0; i < dwBlocks; i++)
		{	
			QSubChannel *pQSub = pDevice->createQSubChannel(SubChannelFormat::PWDeinterleaved, pBuf + (i * BlockSize::CDRaw));
			
			if (pQSub)
			{
				switch(pQSub->formatCode())
				{ 
					// Index Data
					case QSubChannelFormat::Position:
						if (pQSub->position()->trackNumber() > nOldTrackNumber ||
							(pQSub->position()->trackNumber() == nOldTrackNumber && pQSub->position()->indexNumber() > nOldIndexNumber))
						{
							_tprintf(_T("LBA: %05d Ctrl: %d Addr: %d TrackNumber: %02d IndexNumber: %02d AMSF: %02d %02d %02d TMSF: %02d %02d %02d\n"),
										ToLba(pQSub->position()->absoluteMinutes(), pQSub->position()->absoluteSeconds(), pQSub->position()->absoluteFrames()),
										pQSub->position()->control(), 
										pQSub->position()->addr(), 
										pQSub->position()->trackNumber(), 
										pQSub->position()->indexNumber(), 
										pQSub->position()->absoluteMinutes(),
										pQSub->position()->absoluteSeconds(),
										pQSub->position()->absoluteFrames(),
										pQSub->position()->trackMinutes(),
										pQSub->position()->trackSeconds(),
										pQSub->position()->trackFrames()
									);

							nOldIndexNumber = pQSub->position()->indexNumber();
							nOldTrackNumber = pQSub->position()->trackNumber();
						}
					break;

					// Media Catalog Number (MCN)
					case QSubChannelFormat::Mcn:
						if (strncmp(szOldMcn, (const char *)pQSub->mcn()->mcn(), sizeof(szOldMcn) - 1))
						{
							strncpy(szOldMcn, (const char *)pQSub->mcn()->mcn(), sizeof(szOldMcn) - 1);
							_tprintf(_T("Catalog: %s\n"), szOldMcn);
						}
					break;

					// ISRC
					case QSubChannelFormat::Isrc:
						if (strncmp(szOldIsrc, (const char *)pQSub->isrc()->isrc(), sizeof(szOldIsrc) - 1))
						{
							strncpy(szOldIsrc, (const char *)pQSub->isrc()->isrc(), sizeof(szOldIsrc) - 1);
							_tprintf(_T("ISRC %s\n"), szOldIsrc);
						}
					break;
				}

				pQSub->release();
			}
			else
			{
				PrintError(pDevice->error());
			}
		}

		lCurrentFrame += dwBlocks;
	}

	delete [] pBuf;
}

Device * SelectDevice(Engine *pEngine)
{
	int ch;
	int nDevice;

	Device* pDevice = NULL;

	DeviceEnum* pEnum = pEngine->createDeviceEnumerator();
	int nCount = pEnum->count();
	if (0 == nCount) 
	{
		_tprintf(_T("No devices available\n"));
		PrintError(pEnum->error());

		pEnum->release();
		return NULL;
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
	if (0 == pDevice)
	{
		PrintError(pEnum->error());

		pEnum->release();
		return NULL;
	}

	pEnum->release();

	return pDevice;
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
