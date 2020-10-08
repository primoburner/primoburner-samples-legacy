#include "stdafx.h"
#include "BurnerApp.h"
#include "BurnerDialog.h"
#include "ProgressDialog.h"
#include "NewDiscWaitDlg.h"
#include "resource.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CBurnerDialog dialog

CBurnerDialog::CBurnerDialog(CWnd* pParent /*=NULL*/) 
	: CDialog(CBurnerDialog::IDD, pParent)
	, m_nSelectedCopyMode(0)
{
	//{{AFX_DATA_INIT(CBurnerDialog)

	m_hCommandThreadStartedEvent = CreateEvent(NULL, TRUE, FALSE, NULL);
	m_hProgressAvailableEvent = CreateEvent(NULL, TRUE, FALSE, NULL);
	InitializeCriticalSection(&m_csProgressUpdateGuard);

	m_ProgressInfo.Percent = 0;
	m_ProgressInfo.UsedCachePercent = 0;
	m_ProgressInfo.ActualWriteSpeed = 0;
	m_ProgressInfo.Message = _TEXT("");

	m_bInProcess = FALSE;
	m_nSelectedCopyMode = 0;

	m_Burner.set_Callback(this);
}

void CBurnerDialog::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);

	DDX_Control(pDX, IDC_COMBO_SRC_DEVICES, m_comboSrcDevices);
	DDX_Control(pDX, IDC_COMBO_DST_DEVICES, m_comboDstDevices);
	DDX_Control(pDX, IDC_COMBO_MODE, m_cmbWriteMethods);
	DDX_Control(pDX, IDC_COPY, m_btnCopy);
	DDX_Control(pDX, IDC_READ_SUBCHANNEL, m_chkReadSubChannel);
	DDX_Control(pDX, IDC_USE_TEMPORARY_FILES, m_chkUseTemporaryFiles);
	DDX_Control(pDX, IDC_EDIT_ROOT, m_editRootDir);
	DDX_Radio(pDX, IDC_RADIO_SIMPLE_COPY_MODE, m_nSelectedCopyMode);
	DDX_Control(pDX, IDC_BUTTON_BROWSE, m_btnBrowse);
}

