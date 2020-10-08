using System;
using PrimoSoftware.Burner;

namespace DiscCopy.NET
{
	public enum MediaType
	{
		None = 0,
		CD = 1,
		DVD = 2,
		BD = 3,
	};

	// CreateImage Settings
	public class CreateImageSettings
	{
		public string ImageFolderPath = "";
		public bool ReadSubChannel = false;
	};


	// BurnImage Settings
	public class BurnImageSettings
	{
		public string ImageFolderPath = "";
		public CDCopyWriteMethod WriteMethod = CDCopyWriteMethod.CdCooked;
	};

	// Direct Copy Settings
	public class DirectCopySettings
	{
		public bool ReadSubChannel = false;
		public CDCopyWriteMethod WriteMethod = CDCopyWriteMethod.CdCooked;
		public bool UseTemporaryFiles = true;
	};

	// Media Clean Settings
	public enum CleanMethod
	{
		None = 0,
		Erase = 1,
		Format = 2,
	};

	public enum CopyMode
	{
		None = 0,
		Simple,
		Direct,
	}

	// Clean Media Settings
	public class CleanMediaSettings
	{
		public CleanMethod MediaCleanMethod = CleanMethod.None;
		public bool Quick = true;
	};

}
