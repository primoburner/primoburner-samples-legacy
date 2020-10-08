#pragma once

class IBurnerCallback
{
public:

	virtual bool OnContinue() = 0;
	virtual void OnFileProgress(int file, const tstring& fileName, int percentCompleted) = 0;
	virtual void OnProgress(DWORD64 ddwPos, DWORD64 ddwAll) = 0;
	virtual void OnStatus(const tstring& message) = 0;

	virtual void OnFormatProgress(double percentCompleted) = 0;
	virtual void OnEraseProgress(double percentCompleted) = 0;

};
