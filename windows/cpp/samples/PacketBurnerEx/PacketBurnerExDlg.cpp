// This is a part of the High Performance CD Engine Library.
// Copyright (C) 2001-2003 Primo Software Corporation
// All rights reserved.
//
// This source code is only intended as a supplement to the
// High Performance CD Engine Reference and related
// electronic documentation provided with the library.
// See these sources for detailed information regarding the	
// High Performance CD Engine product.

// PacketBurnerDlg.cpp : implementation file
//

#include "stdafx.h"
#include "PacketBurnerEx.h"
#include "PacketBurnerExDlg.h"
#include "DialogProgress.h"

#include <io.h>

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

#define SMALL_FILES_CACHE_LIMIT	20000 
#define SMALL_FILE_SECTORS		(10) 
#define MAX_SMALL_FILE_SECTORS	(1000)

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

CPacketBurnerExDlg::CPacketBurnerExDlg(CWnd* pParent /*=NULL*/)
	: CDialog(CPacketBurnerExDlg::IDD, pParent)
{
	//{{AFX_DATA_INIT(CPacketBurnerExDlg)
	m_strRootDir = _T("");
	m_strFreeSpace = _T("");
	m_strRequiredSpace = _T("");
	m_bEject = FALSE;
	//}}AFX_DATA_INIT
	// Note that LoadIcon does not require a subsequent DestroyIcon in Win32

	m_ddwRequiredSpace = 0;

	m_ctx.bEject = FALSE;
	m_ctx.bStopRequest = FALSE;

	m_ctx.eAction = ACTION_NONE;
	m_ctx.nSpeedKB = -1;
	
	m_hProcessStartedEvent = CreateEvent(NULL, TRUE, FALSE, NULL);
	m_hNotifyEvent = CreateEvent(NULL, TRUE, FALSE, NULL);
	InitializeCriticalSection(&m_cs);

	m_pDevice = NULL;
	m_notify.nPercent = 0;
	m_notify.nUsedCachePercent = 0;
	m_notify.strText = _T("");

	m_MediaProfile = MediaProfile::Unknown;
}

void CPacketBurnerExDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	
	DDX_Control(pDX, IDC_COMBO_SPEED, m_comboSpeed);
	DDX_Control(pDX, IDC_STATIC_REQUIRED_SPACE, m_staticRequiredSpace);
	DDX_Control(pDX, IDC_STATIC_FREE_SPACE, m_staticFreeSpace);
	DDX_Control(pDX, IDC_EDIT_ROOT, m_editRootDir);

	DDX_Text(pDX, IDC_EDIT_ROOT, m_strRootDir);
	DDX_Text(pDX, IDC_STATIC_FREE_SPACE, m_strFreeSpace);
	DDX_Text(pDX, IDC_STATIC_REQUIRED_SPACE, m_strRequiredSpace);
	DDX_Check(pDX, IDC_CHECK_EJECT, m_bEject);

	DDX_Control(pDX, IDC_CHECK_QUICK, m_chkQuickErase);
	DDX_Control(pDX, IDC_CHECK_QUICK2, m_chkQuickFormat);

	DDX_Control(pDX, IDC_STATIC_FREE_SPACE, m_staticFreeSpace);
	DDX_Control(pDX, IDC_COMBO_SPEED, m_comboSpeed);
	DDX_Control(pDX, IDC_COMBO_DEVICES, m_comboDevices);
	DDX_Control(pDX, IDC_CHECK_EJECT, m_chkEject);

	DDX_Control(pDX, IDC_BUTTON_START, m_btnStart);
	DDX_Control(pDX, IDC_BUTTON_APPEND, m_btnAppend);
	DDX_Control(pDX, IDC_BUTTON_FINALIZE, m_btnFinalize);

	DDX_Control(pDX, IDC_STATIC_MEDIA_TYPE, m_staticMediaType);
}

BEGIN_MESSAGE_MAP(CPacketBurnerExDlg, CDialog)
	//{{AFX_MSG_MAP(CPacketBurnerExDlg)

	ON_WM_SYSCOMMAND()
	ON_BN_CLICKED(IDC_BUTTON_BROWSE, OnButtonBrowse)
	
	ON_BN_CLICKED(IDC_BUTTON_START, OnButtonStart)
	ON_BN_CLICKED(IDC_BUTTON_APPEND, OnButtonAppend)
	ON_BN_CLICKED(IDC_BUTTON_FINALIZE, OnButtonFinalize)

	ON_EN_CHANGE(IDC_EDIT_ROOT, OnChangeEditRoot)
	ON_WM_DESTROY()
	
	ON_MESSAGE( WM_DEVICECHANGE, OnDeviceChange)
	ON_CBN_SELCHANGE(IDC_COMBO_DEVICES, OnCbnSelchangeComboDevices)

	ON_BN_CLICKED(IDC_BUTTON_EJECTIN, OnBnClickedButtonCloseTray)
	ON_BN_CLICKED(IDC_BUTTON_EJECTOUT, OnBnClickedButtonEject)

	ON_BN_CLICKED(IDC_BUTTON_ERASE, OnBnClickedButtonErase)
	ON_BN_CLICKED(IDC_BUTTON_FORMAT, OnBnClickedButtonFormat)
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CPacketBurnerExDlg message handlers

