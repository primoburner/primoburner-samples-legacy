#include "stdafx.h"

enum EAppOption
{
	AO_UNKNOWN = 0,		// mo burn option selected
	AO_DEVICE_LIST = 1,	// show device list
	AO_CLEAN = 2,		// clean(erase/format) medium
	AO_IMAGE = 3,		// indicates that an image should be written
	AO_WRITE = 4,		// indicates that a simple burning will be performed
	AO_PACKET = 5,		// indicates that a packet burning will be performed
};

enum EPacketBurningOption
{
	PBO_UNKNOWN = 0,	// no packet burning option selected
	PBO_START = 1,		// indicates that a new disc should be started
	PBO_APPEND = 2,		// indicates data should be appended to existing disc
	PBO_FINALIZE = 3,	// indicates the disc should be finalized
};

enum ESimpleBurnOption
{
	SBO_UNKNOWN = 0,
	SBO_OVERWRITE = 1,	// write the new data by hidinng any previous sessions on the medium
	SBO_MERGE = 2,		// merge the new layout with the one from the last completed track on the medium
};

class SimpleBurnSettings
{
public:
	SimpleBurnSettings(tstring folderSrc, ESimpleBurnOption option)
	{
		m_FolderSrc = folderSrc;
		m_VolumeLabel = _T("SIMPLE_BACKUP");
		m_ImageType = ImageTypeFlags::Joliet;
		m_Option = option;
	}
	virtual ~SimpleBurnSettings()
	{
	}

public:
	tstring get_FolderSrc() const
	{
		return m_FolderSrc;
	}
	tstring get_VolumeLabel() const
	{
		return m_VolumeLabel;
	}

	ImageTypeFlags::Enum get_ImageType() const
	{
		return m_ImageType;
	}

	ESimpleBurnOption get_Option() const
	{
		return m_Option;
	}

private:
	tstring m_FolderSrc;
	tstring m_VolumeLabel;
	ImageTypeFlags::Enum m_ImageType;
	ESimpleBurnOption m_Option;
};
class ImageBurnSettings
{
public:
	ImageBurnSettings(tstring imageFile)
	{
		m_ImageFile = imageFile;
	}
	virtual ~ImageBurnSettings()
	{
	}

public:
	tstring get_ImageFile() const
	{
		return m_ImageFile;
	}

private:
	tstring m_ImageFile;
};

class PacketBurnSettings
{
public:
	PacketBurnSettings(tstring folderSrc, EPacketBurningOption option)
	{
		m_FolderSrc = folderSrc;
		m_VolumeLabel = _T("PRIMOBURNERDISC");
		m_ImageType = ImageTypeFlags::Udf;
		m_Option = option;
		
	}
	virtual ~PacketBurnSettings()
	{
	}

public:
	tstring get_FolderSrc() const
	{
		return m_FolderSrc;
	}
	tstring get_VolumeLabel() const
	{
		return m_VolumeLabel;
	}

	ImageTypeFlags::Enum get_ImageType() const
	{
		return m_ImageType;
	}

	EPacketBurningOption get_Option() const
	{
		return m_Option;
	}


private:
	tstring m_FolderSrc;
	tstring m_VolumeLabel;
	ImageTypeFlags::Enum m_ImageType;
	EPacketBurningOption m_Option;
};
struct AppFunctionality
{
	tstring		LayoutSrc;
	int32_t		DeviceIndex;

	EAppOption AppOption;
	EPacketBurningOption PacketOption;
	ESimpleBurnOption SimpleOption;
};
