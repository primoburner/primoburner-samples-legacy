#include "Reader.h"
#include "ReaderException.h"

Reader::Reader(void)
{
	m_Engine = NULL;
	m_Enumerator = NULL;
	m_Device = NULL;
	m_DataDisc = NULL;

	m_isOpen = false;
	m_UseImage = false;
	m_TrackIndex = 0;
	m_Callback = NULL;
}

Reader::~Reader(void)
{
	Close();
}

//Public methods
bool Reader::Open()
{	
	if (m_isOpen)
		return true;

	// Enable trace log
	Library::enableTraceLog(NULL, TRUE);

	m_Engine = Library::createEngine();
	if (!m_Engine->initialize()) 
	{
		throw ReaderException(m_Engine);
	}

	m_isOpen = true;
	return true;
}

void Reader::Close()
{
	CleanupDataDisc();
	CleanupDevice();
	CleanupDeviceEnum();
	CleanupEngine();

	Library::disableTraceLog();

	m_isOpen = false;
}

void Reader::set_Callback(ReaderCallback* callback)
{
	m_Callback = callback;
}
const DeviceVector& Reader::EnumerateDevices()
{
	if (!m_isOpen)
	{
		throw ReaderException(RE_READER_NOT_OPEN, RE_READER_NOT_OPEN_TEXT);
	}

	m_DeviceVector.clear();

	CleanupDeviceEnum();
	m_Enumerator = m_Engine->createDeviceEnumerator();

	int32_t deviceCount = m_Enumerator->count();
	if (0 == deviceCount) 
	{
		throw ReaderException(RE_NO_DEVICES, RE_NO_DEVICES_TEXT);
	}

	for (int32_t i = 0; i < deviceCount; i++) 
	{
		Device* device = m_Enumerator->createDevice(i);
		if (NULL != device)
		{
			DeviceInfo dev;
			dev.Index = i;
			dev.Title = get_DeviceTitle(device);
			dev.DriveLetter = device->driveLetter();

			m_DeviceVector.push_back(dev);

			device->release();
		}
		else
		{
			throw ReaderException(m_Enumerator);
		}
	}

	return m_DeviceVector;
}

const TrackVector& Reader::EnumerateTracks(int32_t deviceIndex)
{
	if (!m_isOpen)
	{
		throw ReaderException(RE_READER_NOT_OPEN, RE_READER_NOT_OPEN_TEXT);
	}

	m_TrackVector.clear();
	SelectDevice(deviceIndex, false);

	if (NULL != m_Device)
	{
		if (m_Device->isMediaCD())
		{
			PrepareCDTracks(m_Device);
		}
		else if (m_Device->isMediaDVD() || m_Device->isMediaBD())
		{
			PrepareDVDBDTracks(m_Device);
		}
	}
	else
	{
		throw ReaderException(RE_DEVICE_NOT_SET, RE_DEVICE_NOT_SET_TEXT);
	}
	return m_TrackVector;
}