void CPacketBurnerExDlg::SetDeviceControls()
{
	int nDevice = m_comboDevices.GetCurSel();
	if (-1 == nDevice)
		return;

	Device* pDevice = m_pDeviceEnum->createDevice(m_arIndices[nDevice]);
	if (NULL == pDevice) 
	{
		// Auto insert notification
		TRACE(_T("CPacketBurnerExDlg::SetDeviceControls : Unable to get device object\nPossible reason : auto-insert notification enabled\nwhile writing (or any other) operation is in progress.\nThis is just a warning message - your CD is not damaged."));
		return; 
	}

	// Get and display the media profile
	m_MediaProfile = pDevice->mediaProfile();
	m_staticMediaType.SetWindowText(GetProfileDescription(m_MediaProfile, pDevice));

	m_ddwCapacity = pDevice->mediaFreeSpace();
	m_ddwCapacity *= BlockSize::DVD;

	if (0 == m_ddwCapacity || m_ddwCapacity < m_ddwRequiredSpace) 
	{
		m_btnStart.EnableWindow(FALSE);
		m_btnAppend.EnableWindow(FALSE);
	}
	else 
	{
		m_btnStart.EnableWindow();
		m_btnAppend.EnableWindow();
	}

	int nCurSel, nCurSpeedKB, nMaxSpeedKB;
	nCurSel = m_comboSpeed.GetCurSel();
	nCurSpeedKB = (INT32)m_comboSpeed.GetItemData(nCurSel);
	nMaxSpeedKB = pDevice->maxWriteSpeedKB();

	m_comboSpeed.ResetContent();

	SpeedEnum* pSpeeds = pDevice->createWriteSpeedEnumerator();
		for (int i = 0; i < pSpeeds->count(); i++)
		{
			SpeedDescriptor* pSpeed = pSpeeds->at(i);
			
			CString sSpeed;
			int nSpeedKB = pSpeed->transferRateKB();
			
			sSpeed.Format(_T("%1.0fx"), (double)nSpeedKB / (pDevice->isMediaDVD() ? Speed1xKB::DVD : Speed1xKB::CD));
			m_comboSpeed.InsertString(-1, sSpeed);

			m_comboSpeed.SetItemData(i, nSpeedKB);
		}
	pSpeeds->release();
	
	// Restore speed...
	if (nCurSel != -1 && nCurSpeedKB <= nMaxSpeedKB)
	{
		CString sSpeed;
		sSpeed.Format(_T("%dx"), nCurSpeedKB / Speed1xKB::CD);
		m_comboSpeed.SelectString(-1, sSpeed);
	}

	if (-1 == m_comboSpeed.GetCurSel())
		m_comboSpeed.SetCurSel(0);

	CString str;
	str.Format(_T("Required space : %.2fGB"), ((double)(__int64)m_ddwRequiredSpace) / 1e9);
	m_staticRequiredSpace.SetWindowText(str);

	str.Format(_T("Free space : %.2fGB"), ((double)(__int64)m_ddwCapacity) / 1e9);
	m_staticFreeSpace.SetWindowText(str);

	pDevice->release();
}

BOOL CPacketBurnerExDlg::OnInitDialog()
{
	CDialog::OnInitDialog();

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

	// Add extra initialization here
	m_ddwRequiredSpace = 0;
	m_nDeviceCount = 0;

	// Write parameters
	m_chkEject.SetCheck(FALSE);

	// Erase/Format parameters
	m_chkQuickErase.SetCheck(TRUE);
	m_chkQuickFormat.SetCheck(TRUE);

	// 	Create / Initialize engine
	Library::enableTraceLog(NULL, TRUE);
	m_pEngine = Library::createEngine();
	if (!m_pEngine->initialize()) 
	{
		AfxMessageBox(_T("Unable to initialize engine."));
		
		m_pEngine->release();
		m_pEngine = NULL;

		// Close the dialog
		EndModalLoop(-1);
		return FALSE;
	}

	// Get device enumerator
	m_pDeviceEnum = m_pEngine->createDeviceEnumerator();
	int nDevices = m_pDeviceEnum->count();
	if (0 == nDevices) 
	{
		AfxMessageBox(_T("No CD/DVD devices available."));

		m_pDeviceEnum->release();
		m_pDeviceEnum = NULL;

		return FALSE;
	}

	// Count the CD/DVD writers
	m_nDeviceCount = 0;
	for (int i = 0; i < nDevices;i++) 
	{
		// Get next device
		Device* pDevice = m_pDeviceEnum->createDevice(i);

		// Always check for NULL
		if (NULL == pDevice)
			continue;
		
		// Add only writers
		BOOL bWrite = isWritePossible(pDevice);
		if (bWrite) 
		{
			// Create a name
			TCHAR tcsName[256] = {0};
			_stprintf(tcsName, _T("(%c:) - %s"), pDevice->driveLetter(), pDevice->description());

			// Add to the internal arrays
			m_arDeviceNames.Add(tcsName);
			m_arIndices.Add(i);

			// Add to the combo box, user interface
			m_comboDevices.AddString(m_arDeviceNames[m_nDeviceCount++]);
		}

		// Release device object
		pDevice->release();
	}

	// Select the first device
	if (m_nDeviceCount > 0)
	{
		m_comboDevices.SetCurSel(0);
		SetDeviceControls();
	}
	else 
	{
		AfxMessageBox(_T("Could not find any CD/DVD writer devices."));

		m_pDeviceEnum->release();
		m_pDeviceEnum = NULL;

		return FALSE;
	}

	// Do not release the enumerator here, we will need it again
	return TRUE;  // return TRUE  unless you set the focus to a control
}

