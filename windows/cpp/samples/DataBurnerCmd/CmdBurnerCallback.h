#pragma once

#include "BurnerCallback.h"

class CmdBurnerCallback : public IBurnerCallback
{
public:

	virtual bool OnContinue()
	{
		return true;
	}

	virtual void OnFileProgress(int file, const tstring& fileName, int percentCompleted)
	{
		_tprintf(_T("\r\n %d. File: %s - Percent complete: %d%%"), file, fileName.c_str(), percentCompleted);
	}

	virtual void OnProgress(DWORD64 ddwPos, DWORD64 ddwAll)
	{
		_tprintf(_T("\r\n OnProgress: %d%%  ddwPos=%I64u ddwAll=%I64u"),  int(double(100.0 * (__int64)ddwPos) / (__int64)ddwAll), ddwPos, ddwAll);
	}

	virtual void OnStatus(const tstring& message)
	{
		_tprintf(_T("\r\n %s"), message.c_str());
	}

	
	virtual void OnFormatProgress(double percentCompleted)
	{
		_tprintf(_T("\r\n Format completed at %f%%"), percentCompleted);
	}
	virtual void OnEraseProgress(double percentCompleted)
	{
		_tprintf(_T("\r\n Erase completed at %f%%"), percentCompleted);
	}
};