void Reader::ReadTrackUserData(TrackRipSettings& settings)
{
	if (!m_isOpen)
	{
		throw ReaderException(RE_READER_NOT_OPEN, RE_READER_NOT_OPEN_TEXT);
	}

	SelectDevice(settings.get_DeviceIndex(), false);

	if (NULL != m_Device)
	{
		if (m_Device->isMediaCD())
		{
			RipCDTrack(m_Device, settings);
		}
		else if (m_Device->isMediaDVD() || m_Device->isMediaBD())
		{
			RipDVDBDTrack(m_Device, settings);
		}
	}
	else
	{
		throw ReaderException(RE_DEVICE_NOT_SET, RE_DEVICE_NOT_SET_TEXT);
	}
}
void Reader::PrepareSource(int32_t deviceIndex, int32_t trackIndex)
{
	SelectDevice(deviceIndex, false);
	m_TrackIndex = trackIndex;
	m_UseImage = false;
}
void Reader::PrepareSource(tstring& imageFile)
{
	m_ImageFile = imageFile;
	m_UseImage = true;
}
void Reader::ReadFileFromSource(tstring filePath, tstring destinationFolder)
{
	DataFile* dataFile = FindItem(filePath);
	if (NULL == dataFile)
	{
		throw ReaderException(RE_ITEM_NOT_FOUND, RE_ITEM_NOT_FOUND_TEXT);
	}
	try
	{
		if (m_UseImage)
		{
			ReadFileFromImage(dataFile, destinationFolder);
		}
		else
		{
			ReadFileFromDisc(dataFile, destinationFolder);
		}
	}
	catch (ReaderException& ex)
	{
		dataFile->release();
		throw ex;
	}
	dataFile->release();
}
void Reader::GetFolderContentFromLayout(tstring folderPath, LayoutItemVector& content)
{
	content.clear();
	DataFile* dataFile = FindItem(folderPath);
	if (NULL == dataFile)
	{
		throw ReaderException(RE_ITEM_NOT_FOUND, RE_ITEM_NOT_FOUND_TEXT);
	}
	if (!dataFile->directory())
	{
		dataFile->release();
		throw ReaderException(RE_ITEM_NOT_FOLDER, RE_ITEM_NOT_FOLDER_TEXT);
	}

	DataFileList* children = dataFile->children();
	for (int i = 0; i < children->count(); i++)
	{
		DataFile* item = children->at(i);

		LayoutItem layoutItem;
		layoutItem.Address = item->discAddress();
		layoutItem.FileName = item->longFilename();
		layoutItem.FileTime = item->creationTime(); //Get file creation time. It will be UTC(GMT)
		layoutItem.IsDirectory = 1 == item->directory();
		layoutItem.SizeInBytes = item->size();
		content.push_back(layoutItem);
	}

	dataFile->release();
}
//Protected methods
void Reader::CleanupEngine()
{
	if (NULL != m_Engine)
	{
		m_Engine->shutdown();
		m_Engine->release();
	}
	m_Engine = NULL;
}

void Reader::CleanupDeviceEnum()
{
	if (NULL != m_Enumerator)
	{
		m_Enumerator->release();
	}
	m_Enumerator = NULL;
}

void Reader::CleanupDevice()
{
	if (NULL != m_Device)
	{
		m_Device->release();
	}
	m_Device = NULL;
}

void Reader::CleanupDataDisc()
{
	if (NULL != m_DataDisc)
	{
		m_DataDisc->release();
	}
	m_DataDisc = NULL;
}

void Reader::SelectDevice(int32_t deviceIndex, bool exclusive)
{
	CleanupDeviceEnum();
	CleanupDevice();
	m_Enumerator = m_Engine->createDeviceEnumerator();

	m_Device = m_Enumerator->createDevice(deviceIndex, exclusive ? 1 : 0);

	if (NULL == m_Device)
	{
		throw ReaderException(m_Enumerator);
	}
}
tstring Reader::get_DeviceTitle(Device* device)
{
	char_t displayName[200] = {0};
	_stprintf(displayName, _TEXT("(%C:) - %s"), device->driveLetter(), device->description());

	return displayName;
}

void Reader::BuildTrackSegmentList(Device* device, TrackInfoEx *pTrackInfo, TrackSegmentVector& segments)
{
	segments.clear();
	
	if (LayerJumpRecordingStatus::NoLayerJump != pTrackInfo->layerJumpRecordingStatus())
	{
		// This case should be reached only when processing DVD-R DL LJ media
		uint32_t l0Size = device->mediaLayerCapacity();
		if (0 == l0Size)
		{
			throw ReaderException(device);
		}

		if (pTrackInfo->isLRAValid())
		{
			// The start addres of a track in layer jump recording mode would be on layer 0 and the last
			// possible address of a track in layer jump recording would be on layer 1. To calculate the
			// size of such a track TRACKINFOEX::LastLayerJumpAddress value must be used. The formula
			// states that the address of the first block after the jump on layer 1 would be
			// '2M - 1 - LJA' where M is the start LBA of the data area on layer 1 and LJA is the layer
			// jump address on layer 0 (address at which the jump was performed from layer 0 to layer 1)
			// Note that DVD-R DL are OTP (Opposite Track Path) discs. Furthermore layer 0 is slightly
			// larger than layer 1.

			if (l0Size <= static_cast<uint32_t>(pTrackInfo->lastRecordedAddress()))
			{
				// add segment on layer0
				segments.push_back(TrackSegment(pTrackInfo->address(), pTrackInfo->lastLayerJumpAddress() - pTrackInfo->address() + 1));
				// add segment on layer1
				uint32_t layer1StartAddress = 2 * l0Size - 1 - pTrackInfo->lastLayerJumpAddress();
				uint32_t layer1RecordedSize = pTrackInfo->lastRecordedAddress() - layer1StartAddress + 1;
				segments.push_back(TrackSegment(layer1StartAddress, layer1RecordedSize));
			}
			else
			{
				segments.push_back(TrackSegment(pTrackInfo->address(), pTrackInfo->lastRecordedAddress() - pTrackInfo->address() + 1));
			}
		}
		else
		{
			throw ReaderException(RE_TRACK_LRA_INVALID, RE_TRACK_LRA_INVALID_TEXT);
		}
	}
	else
	{
		segments.push_back(TrackSegment(pTrackInfo->address(), pTrackInfo->recordedSize()));
	}
}

