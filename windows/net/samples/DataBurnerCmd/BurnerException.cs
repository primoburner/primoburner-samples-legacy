using System;
using System.Collections.Generic;
using System.Text;

using PrimoSoftware.Burner;

namespace DataBurnerCmd.NET
{
	enum BurnerErrors
	{
		BurnerNotOpen = 0,
		DeviceNotSet,
		NoDevices,
		PBEngineNull,
		PBDeviceEnumNull,
		PBDeviceNull,
		PBDataDiscNull,
	};

	class BurnerErrorMessages
	{
		public const string NoDevices = "No device were found on the machine";
		public const string BurnerNotOpen = "The Burner object is not initialized";
		public const string DeviceNotSet = "No device is selected";

		public const string EngineNull = "Engine object is null";
		public const string DeviceEnumNull = "DeviceEnum object is null";
		public const string DeviceNull = "Device object is null";
		public const string DataDiscNull = "DataDisc object is null";
	}

	enum ErrorProvider
	{
		None = 0,
		Burner,
		System,
		Engine,
		DeviceEnum,
		Device,
		DataDisc,	
	};

	class BurnerException : Exception
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

		public BurnerException()
		{
			m_Error			= 0;
			m_Message		= string.Empty;
			m_Provider		= ErrorProvider.None;
		}

		public BurnerException(Engine engine)
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
				m_Provider = ErrorProvider.Burner;
				m_Error = (int)BurnerErrors.PBEngineNull;
				m_Message = BurnerErrorMessages.EngineNull;
			}
		}

		public BurnerException(DeviceEnumerator deviceEnum)
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
				m_Provider = ErrorProvider.Burner;
				m_Error = (int)BurnerErrors.PBDeviceEnumNull;
				m_Message = BurnerErrorMessages.DeviceEnumNull;
			}
		}

		public BurnerException(Device device)
		{
			InitializeDeviceError(device);
		}

		public BurnerException(DataDisc dataDisc, Device device)
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
				m_Provider = ErrorProvider.Burner;
				m_Error = (int)BurnerErrors.PBDataDiscNull;
				m_Message = BurnerErrorMessages.DataDiscNull;
			}
		}

		public BurnerException(BurnerErrors error, string message, ErrorProvider provider)
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
				m_Provider = ErrorProvider.Burner;
				m_Error = (int)BurnerErrors.PBDeviceNull;
				m_Message = BurnerErrorMessages.DeviceNull;
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
