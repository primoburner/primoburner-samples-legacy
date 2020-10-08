#include "stdafx.h"

#include "Burner.h"
#include "BurnerApp.h"
#include "BurnerDialog.h"
#include "CDTextDialog.h"


/////////////////////////////////////////////////////////////////////////////
// CAboutDlg dialog used for App About

class CAboutDlg : public CDialog
{
public:
	CAboutDlg();

// Dialog Data
	//{{AFX_DATA(CAboutDlg)
	enum { IDD = IDD_ABOUTBOX };
	//}}AFX_DATA

	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(CAboutDlg)
	protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	//}}AFX_VIRTUAL

// Implementation
protected:
	//{{AFX_MSG(CAboutDlg)
	//}}AFX_MSG
	DECLARE_MESSAGE_MAP()
};

CAboutDlg::CAboutDlg() : CDialog(CAboutDlg::IDD)
{
	//{{AFX_DATA_INIT(CAboutDlg)
	//}}AFX_DATA_INIT
}

void CAboutDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	//{{AFX_DATA_MAP(CAboutDlg)
	//}}AFX_DATA_MAP
}

BEGIN_MESSAGE_MAP(CAboutDlg, CDialog)
	//{{AFX_MSG_MAP(CAboutDlg)
		// No message handlers
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CDataBurnerDlg dialog

////////////////////////////////////////////////////////////
// CBurnerDialog dialog

CBurnerDialog::CBurnerDialog(CWnd* pParent /*=NULL*/)
	: CDialog(CBurnerDialog::IDD, pParent),
	m_eProgressAvailable(TRUE, FALSE, NULL),
	m_eCommandThreadStarted(TRUE, FALSE, NULL)
{
	//{{AFX_DATA_INIT(CBurnerDialog)
	//}}AFX_DATA_INIT

	m_pWorkThread = NULL;

	m_ProgressInfo.Percent = 0;
	m_ProgressInfo.UsedCachePercent = 0;
	m_ProgressInfo.ActualWriteSpeed = 0;
	m_ProgressInfo.Message = _TEXT("");

	m_burner.set_Callback(this);
}

CBurnerDialog::~CBurnerDialog() 
{

}

void CBurnerDialog::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	DDX_Control(pDX, IDC_BUTTON_ERASE, m_btnErase);
	DDX_Control(pDX, IDC_CHECK_QUICK, m_chkQuickErase);
	DDX_Control(pDX, IDC_STATIC_FREE_SPACE, m_staticFreeSpace);
	DDX_Control(pDX, IDC_LIST_PLAYLIST, m_listPlaylist);
	DDX_Control(pDX, IDC_COMBO_SPEED, m_comboSpeed);
	DDX_Control(pDX, IDC_COMBO_DEVICES, m_comboDevices);
	DDX_Control(pDX, IDC_CHECK_TEST, m_chkTest);
	DDX_Control(pDX, IDC_CHECK_USE_AUDIO_STREAM, m_chkUseAudioStream);
	DDX_Control(pDX, IDC_CHECK_EJECT, m_chkEject);
	DDX_Control(pDX, IDC_BUTTON_START, m_btnStart);
	DDX_Control(pDX, 1016, m_btnCDTextTrack);
	DDX_Control(pDX, IDC_ALBUM_CDTEXT, m_btnCDTextAlbum);
	DDX_Control(pDX, IDC_CHECK_CDTEXT, m_chkCDText);
	DDX_Control(pDX, IDC_CHECK_HIDDENTRACK, m_chkHiddenTrack);
	DDX_Control(pDX, IDC_COMBO_MODE, m_comboMode);
	DDX_Control(pDX, IDC_CHECK_CLOSE_DISK, m_chkCloseDisc); 
	DDX_Control(pDX, IDC_CHECK_DECODE_IN_TEMPFILES, m_chkDecodeTempFile);
	DDX_Control(pDX, IDC_BUTTON_ADD_FILES, m_btnAddFiles);
	DDX_Control(pDX, IDC_BUTTON_REMOVE_FILES, m_btnRemoveFiles);
}