int Reader::GetLastCompleteTrack(Device * pDevice)
{
	// Get the last track number from the last session if multisession option was specified
	int nLastTrack = 0;

	// Check for DVD+RW and DVD-RW RO random writable media. 
	MediaProfile::Enum mp = pDevice->mediaProfile();

	if ((MediaProfile::DVDPlusRW == mp) || (MediaProfile::DVDMinusRWRO == mp) || 
		(MediaProfile::BDRE == mp)  || (MediaProfile::DVDRam == mp))
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
			throw ReaderException(pDevice);
		}
	}

	return nLastTrack;
}

void Reader::PrepareCDTracks(Device* device)
{
	if (NULL != device)
	{
		// Read the table of content
		Toc *pToc = m_Device->readToc();
		if (!pToc)
		{
			throw ReaderException(m_Device);
		}

		// Last entry is the start address of lead-out
		for (int i = pToc->firstTrack(); i <= pToc->lastTrack(); i++) 
		{
			int nIndex = i - pToc->firstTrack();
			int nAddr = pToc->tracks()->at(nIndex)->address();
			char_t title[200] = {0};

			if (pToc->tracks()->at(nIndex)->isData())
				_stprintf(title, _T("%02d Data  LBA: %05d. Time: (%02d:%02d)"), nIndex + 1, nAddr, nAddr / 4500, (nAddr % 4500) / 75);
			else
				_stprintf(title, _T("%02d Audio LBA: %05d. Time: (%02d:%02d)"), nIndex + 1, nAddr, nAddr / 4500, (nAddr % 4500) / 75);

			m_TrackVector.push_back(TrackDetails(nIndex + 1, title));
		}

		pToc->release();
	}
	else
	{
		throw ReaderException(RE_DEVICE_NOT_SET, RE_DEVICE_NOT_SET_TEXT);
	}
}

void Reader::PrepareDVDBDTracks(Device* device)
{
	if (NULL != device)
	{
		int lastTrackIndex = GetLastCompleteTrack(device);
		if (0 == lastTrackIndex)
			return;

		for (int trackIndex = 1; trackIndex <= lastTrackIndex; trackIndex++)
		{
			TrackInfoEx* pTi = device->readTrackInfoEx(trackIndex);

			if (pTi)
			{
				TrackSegmentVector segments;
				BuildTrackSegmentList(device, pTi, segments);
				uint32_t trackSize = 0;
				for (size_t j = 0; j < segments.size(); j++)
				{
					TrackSegment segment = segments[j];
					trackSize += segment.RecordedSize;
				}

				char_t title[200] = {0};
				_stprintf(title, _T("%02d Packet: %d Start: %06d Track Size: %06d Recorded Size: %06d"), 
					pTi->trackNumber(), pTi->isPacket() ? 1 : 0,  pTi->address(), pTi->trackSize(), trackSize);

				
				m_TrackVector.push_back(TrackDetails(trackIndex, title));
				pTi->release();
			}
			else
			{
				throw ReaderException(device);
			}
		}
	}
	else
	{
		throw ReaderException(RE_DEVICE_NOT_SET, RE_DEVICE_NOT_SET_TEXT);
	}
}

