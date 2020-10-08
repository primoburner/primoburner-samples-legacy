using System;
using PrimoSoftware.Burner;

namespace DVDBurner.NET
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
		public const string FORMAT_NOT_SUPPORTED_TEXT = "Format is supported only for DVD-RW and DVD+RW media.";

		public const int FILE_NOT_FOUND = (-9);
		public const string FILE_NOT_FOUND_TEXT = "File not found while processing source folder.";

		public const int NO_WRITER_DEVICES = (-10);
		public const string NO_WRITER_DEVICES_TEXT = "No CD/DVD/BD writers are available.";
	}

	public class BurnerException : System.Exception
	{
		private string message;
        private int errorCode;
        private ErrorInfo errorInfo;

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

        public BurnerException(int errorCode, string errorMessage)
        {
            this.errorCode = errorCode;
            this.message = errorMessage;
        }

        public BurnerException(PrimoSoftware.Burner.ErrorInfo errorInfo)
        {
            if (errorInfo == null)
                return;

            this.errorInfo = (PrimoSoftware.Burner.ErrorInfo)errorInfo.Clone();

            switch (errorInfo.Facility)
            {
                case ErrorFacility.SystemWindows:
                    message = new System.ComponentModel.Win32Exception(errorInfo.Code).Message;
                    break;

                case ErrorFacility.Success:
                    message = "Success";
                    break;

                case ErrorFacility.DataDisc:
                    message = string.Format("DataDisc error: 0x{0:x8}: {1}", errorInfo.Code, errorInfo.Message);
                    break;

                case ErrorFacility.Device:
                    message = string.Format("Device error: 0x{0:x8}: {1}", errorInfo.Code, errorInfo.Message);
                    break;

                case ErrorFacility.VideoDVD:
                    message = string.Format("VideoDVD error: 0x{0:x8}: {1}", errorInfo.Code, errorInfo.Message);
                    break;

                default:
                    message = string.Format("Facility:{0} error :0x{1:x8}: {2}", errorInfo.Facility, errorInfo.Code, errorInfo.Message);
                    break;

            }
        }
	}

	

	
}
