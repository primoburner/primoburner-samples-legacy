#include "stdafx.h"

#include "Burner.h"
#include "CmdBurnerCallback.h"

// Forward declarations
int ParseCommandLine(int argc, TCHAR* argv[]);
void ShowDevices(Burner* pBurner);

AppFunctionality _Functionality;

/////////////
// Main 
int _tmain( int argc, TCHAR *argv[])
{
	int iParseResult = ParseCommandLine(argc, argv);
	if (0 != iParseResult)
		return iParseResult;

	//////////////////////////////////////////////////////////////////////////////////////////
	// 1) Create the burner object
	Burner* pBurner = new Burner();

	try
	{
		CmdBurnerCallback callback;
		///////////////////////////////////////////////////////////////////////////////////////////
		// 2) Inititialize the burner object
		pBurner->Open();
		pBurner->set_Callback(&callback);

		switch (_Functionality.AppOption)
		{
		case AO_DEVICE_LIST:
			{
				ShowDevices(pBurner);
			}
			break;
		case AO_CLEAN:
			{
				pBurner->SelectDevice(_Functionality.DeviceIndex);
				pBurner->Clean();
			}
			break;
		case AO_IMAGE:
			{
				pBurner->SelectDevice(_Functionality.DeviceIndex);
				ImageBurnSettings imageSettings(_Functionality.LayoutSrc);
				pBurner->BurnImage(imageSettings);
			}
			break;
		case AO_PACKET:
			{
				pBurner->SelectDevice(_Functionality.DeviceIndex);
				PacketBurnSettings packetSettings(_Functionality.LayoutSrc, _Functionality.PacketOption);
				pBurner->BurnPacket(packetSettings);
			}
			break;
		case AO_WRITE:
			{
				pBurner->SelectDevice(_Functionality.DeviceIndex);
				SimpleBurnSettings simpleSettings(_Functionality.LayoutSrc, _Functionality.SimpleOption);
				pBurner->BurnSimple(simpleSettings);
			}
			break;
		}
	}
	catch (BurnerException& be)
	{
		_tprintf(_T("\n\nError detected:"));
		_tprintf(_T("\nError Message:\n\t\t %s \n\n"), be.get_Message().c_str());
	}

	pBurner->Close();
	delete pBurner;

	return 0;
}

