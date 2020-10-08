#include "stdafx.h"
#include "Reader.h"
#include "ReaderCallback.h"
#include "ReaderException.h"
#include "ReaderSettings.h"
#define NO_DEVICE_INDEX -1

int ParseCommandLine(int argc, TCHAR* argv[]);
void FileTimeToLocalSystemTime(const FILETIME & ft, SYSTEMTIME & st);
void ShowDeviceList(Reader* reader);
void ShowDeviceTracks(Reader* reader, int32_t deviceIndex);
void ShowLayoutContent(Reader* reader, AppFunctionality& functionality);
void ReadLayoutItem(Reader* reader, AppFunctionality& functionality);
void Usage();

// Global variables
AppFunctionality _Functionality;

int _tmain( int argc, TCHAR *argv[])
{
	int iParseResult = ParseCommandLine(argc, argv);
	if (0 != iParseResult)
		return iParseResult;

	ReaderCallback* callback = new ReaderCallback();
	Reader* reader = new Reader();
	reader->set_Callback(callback);

	try
	{
		reader->Open();
		switch (_Functionality.AppOption)
		{
		case AO_DEVICE_LIST:
			{
				ShowDeviceList(reader);
			}
			break;
		case AO_DEVICE_TRACK_LIST:
			{
				ShowDeviceTracks(reader, _Functionality.DeviceIndex);
			}
			break;
		case AO_VIEW_CONTENT:
			{
				ShowLayoutContent(reader, _Functionality);
			}
			break;
		case AO_READ_CONTENT:
			{
				ReadLayoutItem(reader, _Functionality);
			}
			break;
		default:
			_tprintf(_T("Unknown application option"));
			break;
		}
	}
	catch (ReaderException& be)
	{
		_tprintf(_T("\n\nError detected:"));
		_tprintf(_T("\nError Message:\n\t\t %s \n\n"), be.get_Message().c_str());
	}

	reader->Close();
	delete reader;
	delete callback;

	_tprintf(_T("\n\nPress any key to exit.\n"));
	_getch();

	return 0;
}

// Implementation
void ShowDeviceList(Reader* reader)
{
	const DeviceVector devices = reader->EnumerateDevices();
	if (0 == devices.size())
	{
		throw ReaderException(RE_NO_DEVICES, RE_NO_DEVICES_TEXT);
	}
	else
	{
		_tprintf(_T("\nAvailable device:\n"));

		// Loop through all the devices and show their name and description
		_tprintf(_T("Index     Description\n"));
		for (size_t i = 0; i < devices.size(); i++) 
		{
			DeviceInfo info = devices[i];
			_tprintf(_T("  %d.     %s:\n"), info.Index, info.Title.c_str());
		}
	}
}

void ShowDeviceTracks(Reader* reader, int32_t deviceIndex)
{
	const TrackVector tracks = reader->EnumerateTracks(deviceIndex);
	if (0 == tracks.size())
	{
		throw ReaderException(RE_NO_TRACKS_ON_DISC, RE_NO_TRACKS_ON_DISC_TEXT);
	}
	else
	{
		// Display a list of tracks
		_tprintf(_T("\nFirst track, Last Track: %d, %d\n\n"), tracks[0].TrackIndex, tracks[tracks.size() - 1].TrackIndex);
		for(size_t i = 0; i < tracks.size(); i++)
		{
			_tprintf(_T("\t%s\n"), tracks[i].DisplayTitle.c_str());
		}
	}
}
void ShowLayoutContent(Reader* reader, AppFunctionality& functionality)
{
	if (ST_DISC_TRACK_LAYOUT == functionality.SourceType)
	{
		reader->PrepareSource(functionality.DeviceIndex ,functionality.TrackIndex);
	}
	else if (ST_IMAGE_LAYOUT == functionality.SourceType)
	{
		reader->PrepareSource(functionality.ImageSource);
	}
	else
	{
		Usage();
		return;
	}
	LayoutItemVector layoutItems;
	reader->GetFolderContentFromLayout( functionality.ItemPath, layoutItems);
	SYSTEMTIME st;

	_tprintf(_T("Content of %s :\n\n"), 0 < functionality.ItemPath.length() ? functionality.ItemPath.c_str() : _T("<root>"));
	for (size_t i = 0; i < layoutItems.size(); i++)
	{
		LayoutItem item = layoutItems[i];
		FileTimeToLocalSystemTime(item.FileTime, st);
		bool isDirectory = item.IsDirectory;
		_tprintf(_T("%02d. Addr: %8u Size: %8llu %d-%d-%d %02d:%02d:%02d.%03d %s %s\n"), 
			i + 1,
			item.Address,
			item.SizeInBytes,
			st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond, st.wMilliseconds,
			isDirectory ? _T("<DIR> ") : _T("<FILE>"),
			item.FileName.c_str());
	}
}
void ReadLayoutItem(Reader* reader, AppFunctionality& functionality)
{
	if (ST_DISC_TRACK_LAYOUT == functionality.SourceType)
	{
		reader->PrepareSource(functionality.DeviceIndex ,functionality.TrackIndex);
	}
	else if (ST_IMAGE_LAYOUT == functionality.SourceType)
	{
		reader->PrepareSource(functionality.ImageSource);
	}
	else if (ST_DISC_TRACK_USER_DATA == functionality.SourceType)
	{
		TrackRipSettings settings(functionality.DeviceIndex, functionality.TrackIndex, functionality.DestinationFolder);
		reader->ReadTrackUserData(settings);
		return;
	}
	else
	{
		Usage();
		return;
	}
	reader->ReadFileFromSource(functionality.ItemPath, functionality.DestinationFolder);
}
void FileTimeToLocalSystemTime(const FILETIME & ft, SYSTEMTIME & st)
{
	FILETIME ftLocal;
	FileTimeToLocalFileTime(&ft, &ftLocal);  // convert to local time
	FileTimeToSystemTime(&ftLocal, &st);
}