bool Reader::ReadFileSectionFromDevice(FileSegment& segment, TrackBuffer *pBuffer, FILE* destinationFile)
{
	uint32_t blocksAtOnce = BufferSize::DefaultReadBufferBlocks;
	uint32_t currentBlockSize = BlockSize::CDRom;

	uint32_t startFrame = segment.StartAddress;
	uint32_t endFrame = segment.EndAddress;

	while (startFrame < endFrame) 
	{
		uint32_t numFrames = std::min(blocksAtOnce, endFrame - startFrame);
		if (!m_Device->readData(pBuffer, startFrame, numFrames))
		{
			return false;
		}

		CallOnReadProgress(startFrame, pBuffer->blocks(), pBuffer->blockSize()); 

		// Detect changes of block size
		if (currentBlockSize != pBuffer->blockSize())
		{
			CallOnBlockSizeChanged(currentBlockSize, pBuffer->blockSize(), startFrame);
			currentBlockSize = pBuffer->blockSize();
		}

		// Update buffer start address in frames (blocks)
		startFrame += pBuffer->blocks();

		if (startFrame < endFrame)
			fwrite(pBuffer->buffer(), sizeof(BYTE), pBuffer->blocks() * pBuffer->blockSize(), destinationFile);
		else
		{
			// The real file size may not align on BlockSize::CDRom. 
			// We should not write the padding bytes
			uint32_t padding = BlockSize::CDRom - (uint32_t)(segment.FileSize % BlockSize::CDRom);
			if (BlockSize::CDRom == padding)
			{
				// if the remainder of (segment.FileSize % BlockSize::CDRom) is 0 then there should be no padding
				padding = 0;
			}

			fwrite(pBuffer->buffer(), sizeof(BYTE), (pBuffer->blocks() * pBuffer->blockSize()) - padding, destinationFile);
		}
	}

	return false;
}

void Reader::ReadFileFromDisc(DataFile* dataFile, tstring destinationFolder)
{
	if (NULL == m_Device)
	{
		throw ReaderException(RE_DEVICE_NOT_SET, RE_DEVICE_NOT_SET_TEXT);
	}

	if (NULL != dataFile)
	{
		if (!dataFile->directory())
		{
			std::vector<FileSegment> segments;
			// Use Udf file extents to track the location of the file segments on the medium. 
			UdfExtentList* extents = dataFile->udfProps()->extents();
			
			int32_t cnt = extents->count();
			if (0 < cnt)
			{
				for (int32_t i = 0; i < cnt; i++)
				{
					UdfExtent* extent = extents->at(i);
					segments.push_back(FileSegment(extent->address(), extent->length()));
				}
			}
			else
			{
				segments.push_back(FileSegment(dataFile->discAddress(), dataFile->size()));
			}
			// Make the full path
			char_t destinationFilePath[MAX_PATH];
			_stprintf(destinationFilePath, _T("%s/%s"), destinationFolder.c_str(), dataFile->longFilename());

			// Open the file and write all the data
			FILE * destinationFile = _tfopen(destinationFilePath, _T("wb"));
			if (0 != destinationFile)
			{
				uint32_t currentBlockSize = BlockSize::CDRom;
				uint32_t blocksAtOnce = BufferSize::DefaultReadBufferBlocks;
				bool deviceError = false;

				TrackBuffer *pBuffer = Library::createTrackBuffer(currentBlockSize, blocksAtOnce);
				for (size_t si = 0; si < segments.size(); si ++)
				{
					FileSegment segment = segments[si];
					deviceError = !ReadFileSectionFromDevice(segment, pBuffer, destinationFile);
					if (deviceError)
					{
						break;
					}
				}

				pBuffer->release();

				fflush(destinationFile);
				fclose(destinationFile);

				if (deviceError)
				{
					throw ReaderException(m_Device);
				}
			}
		}
		else
		{
			throw ReaderException(RE_ITEM_NOT_FILE, RE_ITEM_NOT_FILE_TEXT);
		}
	}
	else
	{
		throw ReaderException(RE_ITEM_NOT_FOUND, RE_ITEM_NOT_FOUND_TEXT);
	}
}

