#include "stdafx.h"
#include "BurnerApp.h"
#include "BurnerDialog.h"
#include "ProgressDialog.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

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
// CPacketBurnerExDlg dialog

/////////////////////////////////////////////////////////////////////////////
// CBurnerDialog dialog

CBurnerDialog::CBurnerDialog(CWnd* pParent /*=NULL*/) 
	: CDialog(CBurnerDialog::IDD, pParent)
{
	//{{AFX_DATA_INIT(CBurnerDialog)
	m_strRootDir = _TEXT("");
	m_strVolume = _TEXT("");
	m_strFreeSpace = _TEXT("");
	m_strRequiredSpace = _TEXT("");
	//}}AFX_DATA_INIT
	// Note that LoadIcon does not require a subsequent DestroyIcon in Win32

	m_ddwRequiredSpace = 0;
	m_strVolume = "DATABD";

	m_MediaProfile = MediaProfile::Unknown;

	m_hCommandThreadStartedEvent = CreateEvent(NULL, TRUE, FALSE, NULL);
	m_hProgressAvailableEvent = CreateEvent(NULL, TRUE, FALSE, NULL);
	InitializeCriticalSection(&m_csProgressUpdateGuard);

	m_ProgressInfo.Percent = 0;
	m_ProgressInfo.UsedCachePercent = 0;
	m_ProgressInfo.ActualWriteSpeed = 0;
	m_ProgressInfo.Message = _TEXT("");

	m_Burner.set_Callback(this);
}

void CBurnerDialog::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);

	DDX_Control(pDX, IDC_COMBO_SPEED, m_comboSpeed);
	DDX_Control(pDX, IDC_STATIC_REQUIRED_SPACE, m_staticRequiredSpace);
	DDX_Control(pDX, IDC_STATIC_FREE_SPACE, m_staticFreeSpace);
	DDX_Control(pDX, IDC_EDIT_VOLUME, m_editVolume);
	DDX_Control(pDX, IDC_BUTTON_BURN, m_btnBurn);
	DDX_Control(pDX, IDC_EDIT_ROOT, m_editRootDir);
	DDX_Text(pDX, IDC_EDIT_ROOT, m_strRootDir);
	DDX_Text(pDX, IDC_EDIT_VOLUME, m_strVolume);
	DDX_Text(pDX, IDC_STATIC_FREE_SPACE, m_strFreeSpace);
	DDX_Text(pDX, IDC_STATIC_REQUIRED_SPACE, m_strRequiredSpace);
	DDX_Control(pDX, IDC_STATIC_FREE_SPACE, m_staticFreeSpace);
	DDX_Control(pDX, IDC_COMBO_SPEED, m_comboSpeed);
	DDX_Control(pDX, IDC_COMBO_DEVICES, m_comboDevices);
	DDX_Control(pDX, IDC_CHECK_EJECT, m_chkEject);
	DDX_Control(pDX, IDC_CHECK_CLOSE_TRACK, m_chkCloseTrack);
	DDX_Control(pDX, IDC_CHECK_CLOSE_SESSION, m_chkCloseSession);
	DDX_Control(pDX, IDC_CHECK_CLOSE_DISC, m_chkCloseDisc);
	DDX_Control(pDX, IDC_COMBO_IMAGE_TYPE, m_comboImageType);
	DDX_Control(pDX, IDC_STATIC_MEDIA_TYPE, m_staticMediaType);
	DDX_Control(pDX, IDC_CHECK_LOAD_LAST_TRACK, m_chkLoadLastTrack);
	DDX_Control(pDX, IDC_CHECK_BD_VIDEO, m_chkBDVideo);
	DDX_Control(pDX, IDC_COMBO_FORMAT_SUBTYPE, m_comboFormatSubType);
	DDX_Control(pDX, IDC_BUTTON_FORMAT, m_btnFormat);
}

