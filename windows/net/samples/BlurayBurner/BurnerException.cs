using System;
using PrimoSoftware.Burner;

namespace BluRayBurner.NET
{
	public class BurnerErrors
	{
		public const int ENGINE_INITIALIZATION = (-1);
		public const string ENGINE_INITIALIZATION_TEXT = "PrimoBurner engine initialization error.";

		public const int BURNER_NOT_OPEN = (-2);
		public const string BURNER_NOT_OPEN_TEXT = "Burner not open.";

		public const int NO_DEVICES = (-3);
		public const string NO_DEVICES_TEXT = "No CD/DVD/BD devices are available.";

		public const int NO_DEVICE = (-4);
		public const string NO_DEVICE_TEXT = "No device selected.";

		public const int DEVICE_ALREADY_SELECTED = (-5);
		public const string DEVICE_ALREADY_SELECTED_TEXT = "Device already selected.";

		public const int INVALID_DEVICE_INDEX = (-6);
		public const string INVALID_DEVICE_INDEX_TEXT = "Invalid device index.";

		public const int FORMAT_NOT_SUPPORTED = (-8);
		public const string FORMAT_NOT_SUPPORTED_TEXT = "Format is supported only for DVD-RW, DVD+RW media, BD-RE and BD-R for Sequential Recording.";
	}

	public class BurnerException : System.Exception
	{
        protected string message;
        protected int errorCode;
        protected ErrorInfo errorInfo;

        public int ErrorCode
        {
            get
            {
                if (errorInfo != null)
                    return errorInfo.Code;

                return errorCode;
            }
        }

		public override string Message { get { return message; } }

		protected BurnerException()
		{
			errorCode = 0;
			message = "No error.";
		}

		protected BurnerException(int burnerError)
		{
			errorCode = burnerError;

			switch (burnerError)
			{
				case BurnerErrors.NO_DEVICE:
					message = BurnerErrors.NO_DEVICE_TEXT;
					break;
				case BurnerErrors.ENGINE_INITIALIZATION:
					message = BurnerErrors.ENGINE_INITIALIZATION_TEXT;
					break;
				case BurnerErrors.BURNER_NOT_OPEN:
					message = BurnerErrors.BURNER_NOT_OPEN_TEXT;
					break;
				case BurnerErrors.NO_DEVICES:
					message = BurnerErrors.NO_DEVICES_TEXT;
					break;
				case BurnerErrors.DEVICE_ALREADY_SELECTED:
					message = BurnerErrors.DEVICE_ALREADY_SELECTED_TEXT;
					break;
				case BurnerErrors.INVALID_DEVICE_INDEX:
					message = BurnerErrors.INVALID_DEVICE_INDEX_TEXT;
					break;
				case BurnerErrors.FORMAT_NOT_SUPPORTED:
					message = BurnerErrors.FORMAT_NOT_SUPPORTED_TEXT;
					break;
			}
		}

		public static BurnerException CreateDataDiscException(DataDisc dataDisc, Device device)
		{
			if (null != dataDisc)
			{
                ErrorInfo error = dataDisc.Error;

                switch (error.Facility)
				{
                    case ErrorFacility.SystemWindows:
                        return CreateSystemException(error.Code);
                    case ErrorFacility.Device:
						return CreateDeviceException(device);
					default:
						return new BurnerDataDiscException(dataDisc);
				}
			}
			return new BurnerException();
		}

		public static BurnerException CreateDeviceException(Device device)
		{
			if (null != device)
			{
                ErrorInfo error = device.Error;

                switch (error.Facility)
				{
                    case ErrorFacility.SystemWindows:
						return CreateSystemException(error.Code);
					default:
						return new BurnerDeviceException(device);
				}
			}

			return new BurnerException();
		}

		public static BurnerException CreateSystemException(int systemError)
		{
			return new BurnerSystemException(systemError);
		}

		public static BurnerException CreateBurnerException(int burnerError)
		{
			return new BurnerException(burnerError);
		}
	}

	public class BurnerDataDiscException : BurnerException
	{
		public BurnerDataDiscException(DataDisc dataDisc)
		{
			if (null != dataDisc)
			{
				errorInfo = dataDisc.Error;
                message = string.Format("DataDisc error detected:{0}\t0x{1:x8}{0}\t{2}", System.Environment.NewLine, errorCode, errorInfo.Code, errorInfo.Message);
			}
		}
	}

	public class BurnerDeviceException : BurnerException
	{
		public BurnerDeviceException(Device device)
		{
			if (null != device)
			{
				errorInfo = device.Error;
                message = string.Format("Device error detected:{0}\t0x{1:x8}{0}\t{2}", System.Environment.NewLine, errorInfo.Code, errorInfo.Message);
			}
		}
	}

	public class BurnerSystemException : BurnerException
	{
		public BurnerSystemException(int systemError)
			: base(systemError)
		{
			message = new System.ComponentModel.Win32Exception(systemError).Message;
		}
	}
}
