using System;
using PrimoSoftware.Burner;

namespace AudioBurner.NET
{
	public class BurnerErrors
	{
		#region Error Definitions
		// User Errors
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

		public const int NO_WRITER_DEVICES = (-10);
		public const string NO_WRITER_DEVICES_TEXT = "No CD/DVD/BD writers are available.";

		public const int NO_AUDIO_TRACKS = -11;
		public const string NO_AUDIO_TRACKS_TEXT = "No Audio tracks detected.";

		public const int INVALID_RECORDING_MODE = -16;
		public const string INVALID_RECORDING_MODE_TEXT = "The selected write method is not valid.";

		#endregion
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

        private PrimoSoftware.Burner.ErrorInfo errorInfo;

        protected BurnerException()
        {
            error = 0;
            message = "No error.";
        }

        public BurnerException(int burnerError)
        {
            error = burnerError;

            switch (burnerError)
            {
                case BurnerErrors.NO_DEVICE:
                    message = BurnerErrors.NO_DEVICE_TEXT;
                    break;
                case BurnerErrors.NO_WRITER_DEVICES:
                    message = BurnerErrors.NO_WRITER_DEVICES_TEXT;
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
                case BurnerErrors.NO_AUDIO_TRACKS:
                    message = BurnerErrors.NO_AUDIO_TRACKS_TEXT;
                    break;
                case BurnerErrors.INVALID_RECORDING_MODE:
                    message = BurnerErrors.INVALID_RECORDING_MODE_TEXT;
                    break;
                case BurnerErrors.ERASE_NOT_SUPPORTED:
                    message = BurnerErrors.ERASE_NOT_SUPPORTED_TEXT;
                    break;
            }
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

                case ErrorFacility.AudioCD:
                    message = string.Format("AudioCD error: 0x{0:x8}: {1}", errorInfo.Code, errorInfo.Message);
                    break;

                case ErrorFacility.Device:
                    message = string.Format("Device error: 0x{0:x8}: {1}", errorInfo.Code, errorInfo.Message);
                    break;

                default:
                    message = string.Format("Facility:{0} error :0x{1:x8}: {2}", errorInfo.Facility, errorInfo.Code, errorInfo.Message);
                    break;

            }
        }
    }
}