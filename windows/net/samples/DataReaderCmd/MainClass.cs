using System;
using System.Collections.Generic;
using System.IO;

using PrimoSoftware.Burner;

namespace DataReaderCmd.NET
{
	class Program
	{
		private const int NoDeviceIndex = -1;
		private const int NoTrackIndex = -1;
		[STAThread]
		static void Main(string[] args)
		{
			Application app = new Application();
			app.Run(ParseCommandLine(args));
		}

		static AppFunctionality ParseCommandLine(string[] args)
		{
			string source = string.Empty;
			string imageSource = string.Empty;
			string itemPath = string.Empty;
			string destinationFolder = string.Empty;
			int deviceIndex = NoDeviceIndex;
			int trackIndex = NoTrackIndex;
			AppOption appOption = AppOption.Unknown;
			SourceType sourceType = SourceType.Unknown;

			for (int i = 0; i < args.Length; i++)
			{
				string argument = args[i];
				if (argument.Equals("-l"))
				{
					if (AppOption.Unknown != appOption || i + 1 < args.Length)
					{
						Usage();
						return null;
					}

					appOption = AppOption.DeviceList;
				}

				else if (argument.Equals("-t"))
				{
					if (AppOption.Unknown != appOption)
					{
						Usage();
						return null;
					}

					appOption = AppOption.DeviceTrackList;
				}

				else if (argument.Equals("-r"))
				{
					if (AppOption.Unknown != appOption)
					{
						Usage();
						return null;
					}

					appOption = AppOption.ReadContent;
				}
				else if (argument.Equals("-v"))
				{
					if (AppOption.Unknown != appOption)
					{
						Usage();
						return null;
					}

					appOption = AppOption.ViewContent;
				}

				else if (argument.Equals("/s"))
				{
					// if option other than read and view was already selected or if the source is already defined, break execution and show usage details
					if ((AppOption.ReadContent != appOption && AppOption.ViewContent != appOption) || 0 < source.Length)
					{
						Usage();
						return null;
					}

					i++;
					if (i == args.Length)
					{
						Usage();
						return null;
					}
					source = args[i];
				}
				else if (argument.Equals("/i"))
				{
					if (appOption != AppOption.DeviceTrackList)
					{
						Usage();
						return null;
					}
					i++;
					if (i == args.Length)
					{
						Usage();
						return null;
					}
					if (!int.TryParse(args[i], out deviceIndex))
					{
						Usage();
						return null;
					}
				}

				else if (argument.Equals("/p"))
				{
					if ((appOption != AppOption.ReadContent && appOption != AppOption.ViewContent) || 0 < itemPath.Length)
					{
						Usage();
						return null;
					}
					i++;
					if (i == args.Length)
					{
						Usage();
						return null;
					}
					itemPath = args[i];
				}

				else if (argument.Equals("/d"))
				{
					if (AppOption.ReadContent != appOption)
					{
						Usage();
						return null;
					}

					i++;
					if (i == args.Length)
					{
						Usage();
						return null;
					}
					destinationFolder = args[i];
				}
				else
				{
					Usage();
					return null;
				}
			}

			// '/p <item path>' and '/d <destination folder>' parameters are not required
			if ((AppOption.DeviceTrackList == appOption && NoDeviceIndex == deviceIndex) ||
				((AppOption.ReadContent == appOption || AppOption.ViewContent == appOption) && 0 == source.Length))
			{
				Usage();
				return null;
			}

			if (AppOption.Unknown != appOption)
			{
				if (AppOption.ReadContent == appOption || AppOption.ViewContent == appOption)
				{
					int pos = source.IndexOf("*");
					if (-1 != pos)
					{
						// it is expected that should the source define a medium track it would actually be in the following form:
						// <device index>:<track index> (For example: '0:5' where 0 is the device index and 5 is the track index)
						string[] portions = source.Split(new string[]{ "*" }, StringSplitOptions.RemoveEmptyEntries);
						if (2 != portions.Length)
						{
							Usage();
							return null;
						}
						if (!int.TryParse(portions[0], out deviceIndex) ||
							!int.TryParse(portions[1], out trackIndex))
						{
							Usage();
							return null;
						}
						if (AppOption.ViewContent == appOption)
						{
							sourceType = SourceType.DiscTrackLayout;
						}
						else
						{
							sourceType = 0 < itemPath.Length ? SourceType.DiscTrackLayout : SourceType.DiscTrackUserData;
						}
					}
					else
					{
						imageSource = source;
						sourceType = SourceType.ImageLayout;
					}
				}
				return new AppFunctionality(appOption, deviceIndex, trackIndex, imageSource, itemPath, destinationFolder, sourceType);
			}
			else
			{
				Usage();
				return null;
			}
		}

		static private void Usage()
		{
			Console.WriteLine("DataReaderCmd -l ");
			Console.WriteLine("DataReaderCmd -t /i <deviceIndex> ");
			Console.WriteLine("DataReaderCmd -v /s <source> [/p <viewLayoutFilePath>] ");
			Console.WriteLine("DataReaderCmd -r /s <source> [/p <readLayoutFilePath>] [/d <destinationFolder>] ");

			Console.WriteLine("***VIEW DEVICES***");
			Console.WriteLine("\t-l\t= display list of available CD/DVD/BD devices on the current machine. ");
			Console.WriteLine("\n");
			Console.WriteLine("***VIEW TRACK LIST***");
			Console.WriteLine("\t-t\t= display the list of tracks available on a medium. ");
			Console.WriteLine("\t/i\t= where <deviceIndex> is the index of the device to use. ");
			Console.WriteLine("\n");
			Console.WriteLine("***VIEW LAYOUT CONTENT***");
			Console.WriteLine("\t-v\t= view list of folders/files in a specified folder that is part of a data layout. ");
			Console.WriteLine("\t/s\t= <source> defines the source layout to load folder content from. ");
			Console.WriteLine("\t\tIt could be either the path to an image file or a medium track to load layout from.");
			Console.WriteLine("\t\t- in case it is an image file the <source> should be a valid path to an image file (*.iso) ");
			Console.WriteLine("\t\t- in case it is a medium track the <source> string should be in the following form: ");
			Console.WriteLine("\t\t\t <deviceIndex>*<trackIndex> ");
			Console.WriteLine("\t\t\t <deviceIndex> could be retrieved using -l option and <trackIndex> could be retrieved using -t option. ");
			Console.WriteLine("\t/p\t= <viewLayoutFilePath> definbes a path to a folder from the loaded layout. The path should be in the form \"/folder1/folder2/.../foldern\". To specify the root folder  simply omit this parameter or use \"\" or / or \\ ");
			Console.WriteLine("\n");
			Console.WriteLine("***EXTRACT FILE FROM LAYOUT OR TRACK USER DATA***");
			Console.WriteLine("\t-r\t= extract the user data content of a track or a file from a specified layout.");
			Console.WriteLine("\t/s\t= <source> defines the source to read from. The format of the source string is as defined above. ");
			Console.WriteLine("\t/p\t= <readLayoutFilePath> defines the path to a file to extract. The path should be in the form \"/folder1/folder2/.../foldern/file_name\". If the <source> defines a medium track and this parameter is omitted the user data of the specified medium track will be extracted. ");
			Console.WriteLine("\t/d\t= <destinationFolder> defines the destination folder where the extracted content will bbe stored. For track content a file is created by the name 'data.bin' which contains the track user data, if the medium, is a CD an extra file by the name 'data_raw.bin' will be created containing not only the user data but system info (such as EDC and ECC) as well. ");
			Console.WriteLine();
		}
	}
}