BEGIN_MESSAGE_MAP(CBurnerDialog, CDialog)
	//{{AFX_MSG_MAP(CBurnerDialog)
	ON_WM_SYSCOMMAND()
	ON_WM_PAINT()
	ON_WM_QUERYDRAGICON()
	ON_WM_DROPFILES()
	ON_WM_VKEYTOITEM()
	ON_WM_DESTROY()
	ON_BN_CLICKED(IDC_BUTTON_START, OnButtonStart)
	ON_BN_CLICKED(IDC_BUTTON_EJECTIN, OnButtonEjectin)
	ON_BN_CLICKED(IDC_BUTTON_EJECTOUT, OnButtonEjectout)
	ON_BN_CLICKED(IDC_BUTTON_ERASE, OnButtonErase)
	ON_MESSAGE( WM_DEVICECHANGE, OnDeviceChange)
	ON_CBN_SELCHANGE(IDC_COMBO_DEVICES, OnCbnSelchangeComboDevices)
	ON_BN_CLICKED(IDC_TRACK_CDTEXT, OnBnClickedTrackCdtext)
	ON_BN_CLICKED(IDC_ALBUM_CDTEXT, OnBnClickedAlbumCdtext)
	ON_BN_CLICKED(IDC_CHECK_CDTEXT, OnBnClickedCheckCdtext)
	ON_BN_CLICKED(IDC_BUTTON_ADD_FILES, OnButtonAddFiles)
	ON_BN_CLICKED(IDC_BUTTON_REMOVE_FILES, OnButtonRemoveFiles)
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CBurnerDialog message handlers

BOOL CBurnerDialog::OnInitDialog()
{
	CDialog::OnInitDialog();

	CString title;
	GetWindowText(title);
	
	#if _WIN64
		title += _T(" 64bit");
	#else
		title += _T(" 32bit");
	#endif
	
	SetWindowText(title);

	m_comboMode.InsertString(0, _TEXT("Session-At-Once"));	m_comboMode.SetItemData(0, WriteMethod::Sao);
	m_comboMode.InsertString(1, _TEXT("Track-At-Once"));	m_comboMode.SetItemData(1, WriteMethod::Tao);  
	m_comboMode.SetCurSel(0);

	m_chkUseAudioStream.SetCheck(FALSE);
	m_chkTest.SetCheck(FALSE);
	m_chkEject.SetCheck(FALSE);
	m_chkCDText.SetCheck(FALSE);
	m_chkHiddenTrack.SetCheck(FALSE);
	m_chkHiddenTrack.EnableWindow(FALSE);
	m_chkQuickErase.SetCheck(TRUE);

	m_chkCloseDisc.SetCheck(TRUE);
	m_chkDecodeTempFile.SetCheck(FALSE);

	try
	{
		m_burner.Open();
		const DeviceVector& devices = m_burner.EnumerateDevices();

		for (size_t i = 0; i < devices.size(); i++) 
		{
			const DeviceInfo& dev = devices[i];
			if (dev.IsWriter) 
			{
				m_comboDevices.AddString(dev.Title.c_str());
				m_deviceIndexArray.push_back(dev.Index);
			}
		}

		if (m_deviceIndexArray.size() == 0)
			throw BurnerException(NO_WRITER_DEVICES, NO_WRITER_DEVICES_TEXT);

		// Device combo
		m_comboDevices.SetCurSel(0);
		UpdateDeviceInformation();
	}
	catch(BurnerException& bme)
	{
		ShowErrorMessage(bme);
		EndModalLoop(-1);
		return FALSE;
	}

	// Add "About..." menu item to system menu.
	// IDM_ABOUTBOX must be in the system command range.

	ASSERT((IDM_ABOUTBOX & 0xFFF0) == IDM_ABOUTBOX);
	ASSERT(IDM_ABOUTBOX < 0xF000);

	CMenu* pSysMenu = GetSystemMenu(FALSE);
	if (pSysMenu != NULL)
	{
		CString strAboutMenu;
		strAboutMenu.LoadString(IDS_ABOUTBOX);
		if (!strAboutMenu.IsEmpty())
		{
			pSysMenu->AppendMenu(MF_SEPARATOR);
			pSysMenu->AppendMenu(MF_STRING, IDM_ABOUTBOX, strAboutMenu);
		}
	}

	DragAcceptFiles();

	// Set the icon for this dialog.  The framework does this automatically
	//  when the application's main window is not a dialog
	SetIcon(m_hIcon, TRUE);			// Set big icon
	SetIcon(m_hIcon, FALSE);		// Set small icon

	return TRUE;  // return TRUE  unless you set the focus to a control
}

