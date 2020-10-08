#pragma once

enum EMediaType
{
	MT_None = 0,
	MT_CD = 1,
	MT_DVD = 2,
	MT_BD = 3,
};

// CreateImage Settings
struct CreateImageSettings
{
	tstring					ImageFolderPath;
	bool					ReadSubChannel;

	CreateImageSettings()
	{
		ReadSubChannel = false;
	}
};

// BurnImage Settings
struct BurnImageSettings
{
	tstring					ImageFolderPath;
	CDCopyWriteMethod::Enum	WriteMethod;

	BurnImageSettings()
	{
		WriteMethod	= CDCopyWriteMethod::Cooked;
	}
};

struct DirectCopySettings
{
	bool					ReadSubChannel;
	CDCopyWriteMethod::Enum	WriteMethod;
	bool					UseTemporaryFiles;

	DirectCopySettings()
	{
		ReadSubChannel = false;
		UseTemporaryFiles = true;
	}
};

enum ECleanMethod
{
	CM_None = 0,
	CM_Erase = 1,
	CM_Format = 2,
};

struct CleanMediaSettings
{
	ECleanMethod			MediaCleanMethod;
	BOOL					Quick;

	CleanMediaSettings()
	{
		MediaCleanMethod = CM_None;
		Quick = TRUE;
	}
};