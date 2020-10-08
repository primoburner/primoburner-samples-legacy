using System;
using System.Collections.Generic;
using System.Text;

using PrimoSoftware.Burner;

namespace DataBurnerCmd.NET
{
	enum AppOption
	{
		Unknown = 0,	// mo burn option selected
		DeviceList,		// show device list
		Clean,			// clean(erase/format) medium
		Image,			// indicates that an image should be written
		Write,			// indicates that a simple burning will be performed
		Packet,			// indicates that a packet burning will be performed
	};

	enum PacketBurnOption
	{
		Unknown = 0,	// no packet burning option selected
		Start,			// indicates that a new disc should be started
		Append,			// indicates data should be appended to existing disc
		Finalize,		// indicates the disc should be finalized
	};

	enum SimpleBurnOption
	{
		Unknown = 0,
		Overwrite,		// write the new data by hidinng any previous sessions on the medium
		Merge,			// merge the new layout with the one from the last completed track on the medium
	};

	class SimpleBurnSettings
	{
		public SimpleBurnSettings(string folderSrc, SimpleBurnOption option)
		{
			m_FolderSrc = folderSrc;
			m_VolumeLabel = "SIMPLE_BACKUP";
			m_ImageType = ImageType.Joliet;
			m_Option = option;
		}

		public string FolderSrc
		{
			get { return m_FolderSrc; }
		}
		public string VolumeLabel
		{
			get { return m_VolumeLabel; }
		}

		public ImageType ImageType
		{
			get { return m_ImageType; }
		}

		public SimpleBurnOption Option
		{
			get { return m_Option; }
		}

		private string m_FolderSrc;
		private string m_VolumeLabel;
		private ImageType m_ImageType;
		private SimpleBurnOption m_Option;
	};

	class ImageBurnSettings
	{
		public ImageBurnSettings(string imageFile)
		{
			m_ImageFile = imageFile;
		}

		public string ImageFile
		{
			get { return m_ImageFile; }
		}

		private string m_ImageFile;
	};

	class PacketBurnSettings
	{
		public PacketBurnSettings(string folderSrc, PacketBurnOption option)
		{
			m_FolderSrc = folderSrc;
			m_VolumeLabel = "HPCDEDISC";
			m_ImageType = ImageType.Udf;
			m_Option = option;
		}

		public string FolderSrc
		{
			get { return m_FolderSrc; }
		}
		public string VolumeLabel
		{
			get { return m_VolumeLabel; }
		}

		public ImageType ImageType
		{
			get { return m_ImageType; }
		}

		public PacketBurnOption Option
		{
			get { return m_Option; }
		}

		private string m_FolderSrc;
		private string m_VolumeLabel;
		private ImageType m_ImageType;
		private PacketBurnOption m_Option;
	};

	class AppFunctionality
	{
		private string m_LayoutSrc;
		private int m_DeviceIndex;

		private AppOption m_AppOption;
		private PacketBurnOption m_PacketOption;
		private SimpleBurnOption m_SimpleOption;

		public string LayoutSrc
		{
			get{return m_LayoutSrc;}
		}

		public int DeviceIndex
		{
			get{return m_DeviceIndex;}
		}

		public AppOption AppOption
		{
			get{return m_AppOption;}
		}

		public PacketBurnOption PacketOption
		{
			get{return m_PacketOption;}
		}

		public SimpleBurnOption SimpleOption
		{
			get{return m_SimpleOption;}
		}

		public AppFunctionality()
		{
			m_LayoutSrc = string.Empty;
			m_DeviceIndex = 0;

			m_AppOption = AppOption.Unknown;
			m_PacketOption = PacketBurnOption.Unknown ;
			m_SimpleOption = SimpleBurnOption.Unknown;
		}
		public AppFunctionality(AppOption appOption, PacketBurnOption packetOption, SimpleBurnOption simpleOption, int deviceIndex, string layoutSrc)
		{
			m_LayoutSrc = layoutSrc;
			m_DeviceIndex = deviceIndex;

			m_AppOption = appOption;
			m_PacketOption = packetOption;
			m_SimpleOption = simpleOption;
		}
	};
}
