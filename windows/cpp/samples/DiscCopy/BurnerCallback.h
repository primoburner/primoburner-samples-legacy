#pragma once

class IBurnerCallback
{
public:
	virtual void OnStatus(const tstring& message) = 0;

	virtual void OnCopyProgress(DWORD64 ddwPos, DWORD64 ddwAll) = 0; 
	virtual void OnTrackStatus(int nTrack, int nPercent) = 0;

	virtual void OnFormatProgress(double percentCompleted) = 0;
	virtual void OnEraseProgress(double percentCompleted) = 0;

	virtual bool OnContinue() = 0;
};