void ShowDevices(Burner* pBurner)
{
	const DeviceVector devices = pBurner->EnumerateDevices();
	if (0 == devices.size())
	{
		throw BurnerException(BE_NO_DEVICES, BE_NO_DEVICES_TEXT);
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

void Usage()
{
	_tprintf(_T("DataBurnerCmd -l\n"));
	_tprintf(_T("DataBurnerCmd -c -d <deviceindex>\n"));
	_tprintf(_T("DataBurnerCmd -i <imagefile>  -d <deviceindex>\n"));
	_tprintf(_T("DataBurnerCmd -w [/a] <sourcefoldername> -d <deviceindex>\n"));
	_tprintf(_T("DataBurnerCmd -p /saf <sourcefoldername> -d <deviceindex>\n"));
	_tprintf(_T("\n"));
	_tprintf(_T("\tBurning options (only one can be used at a time):\n"));
	_tprintf(_T("\t-l\t= display list of available CD/DVD devices on the current machine. \n"));
	_tprintf(_T("\n"));
	_tprintf(_T("\t-c\t= clean rewritable disc from existing recordings(BD-R SRM will be formatted in Pseudo-Overwrite format). \n"));
	_tprintf(_T("\n"));
	_tprintf(_T("\t-i\t= write image to medium. \n"));
	_tprintf(_T("\t<imagefile>\t= a path to a ISO image file to burn on a CD or DVD.\n"));
	_tprintf(_T("\n"));
	_tprintf(_T("\t-w\t= path to a folder that should be recorded.\n"));
	_tprintf(_T("\t/a\t= merge the layout of the folder with that of the last complete track on the medium.\n"));
	_tprintf(_T("\t<sourcefoldername>\t= path to a folder that should be recorded.\n"));
	_tprintf(_T("\n"));
	_tprintf(_T("\t-p\t= use packet burning.\n"));
	_tprintf(_T("\t/s\t= start a new session. This is the default option.\n"));
	_tprintf(_T("\t/a\t= append data.\n"));
	_tprintf(_T("\t/f\t= append data and finalize disk.\n"));
	_tprintf(_T("\t<sourcefoldername>\t= path to a folder that should be recorded.\n"));
	_tprintf(_T("\t\n"));

	_tprintf(_T("\t-d <deviceindex> \t= the index of the device to use.\n"));

	_tprintf(_T("\n\nPress any key to exit.\n"));
	_getch();
}

int ParseCommandLine(int argc, TCHAR* argv[])
{
    int32_t			deviceIndex = -1;
	LPCTSTR			lpLayoutSrc = _T("");

	EAppOption appOption = AO_UNKNOWN;
	EPacketBurningOption packetOption = PBO_UNKNOWN;
	ESimpleBurnOption simpleOption = SBO_UNKNOWN;

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

		// _Erase2
        else if (0 == argument.compare(_T("-c")))
        {
			if (AO_UNKNOWN != appOption)
			{
                Usage();
			    return E_INVALIDARG;
			}

			appOption = AO_CLEAN;
        }

		else if( 0 == argument.compare(_T("-i")) )
        {
			if (AO_UNKNOWN != appOption)
			{
                Usage();
			    return E_INVALIDARG;
			}

			appOption = AO_IMAGE;
        }
		else if (0 == argument.compare(_T("-w")))
        {
			if (AO_UNKNOWN != appOption)
			{
                Usage();
			    return E_INVALIDARG;
			}

			appOption = AO_WRITE;
        }
		// Packet
        else if (0 == argument.compare(_T("-p")))
        {
			if (AO_UNKNOWN != appOption)
			{
                Usage();
			    return E_INVALIDARG;
			}

			appOption = AO_PACKET;
        }
		else if (0 == argument.compare(_T("/s")))
        {
			if (appOption != AO_PACKET ||
				PBO_UNKNOWN != packetOption)
			{
                Usage();
			    return E_INVALIDARG;
			}

			packetOption = PBO_START;
        }
		// Append
        else if (0 == argument.compare(_T("/a")))
        {
			if ((appOption != AO_PACKET && appOption != AO_WRITE) ||
				(AO_PACKET == appOption && PBO_UNKNOWN != packetOption) ||
				(AO_WRITE == appOption && SBO_UNKNOWN != simpleOption))
			{
                Usage();
			    return E_INVALIDARG;
			}

			if (appOption == AO_PACKET)
			{
				packetOption = PBO_APPEND;
			}
			else if (appOption == AO_WRITE)
			{
				simpleOption = SBO_MERGE;
			}
        }

		// Finalize
        else if (0 == argument.compare(_T("/f")))
        {
			if (PBO_UNKNOWN != packetOption)
			{
                Usage();
			    return E_INVALIDARG;
			}

			packetOption = PBO_FINALIZE;
        }
		else if (0 == argument.compare(_T("-d")))
        {
			i++;
			if( i == argc )
			{
				Usage();
				return E_INVALIDARG;
			}
			deviceIndex = _tstoi(argv[i]);
        }

		else
		{
			if (AO_IMAGE != appOption && AO_WRITE != appOption && AO_PACKET != appOption)
			{
				Usage();
				return E_INVALIDARG;
			}

			lpLayoutSrc = argv[i];
		}
	}

	// Set the default for packet burn option if not set by user 
	if (AO_PACKET == appOption && PBO_UNKNOWN == packetOption)
		packetOption = PBO_START;

	// Set the default for simple burn option if not set by user 
	if (AO_WRITE == appOption && SBO_UNKNOWN == simpleOption)
		simpleOption = SBO_OVERWRITE;

	if (AO_DEVICE_LIST != appOption && -1 == deviceIndex)
	{
		Usage();
        return E_INVALIDARG;
	}
	// source folder must be specified
    if (AO_CLEAN != appOption && AO_DEVICE_LIST != appOption && 0 == _tcsicmp(lpLayoutSrc, _T("")))
    {
        Usage();
        return E_INVALIDARG;
    }

	_Functionality.AppOption = appOption;
	_Functionality.DeviceIndex = deviceIndex;
	_Functionality.LayoutSrc = lpLayoutSrc;
	_Functionality.PacketOption = packetOption;
	_Functionality.SimpleOption = simpleOption;
	
	return 0;
}