void CBurnerDialog::OnBnClickedCheckCdtext()
{
	BOOL bCDtext = m_chkCDText.GetCheck();
	m_btnCDTextAlbum.EnableWindow(bCDtext);
	m_btnCDTextTrack.EnableWindow(bCDtext);
}

void CBurnerDialog::OnDestroy() 
{
	CDialog::OnDestroy();
	m_burner.Close();
}

void CBurnerDialog::OnButtonEjectin()
{
	const int deviceSelection = m_comboDevices.GetCurSel();
	const int deviceIndex = (-1 == deviceSelection) ? -1 : m_deviceIndexArray[deviceSelection];

	try
	{
		m_burner.SelectDevice(deviceIndex, false);
		m_burner.CloseTray();
		m_burner.ReleaseDevice();
	}
	catch(BurnerException& bme)
	{
		m_burner.ReleaseDevice();
		ShowErrorMessage(bme);
	}
}

void CBurnerDialog::OnButtonEjectout() 
{
	const int deviceSelection = m_comboDevices.GetCurSel();
	const int deviceIndex = (-1 == deviceSelection) ? -1 : m_deviceIndexArray[deviceSelection];

	try
	{
		m_burner.SelectDevice(deviceIndex, false); 
		m_burner.Eject();
		m_burner.ReleaseDevice();
	}
	catch(BurnerException& bme)
	{
		m_burner.ReleaseDevice();
		ShowErrorMessage(bme);
	}
}

LRESULT CBurnerDialog::OnDeviceChange(WPARAM /* wParam */, LPARAM /* lParam */) 
{
	UpdateDeviceInformation();
	return 0;
}

void CBurnerDialog::OnCbnSelchangeComboDevices()
{
	UpdateDeviceInformation();
}

void CBurnerDialog::OnDropFiles(HDROP hDropInfo) 
{
	int nCount = DragQueryFile(hDropInfo,0xFFFFFFFF,NULL,0);

	TCHAR buf[MAX_PATH];

	for(int nFile=0;nFile<nCount;nFile++) 
	{
		DragQueryFile(hDropInfo,nFile,buf,MAX_PATH);

		AddFileToPlayList(buf);
	}
	CDialog::OnDropFiles(hDropInfo);
}

int CBurnerDialog::OnVKeyToItem(UINT nKey, CListBox* pListBox, UINT nIndex) 
{
	if (VK_DELETE == nKey)
	{
		RemoveFilesFromPlayList(pListBox);
	}
	return CDialog::OnVKeyToItem(nKey, pListBox, nIndex);
}

void CBurnerDialog::ShowErrorMessage(BurnerException& burnerException)
{
	CString msg;
	msg.Format(_T("%s (0x%x)"),burnerException.get_Message().c_str(), burnerException.get_Error());
	AfxMessageBox(msg);
}