int ParseCommandLine(int argc, TCHAR* argv[])
{
	tstring source = _T("");
	tstring itemPath = _T("");
	tstring destinationFolder = _T("");
	int32_t deviceIndex = NO_DEVICE_INDEX;
	EAppOption appOption = AO_UNKNOWN;

	for(int i = 1; i < argc; i ++ )
    {
		tstring argument = argv[i];
		if (0 == argument.compare(_T("-l")))
        {
			if (AO_UNKNOWN != appOption || i + 1 < argc)
			{
                Usage();
			    return E_INVALIDARG;
			}

			appOption = AO_DEVICE_LIST;
        }

        else if (0 == argument.compare(_T("-t")))
        {
			if (AO_UNKNOWN != appOption)
			{
                Usage();
			    return E_INVALIDARG;
			}

			appOption = AO_DEVICE_TRACK_LIST;
        }

		else if( 0 == argument.compare(_T("-r")) )
        {
			if (AO_UNKNOWN != appOption)
			{
                Usage();
			    return E_INVALIDARG;
			}

			appOption = AO_READ_CONTENT;
        }
		else if (0 == argument.compare(_T("-v")))
        {
			if (AO_UNKNOWN != appOption)
			{
                Usage();
			    return E_INVALIDARG;
			}

			appOption = AO_VIEW_CONTENT;
        }

        else if (0 == argument.compare(_T("/s")))
        {
			// if option other than read and view was already selected or if the source is already defined, break execution and show usage details
			if ((AO_READ_CONTENT != appOption && AO_VIEW_CONTENT != appOption) || 0 < source.length())
			{
                Usage();
			    return E_INVALIDARG;
			}

			i++;
			if( i == argc )
			{
				Usage();
				return E_INVALIDARG;
			}
			source = argv[i];
        }
		else if (0 == argument.compare(_T("/i")))
        {
			if (appOption != AO_DEVICE_TRACK_LIST)
			{
                Usage();
			    return E_INVALIDARG;
			}
			i++;
			if( i == argc )
			{
				Usage();
				return E_INVALIDARG;
			}
			deviceIndex = _tstoi(argv[i]);
        }

        else if (0 == argument.compare(_T("/p")))
        {
			if ((appOption != AO_READ_CONTENT && appOption != AO_VIEW_CONTENT) || 0 < itemPath.length())
			{
                Usage();
			    return E_INVALIDARG;
			}
			i++;
			if( i == argc )
			{
				Usage();
				return E_INVALIDARG;
			}
			itemPath = argv[i];
        }

        else if (0 == argument.compare(_T("/d")))
        {
			if (AO_READ_CONTENT != appOption)
			{
                Usage();
			    return E_INVALIDARG;
			}

			i++;
			if( i == argc )
			{
				Usage();
				return E_INVALIDARG;
			}
			destinationFolder = argv[i];
        }
		else
		{
			Usage();
			return E_INVALIDARG;
		}
	}

	// '/p <item path>' and '/d <destination folder>' parameters are not required
	if ((AO_DEVICE_TRACK_LIST == appOption && NO_DEVICE_INDEX == deviceIndex) ||
		((AO_READ_CONTENT == appOption || AO_VIEW_CONTENT == appOption) && 0 == source.length()))
	{
		Usage();
		return E_INVALIDARG;
	}

	if (AO_UNKNOWN != appOption)
	{
		_Functionality.AppOption = appOption;
		if (AO_READ_CONTENT == appOption || AO_VIEW_CONTENT == appOption)
		{
			size_t pos = source.find_first_of(_T("*"));
			if (tstring::npos != pos)
			{
				// it is expected that should the source define a medium track it would actually be in the following form:
				// <device index>:<track index> (For example: '0:5' where 0 is the device index and 5 is the track index)
				_Functionality.DeviceIndex = _tstoi(source.substr(0, pos).c_str());
				_Functionality.TrackIndex = _tstoi(source.substr(pos+1, source.length() - (pos+1)).c_str());
				if (AO_VIEW_CONTENT == appOption)
				{
					_Functionality.SourceType = ST_DISC_TRACK_LAYOUT;
				}
				else
				{
					_Functionality.SourceType = 0 < itemPath.length() ? ST_DISC_TRACK_LAYOUT : ST_DISC_TRACK_USER_DATA;
				}
			}
			else
			{
				_Functionality.ImageSource = source;
				_Functionality.SourceType = ST_IMAGE_LAYOUT;
			}
			_Functionality.DestinationFolder = destinationFolder;
			_Functionality.ItemPath = itemPath;
		}
		else
		{
			_Functionality.DeviceIndex = deviceIndex;
		}
	}
	else
	{
		Usage();
		return E_INVALIDARG;
	}

	return 0;
}

