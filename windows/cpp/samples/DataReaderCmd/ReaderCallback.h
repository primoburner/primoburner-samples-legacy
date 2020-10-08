#pragma once

#include "stdafx.h"

class ReaderCallback
{
public:

	virtual void OnReadProgress(uint32_t startFrame, uint32_t numberOfFrames, uint32_t frameSize)
	{
		_tprintf(_T("LBA: %d, Blocks: %d, Block Size: %d\n"), startFrame, numberOfFrames, frameSize); 
	}

	virtual void OnBlockSizeChange(uint32_t oldBlockSize, uint32_t newBlockSize, uint32_t startFrame)
	{
		_tprintf(_T("\t\tBlock size changed from %d to: %d  bytes at lba: %d.\n"), oldBlockSize, newBlockSize, startFrame);
	}

	virtual void ShowMessage(tstring message)
	{
		_tprintf(message.c_str());
	}
};