void CBurnerDialog::UpdateDeviceInformation()
{
	int nCurSel = m_comboDevices.GetCurSel();
	if (-1 == nCurSel)
		return;

	try
	{
		// Select device. Exclusive access is not required.
		m_burner.SelectDevice(m_deviceIndexArray[nCurSel], false);

		const int mediaFreeSpace = m_burner.get_MediaFreeSpace();

		if (mediaFreeSpace <= 0) 
		{
			m_staticFreeSpace.SetWindowText(_TEXT("Insert a blank disc."));
		} 
		else 
		{
			const int nMin = (mediaFreeSpace) / (75 * 60);
			CString str;

			if (m_burner.get_MediaIsBlank())
				str.Format(_TEXT("%d min free (a blank disc)"), nMin);
			else
				str.Format(_TEXT("%d min free (not a blank disc)"), nMin);

			m_staticFreeSpace.SetWindowText(str);
		}
		m_btnStart.EnableWindow(mediaFreeSpace > 0);

		// Speed
		const int nMaxSpeedKB = m_burner.get_MaxWriteSpeedKB();
		const SpeedVector& speeds = m_burner.EnumerateWriteSpeeds();

		const int nCurSel = m_comboSpeed.GetCurSel();
		const int nCurSpeedKB = (int32_t)m_comboSpeed.GetItemData(nCurSel);

		m_comboSpeed.ResetContent();

		for (size_t i = 0; i < speeds.size(); i++) 
		{
			const SpeedInfo& speed = speeds[i];
				
			CString sSpeed;
			sSpeed.Format(_TEXT("%1.0fx"), (double)speed.TransferRateKB / speed.TransferRate1xKB);
	
			m_comboSpeed.InsertString(-1, sSpeed);
			m_comboSpeed.SetItemData((int)i, speed.TransferRateKB);
		}

		// Restore speed selection
		if ((-1 != nCurSel) && (nCurSpeedKB <= nMaxSpeedKB))
		{
			CString sSpeed;
			sSpeed.Format(_TEXT("%1.0fx"), (double)nCurSpeedKB / Speed1xKB::CD);
			m_comboSpeed.SelectString(-1, sSpeed);
		}

		if (-1 == m_comboSpeed.GetCurSel())
			m_comboSpeed.SetCurSel(0);

		const bool bErasePossible = m_burner.get_ReWritePossible() && m_burner.get_MediaIsReWritable();
		m_chkQuickErase.EnableWindow(bErasePossible);
		m_btnErase.EnableWindow(bErasePossible);

		m_chkCDText.EnableWindow(m_burner.get_CDTextSupport());
		m_burner.ReleaseDevice();
	}
	catch(BurnerException& bme)
	{
		if (DEVICE_ALREADY_SELECTED == bme.get_Error())
			return;

		// Report all other errors
		m_burner.ReleaseDevice();
		ShowErrorMessage(bme);
	}
}

void CBurnerDialog::WaitForThreadToFinish()
{
	ASSERT(NULL != m_pWorkThread);
	while (WAIT_TIMEOUT == WaitForSingleObject(m_pWorkThread->m_hThread, 50))
	{
		ProcessMessages();
		
		if (WAIT_OBJECT_0 == WaitForSingleObject(m_eProgressAvailable.m_hObject, 0)) 
		{
			m_eProgressAvailable.ResetEvent();
			{
				CSingleLock lock(&m_csProgressUpdateGuard, TRUE);
				UpdateProgress(m_ProgressInfo);
			}
		}
	}

	// See if we had any errors: 
	// Exception would be saved in m_ThreadException and the exit code would be set to THREAD_EXCEPTION
	DWORD dwExitCode = 0; GetExitCodeThread(m_pWorkThread->m_hThread, &dwExitCode);
	if (THREAD_EXCEPTION == dwExitCode)
		ShowErrorMessage(m_ThreadException);

	delete m_pWorkThread;
	m_pWorkThread = NULL;
}

void CBurnerDialog::ProcessMessages()
{
	MSG msg;
	while (::PeekMessage(&msg, 0, 0, 0, PM_REMOVE))
		::DispatchMessage(&msg);
}

void CBurnerDialog::OnSysCommand(UINT nID, LPARAM lParam)
{
	if ((nID & 0xFFF0) == IDM_ABOUTBOX)
	{
		CAboutDlg dlgAbout;
		dlgAbout.DoModal();
	}
	else
	{
		CDialog::OnSysCommand(nID, lParam);
	}
}
void CBurnerDialog::onWriteTrack(int /* nTrack */, int /* nPercent */)
{
	//TODO: implement for more detailed burning progress
}

