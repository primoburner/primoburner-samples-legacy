using System;
using System.Collections.Generic;
using System.IO;
//using System.Runtime.InteropServices;
//using System.Threading;
using PrimoSoftware.Burner;

namespace DataBurnerCmd.NET
{
	class Program
	{
		[STAThread]
		static void Main(string[] args)
		{
			Application app = new Application();
			app.Run(ParseCommandLine(args));
		}

		static AppFunctionality ParseCommandLine(string[] args)
		{
			int deviceIndex = -1;
			string layoutSrc = string.Empty;

			AppOption appOption = AppOption.Unknown;
			PacketBurnOption packetOption = PacketBurnOption.Unknown;
			SimpleBurnOption simpleOption = SimpleBurnOption.Unknown;

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

				// _Erase2
				else if (argument.Equals("-c"))
				{
					if (AppOption.Unknown != appOption)
					{
						Usage();
						return null;
					}

					appOption = AppOption.Clean;
				}

				else if (argument.Equals("-i"))
				{
					if (AppOption.Unknown != appOption)
					{
						Usage();
						return null;
					}

					appOption = AppOption.Image;
				}
				else if (argument.Equals("-w"))
				{
					if (AppOption.Unknown != appOption)
					{
						Usage();
						return null;
					}

					appOption = AppOption.Write;
				}
				// Packet
				else if (argument.Equals("-p"))
				{
					if (AppOption.Unknown != appOption)
					{
						Usage();
						return null;
					}

					appOption = AppOption.Packet;
				}
				else if (argument.Equals("/s"))
				{
					if (appOption != AppOption.Packet ||
						PacketBurnOption.Unknown != packetOption)
					{
						Usage();
						return null;
					}

					packetOption = PacketBurnOption.Start;
				}
				// Append
				else if (argument.Equals("/a"))
				{
					if ((appOption != AppOption.Packet && appOption != AppOption.Write) ||
						(AppOption.Packet == appOption && PacketBurnOption.Unknown != packetOption) ||
						(AppOption.Write == appOption && SimpleBurnOption.Unknown != simpleOption))
					{
						Usage();
						return null;
					}

					if (appOption == AppOption.Packet)
					{
						packetOption = PacketBurnOption.Append;
					}
					else if (appOption == AppOption.Write)
					{
						simpleOption = SimpleBurnOption.Merge;
					}
				}

				// Finalize
				else if (argument.Equals("/f"))
				{
					if (PacketBurnOption.Unknown != packetOption)
					{
						Usage();
						return null;
					}

					packetOption = PacketBurnOption.Finalize;
				}
				else if (argument.Equals("-d"))
				{
					i++;
					if (i == args.Length || !int.TryParse(args[i], out deviceIndex))
					{
						Usage();
						return null;
					}
				}

				else
				{
					if (AppOption.Image != appOption && AppOption.Write != appOption && AppOption.Packet != appOption)
					{
						Usage();
						return null;
					}

					layoutSrc = args[i];
				}
			}

			// Set the default for packet burn option if not set by user 
			if (AppOption.Packet == appOption && PacketBurnOption.Unknown == packetOption)
				packetOption = PacketBurnOption.Start;

			// Set the default for simple burn option if not set by user 
			if (AppOption.Write == appOption && SimpleBurnOption.Unknown == simpleOption)
				simpleOption = SimpleBurnOption.Overwrite;

			if (AppOption.DeviceList != appOption && -1 == deviceIndex)
			{
				Usage();
				return null;
			}
			// source folder must be specified
			if (AppOption.Clean != appOption && AppOption.DeviceList != appOption && 0 == layoutSrc.Length)
			{
				Usage();
				return null;
			}

			return new AppFunctionality(appOption, packetOption, simpleOption, deviceIndex, layoutSrc);
		}

		static private void Usage()
		{
			Console.WriteLine("DataBurnerCmd.NET -l");
			Console.WriteLine("DataBurnerCmd.NET -c -d <deviceindex>");
			Console.WriteLine("DataBurnerCmd.NET -i <imagefile>  -d <deviceindex>");
			Console.WriteLine("DataBurnerCmd.NET -w [/a] <sourcefoldername> -d <deviceindex>");
			Console.WriteLine("DataBurnerCmd.NET -p /saf <sourcefoldername> -d <deviceindex>");
			Console.WriteLine("");
			Console.WriteLine("\tBurning options (only one can be used at a time):");
			Console.WriteLine("\t-l\t= display list of available CD/DVD devices on the current machine. ");
			Console.WriteLine("");
			Console.WriteLine("\t-c\t= clean rewritable disc from existing recordings(BD-R SRM will be formatted in Pseudo-Overwrite format). ");
			Console.WriteLine("");
			Console.WriteLine("\t-i\t= write image to medium. ");
			Console.WriteLine("\t<imagefile>\t= a path to a ISO image file to burn on a CD or DVD.");
			Console.WriteLine("");
			Console.WriteLine("\t-w\t= path to a folder that should be recorded.");
			Console.WriteLine("\t/a\t= merge the layout of the folder with that of the last complete track on the medium.");
			Console.WriteLine("\t<sourcefoldername>\t= path to a folder that should be recorded.");
			Console.WriteLine("");
			Console.WriteLine("\t-p\t= use packet burning.");
			Console.WriteLine("\t/s\t= start a new session. This is the default option.");
			Console.WriteLine("\t/a\t= append data.");
			Console.WriteLine("\t/f\t= append data and finalize disk.");
			Console.WriteLine("\t<sourcefoldername>\t= path to a folder that should be recorded.");
			Console.WriteLine("\t");

			Console.WriteLine("\t-d <deviceindex> \t= the index of the device to use.");

			Console.WriteLine("\n\nPress any key to exit.");
			Console.ReadKey();
		}
	}
}