BEGIN_MESSAGE_MAP(CBurnerDialog, CDialog)
	//{{AFX_MSG_MAP(CBurnerDialog)
	ON_MESSAGE( WM_DEVICECHANGE, OnDeviceChange)
	ON_WM_CTLCOLOR()
	ON_WM_DESTROY()
	ON_CBN_SELCHANGE(IDC_COMBO_SRC_DEVICES, OnCbnSelectionChangeSrcDevices)
	ON_CBN_SELCHANGE(IDC_COMBO_DST_DEVICES, OnCbnSelectionChangeDstDevices)
	ON_BN_CLICKED(IDC_BUTTON_BROWSE, OnButtonBrowseClicked)
	ON_BN_CLICKED(IDC_COPY, &CBurnerDialog::OnButtonCopyClicked)
	//}}AFX_MSG_MAP
	ON_BN_CLICKED(IDC_RADIO_SIMPLE_COPY_MODE, &CBurnerDialog::OnBnClickedRadioCopyMode)
	ON_BN_CLICKED(IDC_RADIO_DIRECT_COPY_MODE, &CBurnerDialog::OnBnClickedRadioCopyMode)
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CBurnerDialog message handlers
BOOL CBurnerDialog::OnInitDialog()
{
	CDialog::OnInitDialog();
	OnBnClickedRadioCopyMode();

	try
	{
		int nDeviceCount = 0;

		m_Burner.Open();

		const DeviceVector& devices = m_Burner.EnumerateDevices();
		for (size_t i = 0; i < devices.size(); i++) 
		{
			const DeviceInfo& dev = devices[i];
			if (dev.IsWriter) 
			{
				int srcIndex = m_comboSrcDevices.AddString(dev.Title.c_str());
				if (0 <= srcIndex)
					m_comboSrcDevices.SetItemData(srcIndex, dev.Index);
				int dstIndex = m_comboDstDevices.AddString(dev.Title.c_str());
				if (0 <= dstIndex)
					m_comboDstDevices.SetItemData(dstIndex, dev.Index);

				nDeviceCount++;
			}
		}

		if (0 == nDeviceCount)
			throw BurnerException(NO_WRITER_DEVICES, NO_WRITER_DEVICES_TEXT);

		// Device combo
		m_comboSrcDevices.SetCurSel(0);
		m_comboDstDevices.SetCurSel(0);
		UpdateDeviceInformation();
		m_chkUseTemporaryFiles.SetCheck(BST_CHECKED);

	}
	catch(BurnerException& bme)
	{
		ShowErrorMessage(bme);

		EndModalLoop(-1);
		return FALSE;
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

void CBurnerDialog::OnCbnSelectionChangeSrcDevices()
{
	UpdateDeviceInformation();
}

void CBurnerDialog::OnCbnSelectionChangeDstDevices()
{
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

	m_strImagePath = szPath;

	m_editRootDir.SetWindowText(m_strImagePath);

	UpdateDeviceInformation();
}


void CBurnerDialog::OnButtonCopyClicked()
{
	UpdateData();
	
	if (!ValidateForm())
	{
		return;
	}
	m_bInProcess = TRUE;
	
	switch (m_nSelectedCopyMode)
	{
		case 0:
			RunSimpleCopy();
			break;
		case 1:
			RunDirectCopy();
			break;
	}
	
	m_bInProcess = FALSE;

	UpdateDeviceInformation();
}
void CBurnerDialog::OnBnClickedRadioCopyMode()
{
	UpdateData();
	BOOL enableDst = 1 == m_nSelectedCopyMode;
	m_comboDstDevices.EnableWindow(enableDst);
	m_chkUseTemporaryFiles.EnableWindow(enableDst);
	m_btnBrowse.EnableWindow(!enableDst);
}
//
void CBurnerDialog::OnStatus(const tstring& message)
{
	EnterCriticalSection(&m_csProgressUpdateGuard);
		m_ProgressInfo.Message = message.c_str();
	LeaveCriticalSection(&m_csProgressUpdateGuard);
	SetEvent(m_hProgressAvailableEvent);
}

void CBurnerDialog::OnCopyProgress(DWORD64 ddwPos, DWORD64 ddwAll) 
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

void CBurnerDialog::OnTrackStatus(int nTrack, int nPercent) 
{
	// Do nothing
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

bool CBurnerDialog::OnContinue() 
{
	return 0 == m_ProgressWindow.m_bStopped;
}
//////////////////////////////////
// Implementation
DWORD CBurnerDialog::CreateImage()
{
	int deviceIndex = GetSelectedDeviceIndex();

	try
	{
		m_Burner.SelectDevice(deviceIndex, true, false);
		CreateProgressWindow();

		CreateImageSettings settings;
		{
			settings.ImageFolderPath		= (LPCTSTR)m_strImagePath;
			settings.ReadSubChannel			= BST_CHECKED == m_chkReadSubChannel.GetCheck();
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

		DWORD res = WaitForThreadToFinish();

		DestroyProgressWindow();
		m_Burner.ReleaseDevices();
		return res;
	}
	catch(BurnerException& bme)
	{
		m_Burner.ReleaseDevices();
		ShowErrorMessage(bme);
		return OPERATION_FAILED;
	}
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

DWORD CBurnerDialog::BurnImage()
{
	int deviceIndex = GetSelectedDeviceIndex();

	try
	{
		m_Burner.SelectDevice(deviceIndex, true, false);
		CreateProgressWindow();

		BurnImageSettings settings;
		{
			settings.ImageFolderPath		= (LPCTSTR)m_strImagePath;
			settings.WriteMethod			= GetWriteMethod();
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

		DWORD res = WaitForThreadToFinish();

		DestroyProgressWindow();
		m_Burner.ReleaseDevices();

		return res;
	}
	catch(BurnerException& bme)
	{
		m_Burner.ReleaseDevices();
		ShowErrorMessage(bme);
		return OPERATION_FAILED;
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

DWORD CBurnerDialog::CleanMedia(const CleanMediaSettings& settings)
{
	int deviceIndex = GetSelectedDeviceIndex();

	try
	{
		m_Burner.SelectDevice(deviceIndex, true, false);

		if (m_Burner.get_MediaIsBlank())
		{
			UINT msgId = 0;
			switch (settings.MediaCleanMethod)
			{
			case CM_Erase:
				msgId = IDS_ERASE_BLANK;
				break;
			case CM_Format:
				msgId = IDS_FORMAT_BLANK;
				break;
			default:
				return TRUE;
			}
			if (IDCANCEL == AfxMessageBox(msgId, MB_OKCANCEL))
			{
				m_Burner.ReleaseDevices();
				return NOERROR;
			}
		}
		if (IDCANCEL == AfxMessageBox(_TEXT("Continuing will destroy all information on the medium. Do you still wish to proceed?"), MB_OKCANCEL))
		{
			m_Burner.ReleaseDevices();
			return DISCCOPY_MEDIA_CLEAN_CANCEL;
		}

		CreateProgressWindow();
		
		m_ProgressWindow.GetDlgItem(IDOK)->EnableWindow(FALSE);
		m_ProgressWindow.SetStatus("Erasing. Please wait...");

		CleanMediaThreadParam threadParam;
		{
			threadParam.pThis = this;
			threadParam.Settings = settings;
		}

		ResetEvent(m_hProgressAvailableEvent);
		ResetEvent(m_hCommandThreadStartedEvent);

		DWORD dwThreadID = 0;
		m_hCommandThread = CreateThread(NULL, NULL, CleanMediaThread, &threadParam, NULL, &dwThreadID);
		WaitForSingleObject(m_hCommandThreadStartedEvent, INFINITE);

		DWORD res = WaitForThreadToFinish();

		DestroyProgressWindow();
		m_Burner.ReleaseDevices();
		return res;
	}
	catch(BurnerException& bme)
	{
		m_Burner.ReleaseDevices();
		ShowErrorMessage(bme);
		return OPERATION_FAILED; 
	}
}

DWORD CBurnerDialog::CleanMediaThread(LPVOID pVoid) 
{
	CleanMediaThreadParam* pParam = (CleanMediaThreadParam*)pVoid;
	CBurnerDialog* pThis = pParam->pThis;
	CleanMediaSettings settings = pParam->Settings;

	SetEvent(pThis->m_hCommandThreadStartedEvent);
	try
	{
		pThis->m_Burner.CleanMedia(settings);
	}
	catch(BurnerException& bme)
	{
		pThis->m_ThreadException = bme;
		return THREAD_EXCEPTION;
	}

	return NO_ERROR;
}

DWORD CBurnerDialog::WaitForThreadToFinish()
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
	return dwExitCode;
}

void CBurnerDialog::ProcessMessages()
{
	MSG msg;
	while (::PeekMessage(&msg, 0, 0, 0, PM_REMOVE))
		::DispatchMessage(&msg);
}

CDCopyWriteMethod::Enum CBurnerDialog::GetWriteMethod()
{
	int sel = m_cmbWriteMethods.GetCurSel();
	if (CB_ERR != sel)
	{
		DWORD_PTR res = m_cmbWriteMethods.GetItemData(sel);
		if (CB_ERR != res)
		{
			return (CDCopyWriteMethod::Enum)res;
		}
	}
	return CDCopyWriteMethod::Cooked;
}
//
//

DWORD CBurnerDialog::PrepareNewMedium()
{
	int deviceIndex = GetSelectedDeviceIndex();

	try
	{
		m_Burner.SelectDevice(deviceIndex, false, false);

		CNewDiscWaitDlg dlg;
		dlg.SetBurner(m_Burner);
		DWORD res = dlg.DoModal();
		if (1 == res)
		{
			if (m_Burner.get_MediaIsRewritable() || m_Burner.get_BDRFormatAllowed())
			{
				CleanMediaSettings settings;
				{
					//settings.DeviceToClean = WD_Source;
					settings.Quick =  dlg.get_QuickClean();
					settings.MediaCleanMethod = dlg.get_SelectedCleanMethod();
				}
				m_Burner.ReleaseDevices();
				return CleanMedia(settings);
			}
			else
			{
				bool isBlank = m_Burner.get_MediaIsBlank();
				m_Burner.ReleaseDevices();
				return isBlank ? NOERROR : MEDIA_TYPE_INCOMPATBLE;
			}
		}
		else
		{
			m_Burner.ReleaseDevices();
			return DISCCOPY_ONNEWDISCWAIT_CANCEL;
		}
	}
	catch(BurnerException& bme)
	{
		m_Burner.ReleaseDevices();
		ShowErrorMessage(bme);
		return OPERATION_FAILED;
	}
}
//
void CBurnerDialog::ShowErrorMessage(BurnerException& burnerException)
{
	AfxMessageBox(burnerException.get_Message().c_str());
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
	if (m_bInProcess)
		return;
	int deviceIndex = GetSelectedDeviceIndex();
	if (-1 == deviceIndex)
		return;

	try
	{
		// Select device. Exclusive access is not required.
		m_Burner.SelectDevice(deviceIndex, false, false);

		BOOL copyAllowed = m_Burner.get_MediaIsValidProfile();
		m_btnCopy.EnableWindow(copyAllowed);

		BOOL showReadSettings = FALSE;
		BOOL showWriteSettings = m_Burner.get_MediaIsCD();
		if (copyAllowed)
		{
			showReadSettings = m_Burner.get_MediaCanReadSubChannel();
		}
		m_chkReadSubChannel.EnableWindow(showReadSettings);
		m_cmbWriteMethods.EnableWindow(showWriteSettings);

		CDCopyWriteMethod::Enum method = GetWriteMethod();
		m_cmbWriteMethods.ResetContent();
		int selectedItm = 0;
		if (m_Burner.get_RawDaoPossible())
		{
			m_cmbWriteMethods.AddString(_TEXT("Disc-At-Once"));
			m_cmbWriteMethods.SetItemData(m_cmbWriteMethods.GetCount()-1, CDCopyWriteMethod::Raw);
			if (method == CDCopyWriteMethod::Raw)
			{
				selectedItm = m_cmbWriteMethods.GetCount()-1;
			}
			m_cmbWriteMethods.AddString(_TEXT("Disc-At-Once 2352 Bytes/sector"));
			m_cmbWriteMethods.SetItemData(m_cmbWriteMethods.GetCount()-1, CDCopyWriteMethod::Raw2352);
			if (method == CDCopyWriteMethod::Raw2352)
			{
				selectedItm = m_cmbWriteMethods.GetCount()-1;
			}
			m_cmbWriteMethods.AddString(_TEXT("Disc-At-Once + SUB 2448 Bytes/sector"));
			m_cmbWriteMethods.SetItemData(m_cmbWriteMethods.GetCount()-1, CDCopyWriteMethod::FullRaw);
			if (method == CDCopyWriteMethod::FullRaw)
			{
				selectedItm = m_cmbWriteMethods.GetCount()-1;
			}
		}
		m_cmbWriteMethods.AddString(_TEXT("SAO/TAO/Packet"));
		m_cmbWriteMethods.SetItemData(m_cmbWriteMethods.GetCount()-1, CDCopyWriteMethod::Cooked);
		if (method == CDCopyWriteMethod::Cooked)
		{
			selectedItm = m_cmbWriteMethods.GetCount()-1;
		}
		m_cmbWriteMethods.SetCurSel(selectedItm);	


		m_Burner.ReleaseDevices();
	}
	catch(BurnerException& bme)
	{
		// Ignore the error when it is DEVICE_ALREADY_SELECTED
		if (DEVICE_ALREADY_SELECTED == bme.get_Error())
			return;
		
		// Report all other errors
		m_Burner.ReleaseDevices();
		ShowErrorMessage(bme);
	}
}

BOOL CBurnerDialog::DirectoryExists()
{
	BOOL bExist = FALSE;
	UpdateData();

	DWORD dwAttrib = GetFileAttributes(m_strImagePath);

	return (dwAttrib != INVALID_FILE_ATTRIBUTES &&
		(dwAttrib & FILE_ATTRIBUTE_DIRECTORY));
}

BOOL CBurnerDialog::ValidateForm() 
{
	if (1 != m_nSelectedCopyMode && !DirectoryExists()) 
	{
		AfxMessageBox(_TEXT("Please specify a valid root directory."));
		return FALSE;
	}

	return TRUE;
}
int CBurnerDialog::GetSelectedDeviceIndex()
{
	return GetSelectedDeviceIndex(false);
}
int CBurnerDialog::GetSelectedDeviceIndex(bool dstDevice)
{
	CComboBox& combo = dstDevice ? m_comboDstDevices : m_comboSrcDevices;
	int deviceSelection = combo.GetCurSel();
	DWORD_PTR item = combo.GetItemData(deviceSelection);
	int deviceIndex = (CB_ERR == deviceSelection) ? -1 : (int)item;
	return deviceIndex;
}

void CBurnerDialog::RunSimpleCopy()
{
	if (NOERROR == CreateImage())
	{
		if (NOERROR == PrepareNewMedium())
		{
			BurnImage();
		}
	}
}

void CBurnerDialog::RunDirectCopy()
{
	int srcDeviceIndex = GetSelectedDeviceIndex();
	int dstDeviceIndex = GetSelectedDeviceIndex(true);

	try
	{
		m_Burner.SelectDevice(srcDeviceIndex, true, false);
		m_Burner.SelectDevice(dstDeviceIndex, true, true);
		CreateProgressWindow();

		DirectCopySettings settings;
		{
			settings.ReadSubChannel			= BST_CHECKED == m_chkReadSubChannel.GetCheck();
			settings.WriteMethod			= GetWriteMethod();
			settings.UseTemporaryFiles		= BST_CHECKED == m_chkUseTemporaryFiles.GetCheck();
		}

		DirectCopyThreadParam threadParam;
		{
			threadParam.pThis = this;
			threadParam.Settings = settings;
		}

		ResetEvent(m_hProgressAvailableEvent);
		ResetEvent(m_hCommandThreadStartedEvent);

		DWORD dwThreadID = 0;
		m_hCommandThread = CreateThread(NULL, NULL, DirectCopyThread, &threadParam, NULL, &dwThreadID);
		WaitForSingleObject(m_hCommandThreadStartedEvent, INFINITE);

		DWORD res = WaitForThreadToFinish();

		DestroyProgressWindow();
		m_Burner.ReleaseDevices();
	}
	catch(BurnerException& bme)
	{
		m_Burner.ReleaseDevices();
		ShowErrorMessage(bme);
	}
}
DWORD CBurnerDialog::DirectCopyThread(LPVOID pVoid) 
{
	DirectCopyThreadParam* pParam = (DirectCopyThreadParam*)pVoid;
	CBurnerDialog* pThis = pParam->pThis;
	DirectCopySettings settings = pParam->Settings;

	SetEvent(pThis->m_hCommandThreadStartedEvent);
	// Do not use pParam after this line

	try
	{
		pThis->m_Burner.DirectCopy(settings);
	}
	catch(BurnerException& bme)
	{
		pThis->m_ThreadException = bme;
		return THREAD_EXCEPTION;
	}

	return NOERROR;
}