void CBurnerDialog::onWriteStatus(AudioCDStatus::Enum eStatus)
{
	tstring message = m_burner.TranslateAudioCDStatus(eStatus);
	CSingleLock lock(&m_csProgressUpdateGuard, TRUE);
	m_ProgressInfo.Message = message.c_str();
	m_eProgressAvailable.SetEvent();
}

void CBurnerDialog::onWriteProgress(uint32_t dwPosition, uint32_t dwAll)
{
	int nProgress = dwPosition * 100 / dwAll;
	CSingleLock lock(&m_csProgressUpdateGuard, TRUE);
	m_ProgressInfo.Percent = nProgress;

	try
	{
		uint32_t deviceCacheSize = m_burner.get_DeviceCacheSize();
		uint32_t deviceCacheUsedSize = m_burner.get_DeviceCacheUsedSize();
		if (deviceCacheSize > 0)
		{
			m_ProgressInfo.UsedCachePercent = static_cast<int>(100 * ((double)deviceCacheUsedSize / (double)deviceCacheSize));
		}
	}
	catch (...)
	{
	}

	m_eProgressAvailable.SetEvent();
}

bool_t CBurnerDialog::onContinueWrite()
{
	return (!m_ProgressWindow.m_bStopped);
}

void CBurnerDialog::CreateProgressWindow()
{
	EnableWindow(FALSE);

	m_ProgressWindow.Create();
	m_ProgressWindow.ShowWindow(SW_SHOW);
	m_ProgressWindow.UpdateWindow();
}

void CBurnerDialog::DestroyProgressWindow()
{
	m_ProgressWindow.DestroyWindow();

	EnableWindow();
	BringWindowToTop();
	SetActiveWindow();
}

void CBurnerDialog::UpdateProgress(ProgressInfo& progressInfo)
{
	if (!::IsWindow(m_ProgressWindow.m_hWnd))
		return;

	m_ProgressWindow.SetStatus(progressInfo.Message);
	m_ProgressWindow.SetProgress(progressInfo.Percent);
	m_ProgressWindow.SetInternalBuffer(progressInfo.UsedCachePercent);
}

void CBurnerDialog::Burn()
{
	int deviceSelection = m_comboDevices.GetCurSel();
	int deviceIndex = (-1 == deviceSelection) ? -1 : m_deviceIndexArray[deviceSelection];

	try
	{
		m_burner.SelectDevice(deviceIndex, false);

		CreateProgressWindow();
		
		m_ProgressWindow.GetDlgItem(IDOK)->EnableWindow(TRUE);
		

		BurnSettings settings;
		{
			settings.Simulate = (BST_CHECKED == m_chkTest.GetCheck());
			settings.Eject = (BST_CHECKED == m_chkEject.GetCheck());
			settings.CloseDisc = (BST_CHECKED == m_chkCloseDisc.GetCheck());
			settings.WriteCDText = (BST_CHECKED == m_chkCDText.GetCheck());
			settings.DecodeInTempFiles = (BST_CHECKED == m_chkDecodeTempFile.GetCheck());
			settings.CreateHiddenTrack = (BST_CHECKED == m_chkHiddenTrack.GetCheck());

			int nSel = m_comboMode.GetCurSel();
			settings.WriteMethod = (WriteMethod::Enum)m_comboMode.GetItemData(nSel);

			nSel = m_comboSpeed.GetCurSel();
			if (nSel >= 0)
			{
				settings.WriteSpeedKB = (uint32_t)m_comboSpeed.GetItemData(nSel);
			}
			else
			{
				settings.WriteSpeedKB = 150;
			}

			settings.CDText.Album = m_AlbumCDText;

			for (size_t i = 0; i < m_Files.size(); i++)
			{
				settings.Files.push_back(m_Files[i].FilePath);
				if ( i < 99)
				{
					settings.CDText.Songs[i] = m_Files[i].CDText;
				}
			}

			settings.UseAudioStream = (BST_CHECKED == m_chkUseAudioStream.GetCheck());
		}
		
		BurnThreadParam threadParam;
		{
			threadParam.pThis = this;
			threadParam.Settings = settings;
		}

		m_eProgressAvailable.ResetEvent();
		m_eCommandThreadStarted.ResetEvent();

		ASSERT(NULL == m_pWorkThread);

		m_pWorkThread = AfxBeginThread(BurnThread, &threadParam, THREAD_PRIORITY_NORMAL, 0, CREATE_SUSPENDED);
		m_pWorkThread->m_bAutoDelete = FALSE;
		m_pWorkThread->ResumeThread();

		WaitForSingleObject(m_eCommandThreadStarted.m_hObject, INFINITE);
		WaitForThreadToFinish();

		DestroyProgressWindow();
		
		m_burner.ReleaseAudio();
		m_burner.ReleaseDevice();
	}
	catch(BurnerException& bme)
	{
		m_burner.ReleaseAudio();
		m_burner.ReleaseDevice();

		ShowErrorMessage(bme);
	}

}