void Reader::ReadFileFromImage(DataFile* dataFile, tstring destinationFolder)
{
	if (NULL != dataFile)
	{
		uint32_t startFrame = dataFile->discAddress();
		uint32_t endFrame = startFrame + (uint32_t)(dataFile->size() / BlockSize::CDRom);
		if (dataFile->size() % BlockSize::CDRom)
			endFrame += 1;

		uint32_t currentBlockSize = BlockSize::CDRom;
		uint32_t blocksAtOnce = BufferSize::DefaultReadBufferBlocks;

		if (!dataFile->directory())
		{
			// Make the full path
			char_t destinationFilePath[MAX_PATH] = {0};
			_stprintf(destinationFilePath, _T("%s/%s"), destinationFolder.c_str(), dataFile->longFilename());

			// Open the files
			FILE * sourceFile = _tfopen(m_ImageFile.c_str(), _T("rb"));

			FILE * destinationFile = _tfopen(destinationFilePath, _T("wb"));

			// Copy the file from the image
			if (0 != destinationFile && 0 != sourceFile)
			{
				uint32_t numFrames = 0, frameSize = currentBlockSize;
				
				// allocate data buffer
				BYTE * buffer = new BYTE[frameSize * blocksAtOnce];

				// Seek to the correct offset in the image file
				fseek(sourceFile, startFrame * frameSize, SEEK_SET);
				
				// Rip the file
				while (startFrame < endFrame)
				{
					numFrames = std::min(blocksAtOnce, endFrame - startFrame);
					
					uint32_t len = (uint32_t)fread(buffer, sizeof(BYTE), frameSize * numFrames, sourceFile);
					if (0 == len)
						break;

					CallOnReadProgress(startFrame, numFrames, frameSize); 

					// Update buffer start address in frames (blocks)
					startFrame += len / frameSize;

					if (startFrame < endFrame)
						fwrite(buffer, sizeof(BYTE), len, destinationFile);
					else
					{
						// The real file size may not align on BlockSize::CDRom. 
						// We should not write the padding bytes
						uint32_t padding = BlockSize::CDRom - (uint32_t)(dataFile->size() % BlockSize::CDRom);
						if (BlockSize::CDRom == padding)
						{
							// if the remainder of (dataFile->GetFileSize() % BlockSize::CDRom) is 0 then there should be no padding
							padding = 0;
						}
						fwrite(buffer, sizeof(BYTE), len - padding, destinationFile);
					}
				}

				delete [] buffer;

				fflush(destinationFile);
				fclose(destinationFile);

				fclose(sourceFile);
			}
		}
		else
		{
			throw ReaderException(RE_ITEM_NOT_FILE, RE_ITEM_NOT_FILE_TEXT);
		}
	}
	else
	{
		throw ReaderException(RE_ITEM_NOT_FOUND, RE_ITEM_NOT_FOUND_TEXT);
	}
}

DataFile* Reader::FindItem(tstring& path)
{
	DataFile* item = NULL;
	CleanupDataDisc();
	m_DataDisc = Library::createDataDisc();

	if (!m_UseImage)
	{
		// Load image layout
		m_DataDisc->setDevice(m_Device);
		if (!m_DataDisc->loadFromDisc(m_TrackIndex))
		{
			throw ReaderException(m_DataDisc);
		}
	}
	else
	{
		// Load image layout
		if (!m_DataDisc->loadFromFile(m_ImageFile.c_str()))
		{
			throw ReaderException(m_DataDisc);
		}
	}

	// Get image layout in a tree structure of DataFile objects
	DataFile* root = m_DataDisc->copyImageLayout();

	if (0 == path.length() || 0 == path.compare(_T("/")) || 0 == path.compare(_T("\\")))
	{
		// only if the path parameter is an empty string would it be assumed that the root layout is
		// to be returned, otherwise it is assumed that the path parameter specifies a specific item
		// within the layout representd by the root object
		item = root;
	}
	else
	{
		item = root->find(path.c_str(), false);
		item->retain();

		root->release();
	}

	return item;
}


