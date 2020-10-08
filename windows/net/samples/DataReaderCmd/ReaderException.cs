using System;
using PrimoSoftware.Burner;

namespace DataReaderCmd.NET
{
	enum ReaderError
	{
		ReaderNotOpen = 0,
		DeviceNotSet,
		NoDevices,
		ItemNotFound,
		ItemNotFolder,
		ItemNotFile,
		NoTracksOnDisc,
		TrackLRAInvalid,
		TrackIsAudio,
		PBEngineNull,
		PBDeviceEnumNull,
		PBDataDiscNull,
		PBDeviceNull,
	};

	enum ErrorProvider
	{
		None = 0,
		Reader,
		System,
		Engine,
		DeviceEnum,
		Device,
		DataDisc,
	};

	class ReaderErrorMessages
	{
		public const string NoDevices = "No device were found on the machine";
		public const string ReaderNotOpen = "The Reader object is not initialized";
		public const string DeviceNotSet = "No device is selected";
		public const string ItemNotFound = "No such file or folder exists at the specified location";
		public const string ItemNotFolder = "The specified item is not a folder";
		public const string ItemNotFile = "The specified item is not a file";
		public const string NoTracksOnDisc = "No tracks found on the medium";
		public const string TrackLRAInvalid = "Track's last recorded address is not valid";
		public const string TrackIsAudio = "The specified track is an audio track - such tracks are not read from";
		public const string EngineNull = "Engine object is null";
		public const string DeviceEnumNull = "DeviceEnum object is null";
		public const string DeviceNull = "Device object is null";
		public const string DataDiscNull = "DataDisc object is null";
	};

	class ReaderException : Exception
	{
		protected string m_Message;
		protected int m_Error;
		protected ErrorProvider m_Provider;

		public override string Message
		{
			get { return m_Message; }
		}

		public int Error
		{
			get { return m_Error; }
		}


		public ErrorProvider Provider
		{
			get { return m_Provider; }
		}

		public ReaderException()
		{
			m_Error			= 0;
			m_Message		= string.Empty;
			m_Provider		= ErrorProvider.None;
		}

		public ReaderException(Engine engine)
		{
			if (null != engine)
			{
                ErrorInfo error = engine.Error;

                m_Error = error.Code;
				if (ErrorFacility.SystemWindows == error.Facility)
				{
					InitializeSystemError(error.Code);
				}
				else
				{
					m_Provider = ErrorProvider.Engine;
					m_Message = error.Message;
				}
			}
			else
			{
				m_Provider = ErrorProvider.Reader;
				m_Error = (int)ReaderError.PBEngineNull;
				m_Message = ReaderErrorMessages.EngineNull;
			}
		}

		public ReaderException(DeviceEnumerator deviceEnum)
		{
			if (null != deviceEnum)
			{
                ErrorInfo error = deviceEnum.Error;

                m_Error = error.Code;
                if (ErrorFacility.SystemWindows == error.Facility)
                {
                    InitializeSystemError(error.Code);
                }
				else
				{
					m_Provider = ErrorProvider.DeviceEnum;
					m_Message = error.Message;
				}
			}
			else
			{
				m_Provider = ErrorProvider.Reader;
				m_Error = (int)ReaderError.PBDeviceEnumNull;
				m_Message = ReaderErrorMessages.DeviceEnumNull;
			}
		}

		public ReaderException(Device device)
		{
			InitializeDeviceError(device);
		}

		public ReaderException(DataDisc dataDisc, Device device)
		{
			if (null != dataDisc)
			{
                ErrorInfo error = dataDisc.Error;

                m_Error = error.Code;
				switch (error.Facility)
				{
					case ErrorFacility.SystemWindows:
						InitializeSystemError(error.Code);
						break;
					case ErrorFacility.Device:
						InitializeDeviceError(device);
						break;
					default:
						m_Provider = ErrorProvider.DataDisc;
						m_Message = error.Message;
						break;
				}
			}
			else
			{
				m_Provider = ErrorProvider.Reader;
				m_Error = (int)ReaderError.PBDataDiscNull;
				m_Message = ReaderErrorMessages.DataDiscNull;
			}
		}

		public ReaderException(ReaderError error, string message, ErrorProvider provider)
		{
			m_Error			= (int)error;
			m_Message		= message;
			m_Provider		= provider;
		}

		protected void InitializeDeviceError(Device device)
		{
			if (null != device)
			{
                ErrorInfo error = device.Error;

                m_Error = error.Code;
                if (ErrorFacility.SystemWindows == error.Facility)
                {
                    InitializeSystemError(error.Code);
                }
                else
				{
					m_Provider = ErrorProvider.Device;
					m_Message = error.Message;
				}
			}
			else
			{
				m_Provider = ErrorProvider.Reader;
				m_Error = (int)ReaderError.PBDeviceNull;
				m_Message = ReaderErrorMessages.DeviceNull;
			}
		}

		protected void InitializeSystemError(int error)
		{
			m_Error		= error;
			m_Provider	= ErrorProvider.System;
			m_Message	= BuildSystemErrorMessage(m_Error);
		}

		private string BuildSystemErrorMessage(int systemError)
		{
			System.ComponentModel.Win32Exception ex = new System.ComponentModel.Win32Exception (systemError);
			return ex.Message;
		}
	};
}