UINT CBurnerDialog::BurnThread(LPVOID pVoid)
{
	CoInitialize(NULL);
	BurnThreadParam* pParam = (BurnThreadParam*)pVoid;
	CBurnerDialog* pThis = pParam->pThis;
	BurnSettings settings = pParam->Settings;

	pThis->m_eCommandThreadStarted.SetEvent();
	// Do not use pParam after this line

	try
	{
		pThis->m_burner.Burn(settings);
	}
	catch(BurnerException& bme)
	{
		pThis->m_ThreadException = bme;
		return THREAD_EXCEPTION;
	}
	return NOERROR;
}

void CBurnerDialog::Erase()
{
	int deviceSelection = m_comboDevices.GetCurSel();
	int deviceIndex = (-1 == deviceSelection) ? -1 : m_deviceIndexArray[deviceSelection];

	try
	{
		m_burner.SelectDevice(deviceIndex, false);

		
		if (m_burner.get_MediaIsBlank())
			if (IDCANCEL == AfxMessageBox(_TEXT("Media is already blank. Do you want to erase it again?"), MB_OKCANCEL))
			{
				m_burner.ReleaseDevice();
				return;
			}

		
		if (IDCANCEL == AfxMessageBox(_TEXT("Erasing will destroy all information on the disc. Do you want to continue?"), MB_OKCANCEL))
		{
			m_burner.ReleaseDevice();
			return;
		}

		CreateProgressWindow();
		
		m_ProgressWindow.GetDlgItem(IDOK)->EnableWindow(FALSE);
		m_ProgressWindow.SetStatus(_TEXT("Erasing. Please wait..."));

		EraseSettings settings;
		{
			settings.Quick = (BST_CHECKED == m_chkQuickErase.GetCheck());
			settings.Force = true;
		}
		
		EraseThreadParam threadParam;
		{
			threadParam.pThis = this;
			threadParam.Settings = settings;
		}

		m_eProgressAvailable.ResetEvent();
		m_eCommandThreadStarted.ResetEvent();

		ASSERT(NULL == m_pWorkThread);

		m_pWorkThread = AfxBeginThread(EraseThread, &threadParam, THREAD_PRIORITY_NORMAL, 0, CREATE_SUSPENDED);
		m_pWorkThread->m_bAutoDelete = FALSE;
		m_pWorkThread->ResumeThread();

		WaitForSingleObject(m_eCommandThreadStarted.m_hObject, INFINITE);
		WaitForThreadToFinish();

		DestroyProgressWindow();
		m_burner.ReleaseDevice();
	}
	catch(BurnerException& bme)
	{
		m_burner.ReleaseDevice();
		ShowErrorMessage(bme);
	}
}