void Reader::RipCDTrack(Device* device, TrackRipSettings& settings)
{
	Toc * pToc = device->readToc();

	if (!pToc)
	{
		throw ReaderException(m_Device);
	}
	
	TocTrack *pTrack = pToc->tracks()->at(settings.get_TrackIndex() - 1);
	TocTrack *pNextTrack = pToc->tracks()->at(settings.get_TrackIndex());

	if (pTrack->isAudio())
	{
		pToc->release();
		throw ReaderException(RE_TRACK_IS_AUDIO, RE_TRACK_IS_AUDIO_TEXT);
	}


	//  Last entry is the start address of lead-out
	uint32_t startFrame = pTrack->address();
	uint32_t endFrame = pNextTrack->address();

	pToc->release();

	device->setReadSpeedKB(device->maxReadSpeedKB());

	// Detect the track type. 
	// Scan 100 blocks to see if the track is a mode2 form1, form2 or mixed mode track
	// This sample does not support mixed block tracks.
	TrackType::Enum tt = device->detectTrackType(startFrame, 100);
	if (TrackType::Mode2Mixed == tt || TrackType::Mode2Form1 == tt || TrackType::Mode2Form2 == tt)
	{
		CallShowMessage(_T("Mode 2, Form1, Form2 or Mixed form data detected.\n"));
	}

	// Determine the block size of the chosen track
	uint32_t blocksAtOnce = BufferSize::DefaultReadBufferBlocks;
	
	CDSector* cdSector = Library::createCDSector();
	uint32_t currentBlockSize = cdSector->getTrackBlockSize(tt);
	cdSector->release();

	char_t destinationFilePath[MAX_PATH];
	_stprintf(destinationFilePath, _T("%s/%s"), settings.get_DestinationFolder().c_str(), settings.get_UserDataFileName().c_str());
	FILE * file = _tfopen(destinationFilePath, _T("wb"));

	char_t destinationRawFilePath[MAX_PATH];
	_stprintf(destinationRawFilePath, _T("%s/%s"), settings.get_DestinationFolder().c_str(), settings.get_RawDataFileName().c_str());
	FILE * rawFile = _tfopen(destinationRawFilePath, _T("wb"));

	if (0 != file || 0 != rawFile)
	{
		char_t message[2 * MAX_PATH + 80];
		_stprintf(message,_T("Saving data image to %s and %s using %d byte blocks. ..\n"), destinationFilePath, destinationRawFilePath, currentBlockSize);
		CallShowMessage(message);

		bool deviceError = false;
		TrackBuffer *pBuffer = Library::createTrackBuffer(currentBlockSize, blocksAtOnce);
		while (startFrame < endFrame) 
		{
			uint32_t numberOfSectorsToRead = std::min(blocksAtOnce, endFrame - startFrame);
			if (!device->readData(pBuffer, startFrame, numberOfSectorsToRead))
			{
				deviceError = true;
				break;
			}

			CallOnReadProgress(startFrame, pBuffer->blocks(), pBuffer->blockSize());

			// Detect changes of block size
			if (currentBlockSize != pBuffer->blockSize())
			{
				CallOnBlockSizeChanged(currentBlockSize, pBuffer->blockSize(), startFrame);
				currentBlockSize = pBuffer->blockSize();
			}

			// Write just the user data
			WriteToFile(file, pBuffer);

			// Write the user data and the system information like EDC and ECC
			WriteToRawFile(rawFile, tt, pBuffer, startFrame);

			if ((int)numberOfSectorsToRead > pBuffer->blocks())
			{
				// At the end of the track several sectors might not be readable.
				// Such is the case with Track-At-Once tracks since the lead-out start there is
				// shifted with 2 blocks and that prevents some devices from reading all blocks
				// requested in buffer->dwNumFrames.
				break;
			}

			// Update buffer start address in frames (blocks)
			startFrame += pBuffer->blocks();
		}

		pBuffer->release();

		fflush(file);
		fflush(rawFile);

		fclose(file);
		fclose(rawFile);

		if (deviceError)
		{
			throw ReaderException(device);
		}
	}
	else
	{
		char_t message[2 * MAX_PATH + 30];
		_stprintf(message,_T("Cannot open %s or %s for writing.\n"), destinationFilePath, destinationRawFilePath);
		CallShowMessage(message);
	}
}