BEGIN_MESSAGE_MAP(CBurnerDialog, CDialog)
	ON_MESSAGE( WM_DEVICECHANGE, OnDeviceChange)
	ON_WM_SYSCOMMAND()
	ON_WM_CTLCOLOR()
	ON_WM_DESTROY()
	ON_CBN_SELCHANGE(IDC_COMBO_DEVICES, OnCbnSelchangeComboDevices)
	ON_EN_CHANGE(IDC_EDIT_ROOT, OnChangeEditRoot)
	ON_EN_CHANGE(IDC_EDIT_VOLUME, OnChangeEditVolume)
	ON_BN_CLICKED(IDC_BUTTON_BROWSE, OnButtonBrowseClicked)
	ON_BN_CLICKED(IDC_BUTTON_BURN, OnButtonBurnClicked)
	ON_BN_CLICKED(IDC_BUTTON_BURN_IMAGE, OnButtonBurnImageClicked)
	ON_BN_CLICKED(IDC_BUTTON_CREATE_IMAGE, OnButtonCreateImageClicked)
	ON_BN_CLICKED(IDC_BUTTON_EJECTIN, OnButtonCloseTrayClicked)
	ON_BN_CLICKED(IDC_BUTTON_EJECTOUT, OnButtonEjectClicked)
	ON_BN_CLICKED(IDC_BUTTON_FORMAT, OnButtonFormatClicked)
	ON_BN_CLICKED(IDC_CHECK_BD_VIDEO, OnBnClickedCheckBdVideo)
	ON_BN_CLICKED(IDC_CHECK_CLOSE_DISC, OnBnClickedCheckCloseDisc)
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CBurnerDialog message handlers
BOOL CBurnerDialog::OnInitDialog()
{
	CDialog::OnInitDialog();

	try
	{
		m_Burner.Open();
		const DeviceVector& devices = m_Burner.EnumerateDevices();

		m_nDeviceCount = 0;
		for (size_t i = 0; i < devices.size(); i++) 
		{
			const DeviceInfo& dev = devices[i];
			if (dev.IsWriter) 
			{
				m_comboDevices.AddString(dev.Title.c_str());
				m_DeviceIndexArray.Add(dev.Index);

				m_nDeviceCount++;
			}
		}

		if (0 == m_nDeviceCount)
			throw BurnerException(NO_WRITER_DEVICES, NO_WRITER_DEVICES_TEXT);

		// Add extra initialization here

		// Image Types
		m_comboImageType.InsertString(0, _TEXT("UDF 1.02")); m_comboImageType.SetItemData(0, UdfRevision::Revision102);
		m_comboImageType.InsertString(1, _TEXT("UDF 2.01")); m_comboImageType.SetItemData(1, UdfRevision::Revision201);
		m_comboImageType.InsertString(2, _TEXT("UDF 2.50")); m_comboImageType.SetItemData(2, UdfRevision::Revision250);

		m_comboImageType.SetCurSel(2);

		// Write parameters
		m_chkEject.SetCheck(0);

		m_chkCloseTrack.SetCheck(1);
		m_chkCloseSession.SetCheck(1);
		m_chkCloseDisc.SetCheck(0);

		// Formatting
		m_comboFormatSubType.InsertString(0, _TEXT("BD-R for Pseudo-Overwrite")); m_comboFormatSubType.SetItemData(0, BDFormatSubType::BDRSrmPow);
		m_comboFormatSubType.InsertString(1, _TEXT("BD-R with Spare Areas")); m_comboFormatSubType.SetItemData(1, BDFormatSubType::BDRSrmNoPow);
		m_comboFormatSubType.InsertString(2, _TEXT("BD-RE Quick")); m_comboFormatSubType.SetItemData(2, BDFormatSubType::BDREQuickReformat);

		m_comboFormatSubType.SetCurSel(1);

		// Multisession
		m_chkLoadLastTrack.SetCheck(0);

		// required space
		m_ddwRequiredSpace = 0;

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

	// return TRUE  unless you set the focus to a control
	return TRUE;  
}

void CBurnerDialog::OnDestroy() 
{
	CDialog::OnDestroy();
	m_Burner.Close();
}

LRESULT CBurnerDialog::OnDeviceChange(WPARAM wParam, LPARAM lParam) 
{
	UpdateDeviceInformation();
	return 0;
}

void CBurnerDialog::OnChangeEditRoot() 
{
	m_editRootDir.Invalidate(FALSE);
}

void CBurnerDialog::OnChangeEditVolume() 
{
	m_editVolume.Invalidate(FALSE);
}

void CBurnerDialog::OnCbnSelchangeComboDevices()
{
	UpdateDeviceInformation();
}

void CBurnerDialog::OnBnClickedCheckBdVideo()
{
	BOOL bChecked = (1 == m_chkBDVideo.GetCheck());
	if (bChecked)
	{
		// UDF 2.50;
		m_comboImageType.SetCurSel(2); 

		// Write parameters
		m_chkCloseTrack.SetCheck(TRUE);
		m_chkCloseSession.SetCheck(TRUE);
		m_chkCloseDisc.SetCheck(TRUE);

		// Disable multi-session
		m_chkLoadLastTrack.SetCheck(FALSE);
	}

	m_comboImageType.EnableWindow(!bChecked);

	m_chkLoadLastTrack.EnableWindow(!bChecked);

	m_chkCloseDisc.EnableWindow(!bChecked);

	BOOL bCloseDiscChecked = (1 == m_chkCloseDisc.GetCheck());
	m_chkCloseTrack.EnableWindow(!bCloseDiscChecked);
	m_chkCloseSession.EnableWindow(!bCloseDiscChecked);
}

void CBurnerDialog::OnBnClickedCheckCloseDisc()
{
	BOOL bChecked = (1 == m_chkCloseDisc.GetCheck());
	if (bChecked)
	{
		// Write parameters
		m_chkCloseTrack.SetCheck(TRUE);
		m_chkCloseSession.SetCheck(TRUE);
	}

	m_chkCloseTrack.EnableWindow(!bChecked);
	m_chkCloseSession.EnableWindow(!bChecked);
}

void CBurnerDialog::OnButtonBrowseClicked() 
{
	TCHAR szPath[MAX_PATH];
	memset(szPath, 0, sizeof(szPath));

	BROWSEINFO bi = {0};
	bi.hwndOwner = m_hWnd;
	bi.pidlRoot = NULL;
	bi.lpszTitle = _T("Select a source folder");
	bi.ulFlags = BIF_EDITBOX;
	bi.lpfn = NULL;
	bi.lParam = NULL;
	bi.pszDisplayName = szPath;

	LPITEMIDLIST pidl = ::SHBrowseForFolder(&bi);

	if (NULL == pidl)
		return;

	::SHGetPathFromIDList(pidl, szPath);
	CoTaskMemFree(pidl);
	pidl = NULL;

	m_strRootDir = szPath;

	try
	{
		CWaitCursor wc;

		int nSel = m_comboImageType.GetCurSel();
		UdfRevision::Enum udfRevision = (UdfRevision::Enum)m_comboImageType.GetItemData(nSel);

		m_ddwRequiredSpace = m_Burner.CalculateImageSize((LPCTSTR)m_strRootDir, ImageTypeFlags::Udf, udfRevision);

		m_editRootDir.SetWindowText(m_strRootDir);
		UpdateDeviceInformation();
	}
	catch(BurnerException& bme)
	{
		ShowErrorMessage(bme);
	}
}

void CBurnerDialog::OnButtonCreateImageClicked() 
{
	if (!UpdateData())
		return;

	if (!ValidateForm())
		return;

	CFileDialog dlg(false, _TEXT("*.iso"), NULL, OFN_HIDEREADONLY | OFN_OVERWRITEPROMPT | OFN_NONETWORKBUTTON,
								_TEXT("Image File (*.iso)|*.iso||"), NULL );
	if(IDOK != dlg.DoModal())
		return;

	CString strImageFile = dlg.m_ofn.lpstrFile;
	CreateImage(strImageFile);
}

void CBurnerDialog::OnButtonBurnImageClicked() 
{
	if (!UpdateData())
		return;

	CFileDialog dlg(TRUE, _TEXT("*.iso"), NULL, OFN_HIDEREADONLY|OFN_OVERWRITEPROMPT, 
							_TEXT("Image File (*.iso)|*.iso||"), NULL );
	if(IDOK != dlg.DoModal())
		return;

	CString strImageFile = dlg.m_ofn.lpstrFile; 
	HANDLE hFile = CreateFile(strImageFile, GENERIC_READ, 
									FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, 
									OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);

	if (hFile == INVALID_HANDLE_VALUE) 
	{
		CString str;
		str.Format(_TEXT("Unable to open file %s"), strImageFile);
		AfxMessageBox(str);
		return;
	}

	DWORD dwFileSizeHigh = 0;
	DWORD dwFileSize = GetFileSize(hFile, &dwFileSizeHigh);
	CloseHandle(hFile);

	ULARGE_INTEGER uliFileSize;
	uliFileSize.HighPart = dwFileSizeHigh; 
	uliFileSize.LowPart = dwFileSize;

	DWORD64 ddwFileSize = uliFileSize.QuadPart;
	if (ddwFileSize > m_ddwCapacity) 
	{
		CString str;
		str.Format(_TEXT("Cannot write image file %s.\nThe file is too big."), strImageFile);
		AfxMessageBox(str);
		return;
	}


	BurnImage(strImageFile);
	UpdateDeviceInformation();
}

void CBurnerDialog::OnButtonBurnClicked() 
{
	if (!UpdateData())
		return;

	if (!ValidateForm())
		return;

	Burn();
	UpdateDeviceInformation();
}

void CBurnerDialog::OnButtonFormatClicked()
{
	Format();
	UpdateDeviceInformation();
}

void CBurnerDialog::OnButtonEjectClicked()
{
	int deviceSelection = m_comboDevices.GetCurSel();
	int deviceIndex = (-1 == deviceSelection) ? -1 : m_DeviceIndexArray[deviceSelection];

	try
	{
		m_Burner.SelectDevice(deviceIndex, true);
			m_Burner.Eject();
		m_Burner.ReleaseDevice();
	}
	catch(BurnerException& bme)
	{
		m_Burner.ReleaseDevice();
		ShowErrorMessage(bme);
	}
}

void CBurnerDialog::OnButtonCloseTrayClicked()
{
	int deviceSelection = m_comboDevices.GetCurSel();
	int deviceIndex = (-1 == deviceSelection) ? -1 : m_DeviceIndexArray[deviceSelection];

	try
	{
		m_Burner.SelectDevice(deviceIndex, true);
			m_Burner.CloseTray();
		m_Burner.ReleaseDevice();
	}
	catch(BurnerException& bme)
	{
		m_Burner.ReleaseDevice();
		ShowErrorMessage(bme);
	}
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
//////////////////////////////////
// Implementation
void CBurnerDialog::CreateImage(CString strImageFile)
{
	CreateProgressWindow();

		CreateImageSettings settings;
		{
			settings.ImageFile = (LPCTSTR)strImageFile;
			settings.SourceFolder = (LPCTSTR)m_strRootDir;

			int nSel = m_comboImageType.GetCurSel();

			settings.ImageType = ImageTypeFlags::Udf;
			settings.UdfRevision = (UdfRevision::Enum)m_comboImageType.GetItemData(nSel);
			settings.BDVideo = (BST_CHECKED == m_chkBDVideo.GetCheck());

			settings.VolumeLabel = (LPCTSTR)m_strVolume;
		}
		
		CreateImageThreadParam threadParam;
		{
			threadParam.pThis = this;
			threadParam.Settings = settings;
		}

		ResetEvent(m_hProgressAvailableEvent);
		ResetEvent(m_hCommandThreadStartedEvent);

		DWORD dwThreadID = 0;
		m_hCommandThread = CreateThread(NULL, NULL, CreateImageThread, &threadParam, NULL, &dwThreadID);
		WaitForSingleObject(m_hCommandThreadStartedEvent, INFINITE);

		WaitForThreadToFinish();

	DestroyProgressWindow();
}

DWORD CBurnerDialog::CreateImageThread(LPVOID pVoid) 
{
	CreateImageThreadParam* pParam = (CreateImageThreadParam*)pVoid;
	CBurnerDialog* pThis = pParam->pThis;
	CreateImageSettings settings = pParam->Settings;

	SetEvent(pThis->m_hCommandThreadStartedEvent);
	// Do not use pParam after this line

	try
	{
		pThis->m_Burner.CreateImage(settings);
	}
	catch(BurnerException& bme)
	{
		pThis->m_ThreadException = bme;
		return THREAD_EXCEPTION;
	}

	return NOERROR;
}

void CBurnerDialog::BurnImage(CString strImageFile)
{
	int deviceSelection = m_comboDevices.GetCurSel();
	int deviceIndex = (-1 == deviceSelection) ? -1 : m_DeviceIndexArray[deviceSelection];

	try
	{
		m_Burner.SelectDevice(deviceIndex, true);

		CreateProgressWindow();

			BurnImageSettings settings;
			{
				settings.ImageFile = (LPCTSTR)strImageFile;

				int nSel = m_comboSpeed.GetCurSel();
				settings.WriteSpeedKb = (uint32_t)m_comboSpeed.GetItemData(nSel);

				settings.CloseTrack = (BST_CHECKED == m_chkCloseTrack.GetCheck());
				settings.CloseSession = (BST_CHECKED == m_chkCloseSession.GetCheck());
				settings.CloseDisc = (BST_CHECKED == m_chkCloseDisc.GetCheck());

				settings.Eject = (BST_CHECKED == m_chkEject.GetCheck());
			}

			BurnImageThreadParam threadParam;
			{
				threadParam.pThis = this;
				threadParam.Settings = settings;
			}

			ResetEvent(m_hProgressAvailableEvent);
			ResetEvent(m_hCommandThreadStartedEvent);

			DWORD dwThreadID = 0;
			m_hCommandThread = CreateThread(NULL, NULL, BurnImageThread, &threadParam, NULL, &dwThreadID);
			WaitForSingleObject(m_hCommandThreadStartedEvent, INFINITE);

			WaitForThreadToFinish();

		DestroyProgressWindow();
		m_Burner.ReleaseDevice();
	}
	catch(BurnerException& bme)
	{
		m_Burner.ReleaseDevice();
		ShowErrorMessage(bme);
	}
}

DWORD CBurnerDialog::BurnImageThread(LPVOID pVoid) 
{
	BurnImageThreadParam* pParam = (BurnImageThreadParam*)pVoid;

	CBurnerDialog* pThis = pParam->pThis;
	BurnImageSettings settings = pParam->Settings;

	SetEvent(pThis->m_hCommandThreadStartedEvent);
	// Do not use pParam after this line

	try
	{
		pThis->m_Burner.BurnImage(settings);
	}
	catch(BurnerException& bme)
	{
		pThis->m_ThreadException = bme;
		return THREAD_EXCEPTION;
	}

	return NOERROR;
}

void CBurnerDialog::Burn()
{
	int deviceSelection = m_comboDevices.GetCurSel();
	int deviceIndex = (-1 == deviceSelection) ? -1 : m_DeviceIndexArray[deviceSelection];

	try
	{
		m_Burner.SelectDevice(deviceIndex, true);

		CreateProgressWindow();

			BurnSettings settings;
			{
				settings.SourceFolder = (LPCTSTR)m_strRootDir;

				settings.ImageType = ImageTypeFlags::Udf;

				int nSel = m_comboImageType.GetCurSel();
				settings.UdfRevision = (UdfRevision::Enum)m_comboImageType.GetItemData(nSel);

				settings.BDVideo = (BST_CHECKED == m_chkBDVideo.GetCheck());

				settings.VolumeLabel = (LPCTSTR)m_strVolume;

				nSel = m_comboSpeed.GetCurSel();
				settings.WriteSpeedKb = (uint32_t)m_comboSpeed.GetItemData(nSel);

				settings.CloseTrack = (BST_CHECKED == m_chkCloseTrack.GetCheck());
				settings.CloseSession = (BST_CHECKED == m_chkCloseSession.GetCheck());
				settings.CloseDisc = (BST_CHECKED == m_chkCloseDisc.GetCheck());

				settings.LoadLastTrack = (BST_CHECKED == m_chkLoadLastTrack.GetCheck());

				settings.Eject = (BST_CHECKED == m_chkEject.GetCheck());
			}

			BurnThreadParam threadParam;
			{
				threadParam.pThis = this;
				threadParam.Settings = settings;
			}

			ResetEvent(m_hProgressAvailableEvent);
			ResetEvent(m_hCommandThreadStartedEvent);

			DWORD dwThreadID = 0;
			m_hCommandThread = CreateThread(NULL, NULL, BurnThread, &threadParam, NULL, &dwThreadID);
			WaitForSingleObject(m_hCommandThreadStartedEvent, INFINITE);

			WaitForThreadToFinish();
		
		DestroyProgressWindow();
		m_Burner.ReleaseDevice();
	}
	catch(BurnerException& bme)
	{
		m_Burner.ReleaseDevice();
		ShowErrorMessage(bme);
	}
}

DWORD CBurnerDialog::BurnThread(LPVOID pVoid) 
{
	BurnThreadParam* pParam = (BurnThreadParam*)pVoid;
	CBurnerDialog* pThis = pParam->pThis;
	BurnSettings settings = pParam->Settings;

	SetEvent(pThis->m_hCommandThreadStartedEvent);
	// Do not use pParam after this line

	try
	{
		pThis->m_Burner.Burn(settings);
	}
	catch(BurnerException& bme)
	{
		pThis->m_ThreadException = bme;
		return THREAD_EXCEPTION;
	}

	return NOERROR;
}

void CBurnerDialog::Format()
{
	int deviceSelection = m_comboDevices.GetCurSel();
	int deviceIndex = (-1 == deviceSelection) ? -1 : m_DeviceIndexArray[deviceSelection];

	try
	{
		m_Burner.SelectDevice(deviceIndex, true);

		if (m_Burner.get_MediaIsFullyFormatted())
			if (IDCANCEL == AfxMessageBox(_TEXT("Media is already formatted. Do you want to format it again?"), MB_OKCANCEL))
			{
				m_Burner.ReleaseDevice();
				return;
			}

		if (IDCANCEL == AfxMessageBox(_TEXT("Formatting will destroy all the information on the disc. Do you want to continue?"), MB_OKCANCEL))
		{
			m_Burner.ReleaseDevice();
			return;
		}

		CreateProgressWindow();

		m_ProgressWindow.GetDlgItem(IDOK)->EnableWindow(FALSE);
		m_ProgressWindow.SetStatus(_TEXT("Formatting. Please wait..."));

			FormatSettings settings;
			{
				settings.Type = BDFormatType::BDFull;

				int nSel = m_comboFormatSubType.GetCurSel();
				settings.SubType = (BDFormatSubType::Enum)m_comboFormatSubType.GetItemData(nSel);
			}
			
			FormatThreadParam threadParam;
			{
				threadParam.pThis = this;
				threadParam.Settings = settings;
			}

			ResetEvent(m_hProgressAvailableEvent);
			ResetEvent(m_hCommandThreadStartedEvent);

			DWORD dwThreadID = 0;
			m_hCommandThread = CreateThread(NULL, NULL, FormatThread, &threadParam, NULL, &dwThreadID);
			WaitForSingleObject(m_hCommandThreadStartedEvent, INFINITE);

			WaitForThreadToFinish();

		DestroyProgressWindow();
		m_Burner.ReleaseDevice();
	}
	catch(BurnerException& bme)
	{
		m_Burner.ReleaseDevice();
		ShowErrorMessage(bme);
	}
}

DWORD CBurnerDialog::FormatThread(LPVOID pVoid) 
{
	FormatThreadParam* pParam = (FormatThreadParam*)pVoid;
	CBurnerDialog* pThis = pParam->pThis;
	FormatSettings settings = pParam->Settings;

	SetEvent(pThis->m_hCommandThreadStartedEvent);
	// Do not use pParam after this line

	try
	{
		pThis->m_Burner.Format(settings);
	}
	catch(BurnerException& bme)
	{
		pThis->m_ThreadException = bme;
		return THREAD_EXCEPTION;
	}

	return NOERROR;
}

void CBurnerDialog::WaitForThreadToFinish()
{
	while (WAIT_TIMEOUT == WaitForSingleObject(m_hCommandThread, 50)) 
	{
		ProcessMessages();

		if (WAIT_OBJECT_0 == WaitForSingleObject(m_hProgressAvailableEvent, 0)) 
		{
			ResetEvent(m_hProgressAvailableEvent);
			EnterCriticalSection(&m_csProgressUpdateGuard);
				UpdateProgress(m_ProgressInfo);
			LeaveCriticalSection(&m_csProgressUpdateGuard);
		}
	}

	// See if we had any errors: 
	// Exception would be saved in m_ThreadException and the exit code would be set to THREAD_EXCEPTION
	DWORD dwExitCode = 0; GetExitCodeThread(m_hCommandThread, &dwExitCode);
	if (THREAD_EXCEPTION == dwExitCode)
		ShowErrorMessage(m_ThreadException);
}

void CBurnerDialog::ProcessMessages()
{
	MSG msg;
	while (::PeekMessage(&msg, 0, 0, 0, PM_REMOVE))
		::DispatchMessage(&msg);
}

// Event handlers
void CBurnerDialog::OnImageProgress(DWORD64 ddwPos, DWORD64 ddwAll) 
{
	EnterCriticalSection(&m_csProgressUpdateGuard);

		double dd = (double)(__int64)ddwPos * 100.0 / (double)(__int64)ddwAll;
		m_ProgressInfo.Percent = (int)dd;

		try
		{
			// get device internal buffer
			dd = (double)m_Burner.get_DeviceCacheUsedSize();
			dd = dd * 100.0 / (double)m_Burner.get_DeviceCacheSize();
			m_ProgressInfo.UsedCachePercent = (int)dd;

			// get actual write speed (KB/s)
			m_ProgressInfo.ActualWriteSpeed = m_Burner.get_WriteTransferKB();
		}
		catch (...)
		{
		}

	LeaveCriticalSection(&m_csProgressUpdateGuard);
	SetEvent(m_hProgressAvailableEvent);
}

void CBurnerDialog::OnFileProgress(int file, const tstring& fileName, int percentCompleted) 
{
	// Do nothing
}

bool CBurnerDialog::OnContinue() 
{
	return 0 == m_ProgressWindow.m_bStopped;
}

void CBurnerDialog::OnFormatProgress(DOUBLE fPercentCompleted)
{
	EnterCriticalSection(&m_csProgressUpdateGuard);
		m_ProgressInfo.Message = "Formatting...";
		m_ProgressInfo.Percent = (int)fPercentCompleted;
	LeaveCriticalSection(&m_csProgressUpdateGuard);
	SetEvent(m_hProgressAvailableEvent);
}

void CBurnerDialog::OnEraseProgress(DOUBLE fPercentCompleted)
{
	EnterCriticalSection(&m_csProgressUpdateGuard);
		m_ProgressInfo.Message = "Erasing...";
		m_ProgressInfo.Percent = (int)fPercentCompleted;
	LeaveCriticalSection(&m_csProgressUpdateGuard);
	SetEvent(m_hProgressAvailableEvent);
}

void CBurnerDialog::OnStatus(const tstring& message)
{
	EnterCriticalSection(&m_csProgressUpdateGuard);
		m_ProgressInfo.Message = message.c_str();
	LeaveCriticalSection(&m_csProgressUpdateGuard);
	SetEvent(m_hProgressAvailableEvent);
}

void CBurnerDialog::ShowErrorMessage(BurnerException& burnerException)
{
	CString msg;
	msg.Format(_T("%s (0x%x)"),burnerException.get_Message().c_str(), burnerException.get_Error());
	AfxMessageBox(msg);
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
	m_ProgressWindow.SetActualWriteSpeed(progressInfo.ActualWriteSpeed);
}

void CBurnerDialog::UpdateDeviceInformation()
{
	int nCurSel = m_comboDevices.GetCurSel();
	if (-1 == nCurSel)
		return;

	try
	{
		// Select device. Exclusive access is not required.
		m_Burner.SelectDevice(m_DeviceIndexArray[nCurSel], false);

		// Get and display the media profile
		m_MediaProfile = m_Burner.get_MediaProfile();
		m_ddwCapacity = (DWORD64)m_Burner.get_MediaFreeSpace() * BlockSize::DVD;

		int nMaxSpeedKB = m_Burner.get_MaxWriteSpeedKB();
		const SpeedVector& speeds = m_Burner.EnumerateWriteSpeeds();

		// media profile
		m_staticMediaType.SetWindowText(m_Burner.get_MediaProfileString().c_str());

		// Required space
		CString str;
		str.Format(_TEXT("Required space : %.2fGB"), ((double)(__int64)m_ddwRequiredSpace) / 1e9);
		m_staticRequiredSpace.SetWindowText(str);

		// Capacity
		str.Format(_TEXT("Free space : %.2fGB"), ((double)(__int64)m_ddwCapacity) / 1e9);
		m_staticFreeSpace.SetWindowText(str);

		// Speed
		int nCurSel = m_comboSpeed.GetCurSel();
		int nCurSpeedKB = (int32_t)m_comboSpeed.GetItemData(nCurSel);

		m_comboSpeed.ResetContent();
		for (size_t i = 0; i < speeds.size(); i++) 
		{
			const Speed& speed = speeds[i];
				
			CString sSpeed;
			sSpeed.Format(_TEXT("%1.0fx"), (double)speed.TransferRateKB / speed.TransferRate1xKB);
	
			m_comboSpeed.InsertString(-1, sSpeed);
			m_comboSpeed.SetItemData((int)i, speed.TransferRateKB);
		}

		// Restore speed selection
		if (-1 != nCurSel && nCurSpeedKB <= nMaxSpeedKB)
		{
			CString sSpeed;
			sSpeed.Format(_TEXT("%dx"), nCurSpeedKB / Speed1xKB::BD);
			m_comboSpeed.SelectString(-1, sSpeed);
		}

		if (-1 == m_comboSpeed.GetCurSel())
			m_comboSpeed.SetCurSel(0);

		// burn button
		m_btnBurn.EnableWindow(m_ddwCapacity > 0 && m_ddwCapacity >= m_ddwRequiredSpace);

		// format button and subtype
		bool enableFormat = MediaProfile::BDRSrm == m_MediaProfile && !m_Burner.get_MediaIsFullyFormatted();

		m_btnFormat.EnableWindow(enableFormat);

		m_comboFormatSubType.EnableWindow(enableFormat);
		m_comboFormatSubType.SetCurSel(MediaProfile::BDRE == m_MediaProfile ? 2 : 0);

		m_Burner.ReleaseDevice();
	}
	catch(BurnerException& bme)
	{
		// Ignore the error it is DEVICE_ALREADY_SELECTED
		if (DEVICE_ALREADY_SELECTED == bme.get_Error())
			return;
		
		// Report all other errors
		m_Burner.ReleaseDevice();
		ShowErrorMessage(bme);
	}
}

BOOL CBurnerDialog::DirectoryExists()
{
	TCHAR tcs[1024];
	m_editRootDir.GetWindowText(tcs, 1024);

	if (_taccess(tcs, 0)!=-1)
		return TRUE;
	else
		return FALSE;
}

BOOL CBurnerDialog::ValidateForm() 
{
	if (!DirectoryExists()) 
	{
		AfxMessageBox(_TEXT("Please specify a valid root directory."));
		m_editRootDir.SetFocus();
		return FALSE;
	}

	if (m_strVolume.GetLength() > 16) 
	{
		AfxMessageBox(_TEXT("Volume name can contain maximum of 16 characters."));
		m_editVolume.SetFocus();
		return FALSE;
	}

	if (_TEXT('\\') == m_strRootDir[m_strRootDir.GetLength() - 1] || 
		_TEXT('/') == m_strRootDir[m_strRootDir.GetLength() - 1]) 
	{
		m_strRootDir = m_strRootDir.Left(m_strRootDir.GetLength() - 1);
	}

	return TRUE;
}