void Usage()
{
	_tprintf(_T("DataReaderCmd -l \n"));
	_tprintf(_T("DataReaderCmd -t /i <deviceIndex> \n"));
	_tprintf(_T("DataReaderCmd -v /s <source> [/p <viewLayoutFilePath>] \n"));
	_tprintf(_T("DataReaderCmd -r /s <source> [/p <readLayoutFilePath>] [/d <destinationFolder>] \n"));

	_tprintf(_T("***VIEW DEVICES***\n"));
	_tprintf(_T("\t-l\t= display list of available CD/DVD/BD devices on the current machine. \n"));
	_tprintf(_T("\n"));
	_tprintf(_T("***VIEW TRACK LIST***\n"));
	_tprintf(_T("\t-t\t= display the list of tracks available on a medium. \n"));
	_tprintf(_T("\t/i\t= where <deviceIndex> is the index of the device to use. \n"));
	_tprintf(_T("\n"));
	_tprintf(_T("***VIEW LAYOUT CONTENT***\n"));
	_tprintf(_T("\t-v\t= view list of folders/files in a specified folder that is part of a data layout. \n"));
	_tprintf(_T("\t/s\t= <source> defines the source layout to load folder content from. \n"));
	_tprintf(_T("\t\tIt could be either the path to an image file or a medium track to load layout from.\n"));
	_tprintf(_T("\t\t- in case it is an image file the <source> should be a valid path to an image file (*.iso) \n"));
	_tprintf(_T("\t\t- in case it is a medium track the <source> string should be in the following form: \n"));
	_tprintf(_T("\t\t\t <deviceIndex>*<trackIndex> \n"));
	_tprintf(_T("\t\t\t <deviceIndex> could be retrieved using -l option and <trackIndex> could be retrieved using -t option. \n"));
	_tprintf(_T("\t/p\t= <viewLayoutFilePath> definbes a path to a folder from the loaded layout. The path should be in the form \"/folder1/folder2/.../foldern\". To specify the root folder  simply omit this parameter or use \"\" or / or \\ \n"));
	_tprintf(_T("\n"));
	_tprintf(_T("***EXTRACT FILE FROM LAYOUT OR TRACK USER DATA***\n"));
	_tprintf(_T("\t-r\t= extract the user data content of a track or a file from a specified layout.\n "));
	_tprintf(_T("\t/s\t= <source> defines the source to read from. The format of the source string is as defined above. \n"));
	_tprintf(_T("\t/p\t= <readLayoutFilePath> defines the path to a file to extract. The path should be in the form \"/folder1/folder2/.../foldern/file_name\". If the <source> defines a medium track and this parameter is omitted the user data of the specified medium track will be extracted. \n"));
	_tprintf(_T("\t/d\t= <destinationFolder> defines the destination folder where the extracted content will bbe stored. For track content a file is created by the name 'data.bin' which contains the track user data, if the medium, is a CD an extra file by the name 'data_raw.bin' will be created containing not only the user data but system info (such as EDC and ECC) as well. \n"));
	_tprintf(_T("\n"));

	_tprintf(_T("\n\nPress any key to exit.\n"));
	_getch();
}