// Uses FindFile and WIN32_FIND_DATA to get the file list
void CPacketBurnerExDlg::ProcessInputTree(DataFile * pCurrentFile, CString & sCurrentPath)
{
	WIN32_FIND_DATA FindFileData;

	CString sSearchFor = sCurrentPath + _T("\\*") ;
	HANDLE hfind = FindFirstFile (sSearchFor, &FindFileData);

	if (hfind == INVALID_HANDLE_VALUE)
		throw ProcessInputTreeException("File not found.");

	do 
	{
		// Keep the original file name
		CString sFileName = FindFileData.cFileName; 
		
		// Get the full path
		CString sFullPath;
		sFullPath.Format(_T("%s\\%s"), sCurrentPath, sFileName);

		if (FindFileData.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
		{
			// Skip the curent folder . and the parent folder .. 
			if (sFileName == _T(".")  || sFileName == _T(".."))
				continue;

			// Create a folder entry and scan it for the files
			DataFile * pDataFile = Library::createDataFile(); 

			pDataFile->setDirectory(TRUE);
			pDataFile->setPath((LPCTSTR)sFileName);			
			pDataFile->setLongFilename((LPCTSTR)sFileName);

			if(FindFileData.dwFileAttributes & FILE_ATTRIBUTE_HIDDEN)
				pDataFile->setHiddenMask(ImageTypeFlags::Udf);

			pDataFile->setCreationTime(FindFileData.ftCreationTime);
			
			// Search for all files
			ProcessInputTree(pDataFile, sFullPath);

			// Add this folder to the tree
			pCurrentFile->children()->add(pDataFile);

			pDataFile->release();
		} 
		else
		{
			// File
			DataFile* pDataFile = Library::createDataFile();

			pDataFile->setDirectory(FALSE);
			pDataFile->setPath((LPCTSTR)sFullPath);			
			pDataFile->setLongFilename((LPCTSTR)sFileName);

			if(FindFileData.dwFileAttributes & FILE_ATTRIBUTE_HIDDEN)
				pDataFile->setHiddenMask(ImageTypeFlags::Udf);

			pDataFile->setCreationTime(FindFileData.ftCreationTime);

			// Add to the tree
			pCurrentFile->children()->add(pDataFile);

			pDataFile->release();
		}

	} while (FindNextFile (hfind, &FindFileData));

	FindClose(hfind);
}

BOOL CPacketBurnerExDlg::SetEmptyImageLayout(DataDisc* pDataDisc)
{
	DataFile * pDataFile = Library::createDataFile(); 

	// Entry for the root of the image file system
	pDataFile->setDirectory(TRUE);
	pDataFile->setPath(_T("\\"));			
	pDataFile->setLongFilename(_T("\\"));

	BOOL bRes = pDataDisc->setImageLayout(pDataFile);
	pDataFile->release();
	return bRes;
}

BOOL CPacketBurnerExDlg::SetImageLayoutFromFolder(DataDisc* pDataDisc, LPCTSTR fname)
{
	DataFile * pDataFile = Library::createDataFile(); 

	// Entry for the root of the image file system
	pDataFile->setDirectory(TRUE);
	pDataFile->setPath(_T("\\"));			
	pDataFile->setLongFilename(_T("\\"));

	try
	{
		CString sFullPath = fname;
		ProcessInputTree(pDataFile, sFullPath);
	}
	catch(ProcessInputTreeException &ex)
	{
		TRACE(_T("SetImageLayoutFromFolder Error: %s") , ex.what());
		
		pDataFile->release();
		return FALSE;
	}

	BOOL bRes = pDataDisc->setImageLayout(pDataFile);
	pDataFile->release();
	return bRes;
}

void CPacketBurnerExDlg::OnSysCommand(UINT nID, LPARAM lParam)
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
void CPacketBurnerExDlg::OnButtonBrowse() 
{
	TCHAR szPath[MAX_PATH];
	memset(szPath, 0, sizeof(szPath));

	BROWSEINFO bi = {0};
	bi.hwndOwner = m_hWnd;
	bi.pidlRoot = NULL;
	bi.lpszTitle = _T("Select a destination folder");
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

	m_strRootDir=szPath;
	m_editRootDir.SetWindowText(m_strRootDir);

	DataDisc* pDataDisc = Library::createDataDisc();
	pDataDisc->setImageType(ImageTypeFlags::Udf);

	CWaitCursor wc;
    BOOL bRes = SetImageLayoutFromFolder(pDataDisc, m_strRootDir);
	if (!bRes) 
	{
		CString str;
		str.Format(_T("A problem occured when trying to set directory:\n%s"), m_strRootDir);
		AfxMessageBox(str);

		m_strRootDir = _T("");
	} 
	else 
	{
		m_ddwRequiredSpace = pDataDisc->imageSizeInBytes();
	}

	SetDeviceControls();
	pDataDisc->release();
}

BOOL CPacketBurnerExDlg::DirectoryExists()
{
	TCHAR tcs[1024];
	m_editRootDir.GetWindowText(tcs, 1024);

	if (_taccess(tcs, 0)!=-1)
		return TRUE;
	else
		return FALSE;
}

void CPacketBurnerExDlg::OnChangeEditRoot() 
{
	m_editRootDir.Invalidate(FALSE);
}

BOOL CPacketBurnerExDlg::ValidateForm() 
{
	if (!DirectoryExists()) 
	{
		AfxMessageBox(_T("Please specify a valid directory."));
		m_editRootDir.SetFocus();
		return FALSE;
	}

	if (_T('\\') == m_strRootDir[m_strRootDir.GetLength()-1] || 
		_T('/') == m_strRootDir[m_strRootDir.GetLength()-1]) 
	{
		m_strRootDir = m_strRootDir.Left(m_strRootDir.GetLength()-1);
	}

	return TRUE;
}

void CPacketBurnerExDlg::StartProcess(EAction eAction) 
{
	EnableWindow(FALSE);

	CDialogProgress dlg;
	dlg.Create();
	dlg.ShowWindow(SW_SHOW);
	dlg.UpdateWindow();

	m_ctx.eAction = eAction;

	m_ctx.bStopRequest = FALSE;
	m_ctx.bEject = m_bEject;

	int nSel = m_comboSpeed.GetCurSel();
	m_ctx.nSpeedKB = (int)m_comboSpeed.GetItemData(nSel);

	ResetEvent(m_hProcessStartedEvent);
	ResetEvent(m_hNotifyEvent);

	// Create thread to execute the current operation
	DWORD dwId;
	m_hThread = CreateThread(NULL, NULL, ProcessThreadProc, this, NULL, &dwId);
	WaitForSingleObject(m_hProcessStartedEvent, INFINITE);

	while (WAIT_TIMEOUT == WaitForSingleObject(m_hThread, 50)) 
	{
		___PumpMessages();

		if (dlg.m_bStopped)
			m_ctx.bStopRequest=TRUE;

		if (WAIT_OBJECT_0 == WaitForSingleObject(m_hNotifyEvent, 0)) 
		{
			ResetEvent(m_hNotifyEvent);
			EnterCriticalSection(&m_cs);

				dlg.SetStatus(m_notify.strText);
				dlg.SetProgress(m_notify.nPercent);
				dlg.SetInternalBuffer(m_notify.nUsedCachePercent);

			LeaveCriticalSection(&m_cs);
		}
	}

	dlg.DestroyWindow();
	
	EnableWindow();
	BringWindowToTop();
	SetActiveWindow();

	m_notify.nPercent = 0;
	m_notify.nUsedCachePercent = 0;
	m_notify.strText = _T("");

	SetDeviceControls();
}

// Process thread...
DWORD CPacketBurnerExDlg::ProcessThreadProc(LPVOID pParam) 
{
	CPacketBurnerExDlg* pThis = (CPacketBurnerExDlg* )pParam;
	return pThis->ProcessThread();

}

//  Thread main function
DWORD CPacketBurnerExDlg::ProcessThread() 
{
	SetEvent(m_hProcessStartedEvent);

	// Get a device 
	int nDevice = m_comboDevices.GetCurSel();

	// No devices and not an OPERATION_IMAGE_CREATE - nothing to do here
	if (-1 == nDevice)
		return -1;

	// Get device object
	m_pDevice = m_pDeviceEnum->createDevice(m_arIndices[nDevice]);
	if (NULL == m_pDevice)
		return -2;

	// Burn, Append, Finalize Disc
	switch (m_ctx.eAction) 
	{
		case ACTION_BURN_START:
			Burn(m_pDevice, FALSE, FALSE);
		break;
		case ACTION_BURN_APPEND:
			Burn(m_pDevice, FALSE, TRUE);
		break;
		case ACTION_BURN_FINALIZE:
			Burn(m_pDevice, TRUE, TRUE);
		break;
		case ACTION_ERASE:
			Erase(m_pDevice);
		break;
		case ACTION_FORMAT:
			Format(m_pDevice);
		break;
	}

	if (NULL != m_pDevice)
	{
		// Call Device::Dismount to dismount the system volume. This works only on Windows NT, 2000 and XP.
		m_pDevice->dismount();

		m_pDevice->release();
		m_pDevice = NULL;
	}

	return 0;
}

void CPacketBurnerExDlg::OnButtonStart() 
{
	if (!UpdateData())
		return;

	if (!ValidateForm())
		return;

	StartProcess(ACTION_BURN_START);
}

void CPacketBurnerExDlg::OnButtonAppend() 
{
	if (!UpdateData())
		return;

	if (!ValidateForm())
		return;

	StartProcess(ACTION_BURN_APPEND);
}

void CPacketBurnerExDlg::OnButtonFinalize() 
{
	if (!UpdateData())
		return;

	StartProcess(ACTION_BURN_FINALIZE);
}

LRESULT CPacketBurnerExDlg::OnDeviceChange(WPARAM wParam, LPARAM lParam) 
{
	SetDeviceControls();
	return 0;
}

void CPacketBurnerExDlg::OnDestroy() 
{
	CDialog::OnDestroy();

	if (m_pDeviceEnum)
		m_pDeviceEnum->release();

	if (m_pEngine)
	{
		m_pEngine->shutdown();
		m_pEngine->release();
	}

	Library::disableTraceLog();
}

BOOL CPacketBurnerExDlg::Erase(Device * pDevice) 
{
	if (NULL == pDevice)
		return FALSE;

	MediaProfile::Enum mp = pDevice->mediaProfile();
	if (MediaProfile::DVDMinusRWSeq != mp && MediaProfile::DVDMinusRWRO != mp && MediaProfile::CDRW != mp)
	{
		AfxMessageBox(_T("Erasing is supported on CD-RW and DVD-RW media only."), MB_OK);
		return FALSE;
	}

	int nRes = AfxMessageBox(_T("Erasing will destroy all the information on the disc. Do you want to continue?"), MB_OKCANCEL);
	if (IDCANCEL == nRes)
		return TRUE;

	/*
	// ???
	if (MP_DVD_MINUS_RW_RO == mp && m_ctx.bQuick)
	{
		nRes = AfxMessageBox(_T("The media is DVD-RW in restricted overwrite mode and quick erase ")
									_T("cannot be performed. Do you want to perform a full erase instead?"), MB_OKCANCEL);

		if (IDCANCEL == nRes)
			return TRUE;

		m_ctx.bQuick = FALSE;
	}
	*/
	
	pDevice->setCallback(this);
	return pDevice->erase(m_ctx.bQuick ? EraseType::Minimal : EraseType::Disc);
}

BOOL CPacketBurnerExDlg::Format(Device * pDevice) 
{
	if (NULL == pDevice)
		return FALSE;

	MediaProfile::Enum mp = pDevice->mediaProfile();
	if (MediaProfile::DVDMinusRWSeq != mp && MediaProfile::DVDMinusRWRO != mp && MediaProfile::DVDPlusRW != mp)
	{
		AfxMessageBox(_T("Format is supported on DVD-RW and DVD+RW media only."), MB_OK);
		return FALSE;
	}

	int nRes = AfxMessageBox(_T("Format will destroy all the information on the disc. Do you want to continue?"), MB_OKCANCEL);
	if (IDCANCEL == nRes)
		return TRUE;

	pDevice->setCallback(this);

	BOOL bRes = TRUE;
	switch(mp)
	{
		case MediaProfile::DVDMinusRWRO:
			bRes = pDevice->format(m_ctx.bQuick ? FormatType::DVDMinusRWQuick : FormatType::DVDMinusRWFull);
		break;
		case MediaProfile::DVDMinusRWSeq:
			bRes = pDevice->format(m_ctx.bQuick ? FormatType::DVDMinusRWQuick : FormatType::DVDMinusRWFull);
		break;

		case MediaProfile::DVDPlusRW:
		{
			BgFormatStatus::Enum fmt = pDevice->bgFormatStatus();
			switch(fmt)
			{
			case BgFormatStatus::NotFormatted:
				bRes = pDevice->format(FormatType::DVDPlusRWFull, 0, !m_ctx.bQuick);
				break;
			case BgFormatStatus::Partial:
				bRes = pDevice->format(FormatType::DVDPlusRWRestart, 0, !m_ctx.bQuick);
				break;
			}
		}
		break;
	}

	if (!bRes)
		ShowErrorMessage(pDevice->error());

	return bRes;
}

// IDataDiscCallback14
void CPacketBurnerExDlg::onProgress(int64_t bytesWritten, int64_t all) 
{
	EnterCriticalSection(&m_cs);
	{
		double dd = (double)(__int64)bytesWritten * 100.0 / (double)(__int64)all;
		m_notify.nPercent=(int)dd;

		if (m_pDevice)
		{
			// get device internal buffer
			dd = (double)m_pDevice->internalCacheUsedSpace();
			dd = dd * 100.0 / (double)m_pDevice->internalCacheCapacity();
			m_notify.nUsedCachePercent = (int)dd;
		}
		else
		{
			m_notify.nUsedCachePercent = 0;
		}
	}
	LeaveCriticalSection(&m_cs);

	SetEvent(m_hNotifyEvent);
}


void CPacketBurnerExDlg::onStatus(DataDiscStatus::Enum eStatus) 
{
	EnterCriticalSection(&m_cs);
	{
		m_notify.strText = GetTextStatus(eStatus);
	}
	LeaveCriticalSection(&m_cs);

	SetEvent(m_hNotifyEvent);
}

void CPacketBurnerExDlg::onFileStatus(int32_t fileNumber, const char_t* filename, int32_t percentWritten)
{
	// ATLTRACE(_T("%d\n"), nPercent);
}

BOOL CPacketBurnerExDlg::onContinueWrite() 
{
	if (m_ctx.bStopRequest)
		return FALSE;

	return TRUE;
}

// IDeviceCallback13
void CPacketBurnerExDlg::onFormatProgress(double fPercentCompleted)
{
	EnterCriticalSection(&m_cs);
	m_notify.strText = _T("Formatting...");
	m_notify.nPercent = (int)fPercentCompleted;
	LeaveCriticalSection(&m_cs);

	SetEvent(m_hNotifyEvent);
}

void CPacketBurnerExDlg::onEraseProgress(double fPercentCompleted)
{
	EnterCriticalSection(&m_cs);
	m_notify.strText = _T("Erasing...");
	m_notify.nPercent = (int)fPercentCompleted;
	LeaveCriticalSection(&m_cs);

	SetEvent(m_hNotifyEvent);
}

CString CPacketBurnerExDlg::GetTextStatus(DataDiscStatus::Enum eStatus)
{
	switch(eStatus)
	{
		case DataDiscStatus::BuildingFileSystem:
			return CString(_TEXT("Building filesystem..."));
		
		case DataDiscStatus::WritingFileSystem:
			return CString(_TEXT("Writing filesystem..."));

		case DataDiscStatus::WritingImage:
			return CString(_TEXT("Writing image..."));

		case DataDiscStatus::CachingSmallFiles:
			return CString(_TEXT("Caching small files ..."));

		case DataDiscStatus::CachingNetworkFiles:
			return CString(_TEXT("Caching network files ..."));

		case DataDiscStatus::CachingCDRomFiles:
			return CString(_TEXT("Caching CD-ROM files ..."));

		case DataDiscStatus::Initializing:
			return CString(_TEXT("Initializing..."));

		case DataDiscStatus::Writing:
			return CString(_TEXT("Writing..."));

		case DataDiscStatus::WritingLeadOut:
			return CString(_TEXT("Flushing device cache and writing lead-out..."));

		case DataDiscStatus::LoadingImageLayout:
			return CString(_TEXT("Loading image layout from last track..."));
	}

	return CString(_TEXT("Unknown status..."));
}

bool CPacketBurnerExDlg::isWritePossible(Device *device) const
{
	CDFeatures *cdfeatures = device->cdFeatures();
	DVDFeatures *dvdfeatures = device->dvdFeatures();
	BDFeatures *bdfeatures = device->bdFeatures();

	bool cdWritePossible = cdfeatures->canWriteCDR() || cdfeatures->canWriteCDRW();
	bool dvdWritePossible = dvdfeatures->canWriteDVDMinusR() || dvdfeatures->canWriteDVDMinusRDL() ||
							dvdfeatures->canWriteDVDPlusR() || dvdfeatures->canWriteDVDPlusRDL() ||
							dvdfeatures->canWriteDVDMinusRW() || dvdfeatures->canWriteDVDPlusRW() ||
							dvdfeatures->canWriteDVDRam();
	bool bdWritePossible = bdfeatures->canWriteBDR() || bdfeatures->canWriteBDRE();
	return cdWritePossible || dvdWritePossible || bdWritePossible;
}

void CPacketBurnerExDlg::SetParameters(DataDisc* pDataDisc, BOOL bCloseSessionAndDisc)
{
	pDataDisc->setImageType(ImageTypeFlags::Udf);

	pDataDisc->udfVolumeProps()->setVolumeLabel(_T("PRIMOBURNERDISC"));
	pDataDisc->setCallback(this);

	pDataDisc->setSimulateBurn(FALSE);

	// Packet mode
	pDataDisc->setWriteMethod(WriteMethod::Packet);

	// Reserve the path table
	DataWriteStrategy::Enum writeStrategy = DataWriteStrategy::None;

	switch(m_ctx.eAction)
	{
	case ACTION_BURN_START:
		writeStrategy = DataWriteStrategy::ReserveFileTableTrack;
		break;
	case ACTION_BURN_FINALIZE:
		writeStrategy = DataWriteStrategy::WriteFileTableTrack;
		break;
	}

	pDataDisc->setWriteStrategy(writeStrategy);

	pDataDisc->setCloseTrack(DataWriteStrategy::WriteFileTableTrack == writeStrategy);
	pDataDisc->setCloseSession(bCloseSessionAndDisc);
	pDataDisc->setCloseDisc(bCloseSessionAndDisc);
}

int CPacketBurnerExDlg::GetLastTrack(Device * pDevice)
{
	// Get the last track number from the last session if multisession option was specified
	int nLastTrack = 0;

	// Check for DVD+RW and DVD-RW RO random writable media. 
	MediaProfile::Enum mp = pDevice->mediaProfile();

	if ((MediaProfile::DVDPlusRW == mp) || (MediaProfile::DVDMinusRWRO == mp) || 
		(MediaProfile::BDRE == mp)  || (MediaProfile::BDRSrmPow == mp) || (MediaProfile::DVDRam == mp))
	{
		// DVD+RW and DVD-RW RO has only one session with one track
		if (pDevice->mediaFreeSpace() > 0)
			nLastTrack = 1;	
	}		
	else
	{
		// All other media is recorded using tracks and sessions and multi-session discs are similar to CD. 
		// Use the ReadDiskInfo method to get the last track number
		DiscInfo *pDI = pDevice->readDiscInfo();
		if(NULL != pDI)
		{
			nLastTrack = pDI->lastTrack();
			pDI->release();
		}
	}

	return nLastTrack;
}

BOOL CPacketBurnerExDlg::Burn(Device* pDevice, BOOL fFinalize, BOOL fLoadLastTrack)
{
	if (!pDevice)
		return FALSE;

	pDevice->setWriteSpeedKB(m_ctx.nSpeedKB);

	// Create DataDisc object 
	DataDisc* pDataDisc = Library::createDataDisc();
	pDataDisc->setDevice(pDevice);

	// Set burning parameters
	SetParameters(pDataDisc, fFinalize);

	// Get disk information
	{
		DiscInfo *pDI = pDevice->readDiscInfo();
		if(NULL == pDI)
		{
			pDataDisc->release();
			return FALSE;
		}

		// Set the session start address. Must do this before intializing the directory structure.
		if (SessionState::Open == pDI->sessionState())
			pDataDisc->setSessionStartAddress(pDevice->newTrackStartAddress());
		else
			pDataDisc->setSessionStartAddress(pDevice->newSessionStartAddress());

		pDI->release();
	}

	// Get the last track number from the last session if multi-session option was specified
	int nPrevTrackNumber = fLoadLastTrack ? GetLastTrack(pDevice) : 0;
	pDataDisc->setLayoutLoadTrack(nPrevTrackNumber);

	// Set image layout
	BOOL bRes = SetImageLayoutFromFolder(pDataDisc, m_strRootDir);
	if (!bRes) 
	{
		ShowErrorMessage(pDataDisc->error());

		pDataDisc->release();
		return FALSE;
	}

	// Burn 
	while (true)
	{
		// Try to write the image
		bRes = pDataDisc->writeToDisc();
		if (!bRes)
		{
			// Check if the error is: Cannot load image layout. 
			// If so most likely it is an empty formatted DVD+RW or empty formatted DVD-RW RO with one track. 
			if((pDataDisc->error()->facility() == ErrorFacility::DataDisc) &&
				(pDataDisc->error()->code() == DataDiscError::CannotLoadImageLayout))
			{
				// Set to 0 to disable loading filesystem from previous track
				pDataDisc->setLayoutLoadTrack(0);

				// retry writing
				continue;
			}
		}

		break;
	}

	// Check for errors
	if (!bRes) 
	{
		ShowErrorMessage(pDataDisc->error());
		pDataDisc->release();
		return FALSE;
	}

	if (m_ctx.bEject)
		pDevice->eject(m_ctx.bEject);

	pDataDisc->release();
	return TRUE;
}

void CPacketBurnerExDlg::OnCbnSelchangeComboDevices()
{
	int nCurSel = m_comboDevices.GetCurSel();
	if (-1 != nCurSel)
		SetDeviceControls();
}

void CPacketBurnerExDlg::OnBnClickedButtonCloseTray()
{
	int nDevice = m_comboDevices.GetCurSel();

	Device* pDevice = m_pDeviceEnum->createDevice(m_arIndices[nDevice]);
	if(pDevice)
	{
		pDevice->eject(0);
		pDevice->release();
	}
}

void CPacketBurnerExDlg::OnBnClickedButtonEject()
{
	int nDevice = m_comboDevices.GetCurSel();
	Device* pDevice = m_pDeviceEnum->createDevice(m_arIndices[nDevice]);
	if (pDevice)
	{	
		pDevice->eject(1);
		pDevice->release();
	}
}

void CPacketBurnerExDlg::OnBnClickedButtonErase()
{
	EnableWindow(FALSE);

	CDialogProgress dlg;
	dlg.Create();
	dlg.ShowWindow(SW_SHOW);
	dlg.UpdateWindow();
	dlg.GetDlgItem(IDOK)->EnableWindow(FALSE);

	dlg.SetStatus(_T("Erasing disc. Please wait..."));

	ResetEvent(m_hProcessStartedEvent);

	m_ctx.eAction = ACTION_ERASE;
	m_ctx.bQuick = m_chkQuickErase.GetCheck();

	DWORD dwId;
	m_hThread = CreateThread(NULL, NULL, ProcessThreadProc, this, NULL, &dwId);
	WaitForSingleObject(m_hProcessStartedEvent, INFINITE);

	while (WAIT_TIMEOUT == WaitForSingleObject(m_hThread, 0)) 
	{
		___PumpMessages();

		if (WAIT_OBJECT_0 == WaitForSingleObject(m_hNotifyEvent, 0)) 
		{
			ResetEvent(m_hNotifyEvent);
			EnterCriticalSection(&m_cs);
			dlg.SetProgress(m_notify.nPercent);
			LeaveCriticalSection(&m_cs);
		}

		Sleep(50);
	}

	dlg.DestroyWindow();
	
	EnableWindow();
	BringWindowToTop();
	SetActiveWindow();

	SetDeviceControls();
}

void CPacketBurnerExDlg::OnBnClickedButtonFormat()
{
	EnableWindow(FALSE);

	CDialogProgress dlg;
	dlg.Create();
	dlg.ShowWindow(SW_SHOW);
	dlg.UpdateWindow();
	dlg.GetDlgItem(IDOK)->EnableWindow(FALSE);

	dlg.SetStatus(_T("Formatting disc. Please wait..."));

	ResetEvent(m_hProcessStartedEvent);

	m_ctx.eAction = ACTION_FORMAT;
	m_ctx.bQuick = (1 == m_chkQuickFormat.GetCheck());

	DWORD dwId;
	m_hThread = CreateThread(NULL, NULL, ProcessThreadProc, this, NULL, &dwId);
	WaitForSingleObject(m_hProcessStartedEvent, INFINITE);

	while (WAIT_TIMEOUT == WaitForSingleObject(m_hThread, 0)) 
	{
		___PumpMessages();

		if (WAIT_OBJECT_0 == WaitForSingleObject(m_hNotifyEvent, 0)) 
		{
			ResetEvent(m_hNotifyEvent);
			EnterCriticalSection(&m_cs);
			dlg.SetProgress(m_notify.nPercent);
			LeaveCriticalSection(&m_cs);
		}

		Sleep(50);
	}

	dlg.DestroyWindow();
	
	EnableWindow();
	BringWindowToTop();
	SetActiveWindow();

	SetDeviceControls();
}


// static
LPCTSTR CPacketBurnerExDlg::GetProfileDescription(WORD wProfile, Device * pDevice)
{
	switch(wProfile)
	{
		case MediaProfile::CDRom:
			return _TEXT("CD-ROM. Read only CD."); 

		case MediaProfile::CDR:
			return _TEXT("CD-R. Write once CD."); 

		case MediaProfile::CDRW:
			return _TEXT("CD-RW. Re-writable CD.");

		case MediaProfile::DVDRom:
			return _TEXT("DVD-ROM. Read only DVD.");

		case MediaProfile::DVDMinusRSeq:
			return _TEXT("DVD-R for Sequential Recording.");

		case MediaProfile::DVDMinusRDLSeq:	
			return _TEXT("DVD-R DL Double Layer for Sequential Recording.");

		case MediaProfile::DVDMinusRDLJump:
			return _TEXT("DVD-R DL Double Layer for Layer Jump Recording.");

		case MediaProfile::DVDRam:
			return _TEXT("DVD-RAM ReWritable DVD.");

		case MediaProfile::DVDMinusRWRO:
			return _TEXT("DVD-RW for Restricted Overwrite.");

		case MediaProfile::DVDMinusRWSeq:
			return _TEXT("DVD-RW for Sequential Recording.");

		case MediaProfile::DVDPlusRW:
		{
			BgFormatStatus::Enum fmt = pDevice->bgFormatStatus();
			switch(fmt)
			{
				case BgFormatStatus::NotFormatted:
					return _TEXT("DVD+RW. Not formatted.");
				break;
				case BgFormatStatus::Partial:
					return _TEXT("DVD+RW. Partially formatted.");
				break;
				case BgFormatStatus::Pending:
					return _TEXT("DVD+RW. Background format is pending ...");
				break;
				case BgFormatStatus::Completed:
					return _TEXT("DVD+RW. Formatted.");
				break;
			}

			return _TEXT("DVD+RW for Random Recording.");
		}

		case MediaProfile::DVDPlusR:
			return _TEXT("DVD+R for Sequential Recording.");
		
		case MediaProfile::DVDPlusRDL:
			return _TEXT("DVD+R DL Double Layer for Sequential Recording.");

		default:
			return _TEXT("Unknown Profile.");
	}
}

// Helpers
void CPacketBurnerExDlg::ShowErrorMessage(const ErrorInfo *pErrInfo)
{
	CString strMessage;

	switch(pErrInfo->facility())
	{
	case ErrorFacility::SystemWindows:
		{
			
			TCHAR tcsErrorMessage[1024];
			::FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM, NULL, pErrInfo->code(),
	 				MAKELANGID(LANG_NEUTRAL, SUBLANG_DEFAULT), tcsErrorMessage, 1024, NULL);
			strMessage.Format(_T("System Error: 0x%06x - %s\n"), pErrInfo->code(), tcsErrorMessage);
		}
		break;

	case ErrorFacility::Device:
		strMessage.Format(_T("Device Error: 0x%06x - %s \n"), pErrInfo->code(), pErrInfo->message());
		break;

	case ErrorFacility::DeviceEnumerator:
		strMessage.Format(_T("DeviceEnumerator Error: 0x%06x - %s \n"), pErrInfo->code(), pErrInfo->message());
		break;

	case ErrorFacility::DataDisc:
		strMessage.Format(_T("DataDisc Error: 0x%06x - %s \n"), pErrInfo->code(), pErrInfo->message());
		break;

	default:
		strMessage.Format(_T("Error Facility: 0x%06x   Code: 0x%06x - %s \n"), pErrInfo->facility(), pErrInfo->code(), pErrInfo->message());
		break;
	}

	AfxMessageBox(strMessage);
}


void CPacketBurnerExDlg::___PumpMessages() 
{
	MSG msg;
	while (PeekMessage(&msg, 0, 0, 0, PM_REMOVE))
		DispatchMessage(&msg);
}