UINT CBurnerDialog::EraseThread(LPVOID pVoid)
{
	CoInitialize(NULL);
	EraseThreadParam* pParam = (EraseThreadParam*)pVoid;
	CBurnerDialog* pThis = pParam->pThis;
	EraseSettings settings = pParam->Settings;

	pThis->m_eCommandThreadStarted.SetEvent();
	// Do not use pParam after this line

	try
	{
		pThis->m_burner.Erase(settings);
	}
	catch(BurnerException& bme)
	{
		pThis->m_ThreadException = bme;
		return THREAD_EXCEPTION;
	}

	return NO_ERROR;
}


void CBurnerDialog::OnButtonErase()
{
	Erase();
	UpdateDeviceInformation();
}


void CBurnerDialog::OnButtonStart()
{
	Burn();
	UpdateDeviceInformation();
}

void CBurnerDialog::OnBnClickedTrackCdtext()
{
	int nCurSel = m_listPlaylist.GetCurSel();
	if (-1 == nCurSel || 0 == m_listPlaylist.GetCount())
	{
		AfxMessageBox(_TEXT("To edit the CDText information of a track select the track from the track list and click on this button again."));
		return;
	}

	if (nCurSel > 99)
	{
		AfxMessageBox(_TEXT("CDText information is supported for up to 99 tracks only."));
		return;
	}

	CCDTextDialog dlg;
	dlg.m_bEditAlbum = FALSE;

	dlg.SetCDText(m_Files[nCurSel].CDText);

	if (IDOK == dlg.DoModal())
	{
		dlg.GetCDText(m_Files[nCurSel].CDText);
	}
}

void CBurnerDialog::OnBnClickedAlbumCdtext()
{
	CCDTextDialog dlg;
	dlg.m_bEditAlbum = TRUE;

	dlg.SetCDText(m_AlbumCDText);

	if (IDOK == dlg.DoModal())
	{
		dlg.GetCDText(m_AlbumCDText);
	}
}

void CBurnerDialog::OnButtonAddFiles()
{
   TCHAR szFilters[]= _T("Audio Files (*.wav; *.mp3; *.ogg; *.wma; *.au; *.aiff)|*.wav;*.mp3;*.ogg;*.wma;*.au;*.aiff|All Files (*.*)|*.*||");

   CFileDialog dlg(TRUE, 0, 0, OFN_FILEMUSTEXIST | OFN_HIDEREADONLY, szFilters, this);
   OPENFILENAME& ofn = dlg.m_ofn;
   ofn.lpstrTitle = _T("Open audio file");
   ofn.Flags |= OFN_ALLOWMULTISELECT;
   const DWORD bufSize = 99 * (_MAX_PATH+1) + 1;
   WCHAR buf[bufSize];
   ofn.lpstrFile = buf;
   ofn.nMaxFile = bufSize;
   buf[0] = NULL;
   buf[bufSize - 1] = NULL;

   if(dlg.DoModal() == IDOK)
   {
	   POSITION pos = dlg.GetStartPosition();

	   while( pos )
	   {
		   CString file(dlg.GetNextPathName(pos));
		   AddFileToPlayList(file);
	   }
   }

}

void CBurnerDialog::OnButtonRemoveFiles()
{
	RemoveFilesFromPlayList(&m_listPlaylist);
}

void CBurnerDialog::AddFileToPlayList(LPCTSTR file)
{
	FileEntry fe;
	fe.FilePath = file;

	m_Files.push_back(fe);
	m_listPlaylist.AddString(file);
	m_chkHiddenTrack.EnableWindow(1 < m_listPlaylist.GetCount());
}

void CBurnerDialog::RemoveFilesFromPlayList(CListBox* pListBox)
{
	BOOL bSel=FALSE;

	do 
	{
		int nCount = pListBox->GetCount();
		bSel = FALSE;

		for (int i = 0; i < nCount; i++ )
		{
			bSel = pListBox->GetSel(i);
			if (bSel)
			{
				pListBox->DeleteString(i);
				m_Files.erase(m_Files.begin() + i);
				break;
			}
		}
	} 
	while (bSel);

	m_chkHiddenTrack.EnableWindow(1 < m_listPlaylist.GetCount());
}