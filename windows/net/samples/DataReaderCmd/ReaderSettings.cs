using System;

namespace DataReaderCmd.NET
{

	enum AppOption
	{
		Unknown = 0,				// no burn option selected
		DeviceList = 1,				// show device list	- option '-l'
		DeviceTrackList = 2,		// show list of tracks available on the medium mounted in the specified device	- option '-t'
		ViewContent = 3,			// display content of track/image layout item - option '-v'
		ReadContent = 4,			// read content(track/file) from source(device/image layout) - option '-r'
	};

	enum SourceType
	{
		Unknown = 0,				// source is not selected
		DiscTrackUserData = 1,		// source is a medium track - the user data content is to be read
		ImageLayout = 2,			// source is an image file - load its layout in DataDisc object
		DiscTrackLayout = 3,		// source is a medium track - load its layout in DataDisc object
	};

	class TrackRipSettings
	{
		// Index of device to read from
		private int m_DeviceIndex;
		// Index of disc track to read from
		private int m_TrackIndex;
		// Path to the folder to save the track data files to
		private string m_DestinationFolder;
		// Name of the file to write track user data to
		private string m_UserDataFileName;
		// Name of the file to write raw track data to (used for CDs only)
		private string m_RawDataFileName;

		public int DeviceIndex
		{
			get { return m_DeviceIndex; }
		}
		public int TrackIndex
		{
			get { return m_TrackIndex; }
		}
		public string DestinationFolder
		{
			get { return m_DestinationFolder; }
		}
		public string UserDataFileName
		{
			get { return m_UserDataFileName; }
		}
		public string RawDataFileName
		{
			get { return m_RawDataFileName; }
		}

		public TrackRipSettings(int deviceIndex, int trackIndex, string destinationFolder)
		{
			m_DeviceIndex = deviceIndex;
			m_TrackIndex = trackIndex;
			m_DestinationFolder = destinationFolder;
			m_UserDataFileName = "data.bin";
			m_RawDataFileName = "data_raw.bin";
		}

	};

	class AppFunctionality
	{
		private int m_DeviceIndex;
		private int m_TrackIndex;
		private string m_ImageSource;
		private string m_ItemPath;
		private string m_DestinationFolder;
		private SourceType m_SourceType;
		private AppOption m_AppOption;

		public int DeviceIndex
		{
			get { return m_DeviceIndex; }
		}
		public int TrackIndex
		{
			get { return m_TrackIndex; }
		}
		public string ImageSource
		{
			get { return m_ImageSource; }
		}
		public string ItemPath
		{
			get { return m_ItemPath; }
		}
		public string DestinationFolder
		{
			get { return m_DestinationFolder; }
		}
		public SourceType SourceType
		{
			get { return m_SourceType; }
		}
		public AppOption AppOption
		{
			get { return m_AppOption; }
		}

		public AppFunctionality(AppOption appOption, int deviceIndex, int trackIndex, string imageSource,
			string itemPath, string destinationFolder, SourceType sourceType)
		{
			m_DeviceIndex = deviceIndex;
			m_TrackIndex = trackIndex;
			m_ImageSource = imageSource;
			m_ItemPath = itemPath;
			m_DestinationFolder = destinationFolder;
			m_SourceType = sourceType;
			m_AppOption = appOption;
		}
	};
}