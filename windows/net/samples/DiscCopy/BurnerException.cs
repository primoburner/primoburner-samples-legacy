using System;
using PrimoSoftware.Burner;

namespace DiscCopy.NET
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

		public const int ERASE_NOT_SUPPORTED = (-7);
		public const string ERASE_NOT_SUPPORTED_TEXT = "Erasing is supported only for CD-RW and DVD-RW media.";

		public const int FORMAT_NOT_SUPPORTED = (-8);
		public const string FORMAT_NOT_SUPPORTED_TEXT = "Format is supported only for DVD-RW, DVD-RAM, DVD+RW and BD-RE media.";

		public const int NO_WRITER_DEVICES = (-10);
		public const string NO_WRITER_DEVICES_TEXT = "No CD/DVD/BD writers are available.";

		public const int DEVICE_NOT_READY = (-11);
		public const string DEVICE_NOT_READY_TEXT = "Device not ready.";

		public const int MEDIA_TYPE_NOT_SUPPORTED = (-12);
		public const string MEDIA_TYPE_NOT_SUPPORTED_TEXT = "Media type not supported for disc copy.";

		public const int MEDIA_NOT_REWRITABLE = (-30);
		public const string MEDIA_NOT_REWRITABLE_TEXT = "Medium not rewritable";
	}

	public class BurnerException : System.Exception
	{
		protected int error;

		protected string message;

		public int Error
		{
			get { return error; }
		}

		public override string Message { get { return message; } }

		protected BurnerException()
		{
			error = 0;
			message = "No error.";
		}

		protected BurnerException(int _error)
		{
			error = _error;

			switch (error)
			{
				case BurnerErrors.BURNER_NOT_OPEN:
					message = BurnerErrors.BURNER_NOT_OPEN_TEXT;
					break;
				case BurnerErrors.DEVICE_ALREADY_SELECTED:
					message = BurnerErrors.DEVICE_ALREADY_SELECTED_TEXT;
					break;
				case BurnerErrors.DEVICE_NOT_READY:
					message = BurnerErrors.DEVICE_NOT_READY_TEXT;
					break;
				case BurnerErrors.ENGINE_INITIALIZATION:
					message = BurnerErrors.ENGINE_INITIALIZATION_TEXT;
					break;
				case BurnerErrors.ERASE_NOT_SUPPORTED:
					message = BurnerErrors.ERASE_NOT_SUPPORTED_TEXT;
					break;
				case BurnerErrors.FORMAT_NOT_SUPPORTED:
					message = BurnerErrors.FORMAT_NOT_SUPPORTED_TEXT;
					break;
				case BurnerErrors.INVALID_DEVICE_INDEX:
					message = BurnerErrors.INVALID_DEVICE_INDEX_TEXT;
					break;
				case BurnerErrors.MEDIA_NOT_REWRITABLE:
					message = BurnerErrors.MEDIA_NOT_REWRITABLE_TEXT;
					break;
				case BurnerErrors.MEDIA_TYPE_NOT_SUPPORTED:
					message = BurnerErrors.MEDIA_TYPE_NOT_SUPPORTED_TEXT;
					break;
				case BurnerErrors.NO_DEVICE:
					message = BurnerErrors.NO_DEVICE_TEXT;
					break;
				case BurnerErrors.NO_DEVICES:
					message = BurnerErrors.NO_DEVICES_TEXT;
					break;
				case BurnerErrors.NO_WRITER_DEVICES:
					message = BurnerErrors.NO_WRITER_DEVICES_TEXT;
					break;
			}
		}

		public static BurnerException CreateDiscCopyException(Device srcDevice, Device dstDevice, PrimoSoftware.Burner.DiscCopy discCopy)
		{
			if (null != discCopy)
			{
                ErrorInfo error = discCopy.Error;
				switch (error.Facility)
				{
                    case ErrorFacility.Device:
                        {
                            if (null != srcDevice)
                                if (srcDevice.Error.Facility != ErrorFacility.Success)
    						        return CreateDeviceException(srcDevice, true);

                            if (null != dstDevice)
                                if (dstDevice.Error.Facility != ErrorFacility.Success)
                                    return CreateDeviceException(dstDevice, false);
                        }
                        break;

					case ErrorFacility.SystemWindows:
						return CreateSystemException(error.Code);

					default:
						return new BurnerDiscCopyException(discCopy);
				}
			}

			return new BurnerException();
		}

		public static BurnerException CreateDeviceException(Device device, bool sourceDevice)
		{
			if (null != device)
			{
                ErrorInfo error = device.Error;
				switch (error.Facility)
				{
					case ErrorFacility.SystemWindows:
						return CreateSystemException(error.Code);

					default:
						return new BurnerDeviceException(device, sourceDevice);
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

	public class BurnerDiscCopyException : BurnerException
	{
		public BurnerDiscCopyException(PrimoSoftware.Burner.DiscCopy discCopy)
		{
			if (null != discCopy)
			{
				error = discCopy.Error.Code;
                message = string.Format("DiscCopy error detected:{0}\t0x{1:x8}{0}\t{2}", System.Environment.NewLine, error, discCopy.Error.Message);
			}
		}
	}

	public class BurnerDeviceException : BurnerException
	{
		public BurnerDeviceException(Device device, bool sourceDevice)
		{
			if (null != device)
			{
				error = device.Error.Code;
				if (sourceDevice)
					message = string.Format("Source device error detected:{0}\t0x{1:x8}{0}\t{2}", System.Environment.NewLine, error, device.Error.Message);
				else
					message = string.Format("Destination device error detected:{0}\t0x{1:x8}{0}\t{2}", System.Environment.NewLine, error, device.Error.Message);
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