void Reader::RipDVDBDTrack(Device * device, TrackRipSettings& settings)
{
	char_t destinationFilePath[MAX_PATH];
	_stprintf(destinationFilePath, _T("%s/%s"), settings.get_DestinationFolder().c_str(), settings.get_UserDataFileName().c_str());
	FILE * file = _tfopen(destinationFilePath, _T("wb"));

	if (NULL == file)
	{
		char_t message[MAX_PATH + 30];
		_stprintf(message,_T("Cannot open %s for writing.\n"), destinationFilePath);
		CallShowMessage(message);
	}

	// Get track informations
	TrackInfoEx *pTi = device->readTrackInfoEx(settings.get_TrackIndex());
	if (NULL == pTi)
	{
		throw ReaderException(device);
	}

	device->setReadSpeedKB(device->maxReadSpeedKB());

	bool deviceError = false;
	TrackSegmentVector segments;

	BuildTrackSegmentList(device, pTi, segments);

	pTi->release();
	pTi = NULL;

	// Determine the block size of the chosen track
	uint32_t currentBlockSize = BlockSize::DVD;
	uint32_t blocksAtOnce = 32;

	char_t message[MAX_PATH + 80];
	_stprintf(message,_T("Saving data image to %s using %d byte blocks. ..\n"), destinationFilePath, currentBlockSize);
	CallShowMessage(message);

	TrackBuffer *pBuffer = Library::createTrackBuffer(currentBlockSize, blocksAtOnce);

	for (size_t pos = 0; pos < segments.size(); pos++)
	{
		TrackSegment segment = segments[pos];
		//  Last entry is the start address of lead-out
		uint32_t startFrame = segment.StartAddress;
		uint32_t endFrame = segment.EndAddress;

		while (startFrame < endFrame)
		{
			uint32_t numFrames = std::min(blocksAtOnce, endFrame - startFrame + 1);
			if (!device->readData(pBuffer, startFrame, numFrames))
			{
				deviceError = true;
				break;
			}

			CallOnReadProgress(startFrame, pBuffer->blocks(), pBuffer->blockSize()); 

			// Write the user data to a file
			WriteToFile(file, pBuffer);

			// Update buffer start address in frames (blocks)
			startFrame += pBuffer->blocks();
		}

		if (deviceError)
		{
			break;
		}
	}

	if (NULL != pBuffer)
	{
		pBuffer->release();
		pBuffer = NULL;
	}


	fflush(file);
	fclose(file);
	if (deviceError)
	{
		throw ReaderException(device);
	}
}

void Reader::WriteToFile(FILE * file, TrackBuffer *pBuffer)
{
	fwrite(pBuffer->buffer(), sizeof(BYTE), pBuffer->blocks() * pBuffer->blockSize(), file);
}

void Reader::WriteToRawFile(FILE * file, TrackType::Enum tt, TrackBuffer *pBuffer, int32_t startFrame)
{
	CDSector * cdSector = Library::createCDSector();

	if (NULL != cdSector)
	{
		BYTE rawBuf[BlockSize::CDDA];
		for (uint32_t dw = 0; dw < (uint32_t)(pBuffer->blocks()); dw++)
		{
			cdSector->encodeRawCDBlock(tt, startFrame +  dw, TRUE, rawBuf, pBuffer->buffer() + dw * pBuffer->blockSize(), pBuffer->blockSize());
			fwrite(rawBuf, sizeof(BYTE), BlockSize::CDDA, file);
		}

		cdSector->release();
	}
}

void Reader::CallOnReadProgress(uint32_t startFrame, uint32_t numberOfFrames, uint32_t frameSize)
{
	if (NULL != m_Callback)
	{
		m_Callback->OnReadProgress(startFrame, numberOfFrames, frameSize);
	}
}

void Reader::CallOnBlockSizeChanged(uint32_t currentBlockSize, uint32_t newBlockSize, uint32_t startFrame)
{
	if (NULL != m_Callback)
	{
		m_Callback->OnBlockSizeChange(currentBlockSize, newBlockSize, startFrame);
	}
}

void Reader::CallShowMessage(tstring message)
{
	if (NULL != m_Callback)
	{
		m_Callback->ShowMessage(message);
	}
}