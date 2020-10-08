// This is a part of the High Performance CD Engine Library.
// Copyright (C) 2001-2003 Primo Software Corporation
// All rights reserved.
//
// This source code is only intended as a supplement to the
// High Performance CD Engine Reference and related
// electronic documentation provided with the library.
// See these sources for detailed information regarding the	
// High Performance CD Engine product.

// DataBurnerDlg.cpp : implementation file
//

#include "stdafx.h"
#include "DataBurner.h"
#include "DataBurnerDlg.h"
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
// CDataBurnerDlg dialog

CDataBurnerDlg::CDataBurnerDlg(CWnd* pParent /*=NULL*/)
	: CDialog(CDataBurnerDlg::IDD, pParent)
	, m_bBootableCD(FALSE)
{
	//{{AFX_DATA_INIT(CDataBurnerDlg)
	m_bTest = FALSE;
	m_strRootDir = _T("");
	m_strVolume = _T("");
	m_strFreeSpace = _T("");
	m_strRequiredSpace = _T("");
	m_bEject = FALSE;
	m_bCacheSmallFiles = FALSE;
	m_dwSmallFile = 0;
	m_dwSmallFilesCache = 0;
	//}}AFX_DATA_INIT
	// Note that LoadIcon does not require a subsequent DestroyIcon in Win32

	m_hIcon = AfxGetApp()->LoadIcon(IDR_DATA_ICON);
	m_nRequiredSpace=0;
	m_strVolume="DATADISC";

	m_ctx.bEject=FALSE;
	m_ctx.bStopRequest=FALSE;
	m_ctx.bSimulate=FALSE;

	m_ctx.nMode = 0;
	m_ctx.bRaw = FALSE;
	m_ctx.bCloseDisc = TRUE;

	m_ctx.nOperation=OPERATION_INVALID;
	m_ctx.nSpeedKB = -1;
	m_ctx.strImageFile = "";
	m_ctx.bCacheSmallFiles = TRUE;
	m_ctx.dwSmallFilesCacheLimit = SMALL_FILES_CACHE_LIMIT;
	m_ctx.dwSmallFileSize = SMALL_FILE_SECTORS;
	
	m_hOperationStartedEvent = CreateEvent(NULL, TRUE, FALSE, NULL);
	m_hNotifyEvent=CreateEvent(NULL, TRUE, FALSE, NULL);
	InitializeCriticalSection(&m_cs);

	m_pDevice = NULL;
	m_notify.nPercent=0;
	m_notify.nUsedCachePercent = 0;
	m_notify.nActualWriteSpeed = 0;
	m_notify.strText="";

	m_bCacheSmallFiles=TRUE;
	m_dwSmallFile=SMALL_FILE_SECTORS;
	m_dwSmallFilesCache=SMALL_FILES_CACHE_LIMIT;
}

void CDataBurnerDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	DDX_Control(pDX, IDC_CHECK_CACHE_SMALL_FILES, m_chkCacheSmallFiles);
	DDX_Control(pDX, IDC_COMBO_SPEED, m_comboSpeed);
	DDX_Control(pDX, IDC_STATIC_REQUIRED_SPACE, m_staticRequiredSpace);
	DDX_Control(pDX, IDC_STATIC_FREE_SPACE, m_staticFreeSpace);
	DDX_Control(pDX, IDC_EDIT_VOLUME, m_editVolume);
	DDX_Control(pDX, IDC_BUTTON_CREATE, m_btnCreate);
	DDX_Control(pDX, IDC_EDIT_ROOT, m_editRootDir);
	DDX_Check(pDX, IDC_CHECK_TEST, m_bTest);
	DDX_Text(pDX, IDC_EDIT_ROOT, m_strRootDir);
	DDX_Text(pDX, IDC_EDIT_VOLUME, m_strVolume);
	DDX_Text(pDX, IDC_STATIC_FREE_SPACE, m_strFreeSpace);
	DDX_Text(pDX, IDC_STATIC_REQUIRED_SPACE, m_strRequiredSpace);
	DDX_Check(pDX, IDC_CHECK_EJECT, m_bEject);
	DDX_Check(pDX, IDC_CHECK_CACHE_SMALL_FILES, m_bCacheSmallFiles);
	DDX_Text(pDX, IDC_EDIT_SMALL_FILE, m_dwSmallFile);
	DDV_MinMaxDWord(pDX, m_dwSmallFile, 1, 1000);
	DDX_Text(pDX, IDC_EDIT_SMALL_FILES_CACHE, m_dwSmallFilesCache);
	DDX_Control(pDX, IDC_BUTTON_ERASE, m_btnErase);
	DDX_Control(pDX, IDC_CHECK_QUICK, m_chkQuick);
	DDX_Control(pDX, IDC_STATIC_FREE_SPACE, m_staticFreeSpace);
	DDX_Control(pDX, IDC_COMBO_SPEED, m_comboSpeed);
	DDX_Control(pDX, IDC_COMBO_DEVICES, m_comboDevices);
	DDX_Control(pDX, IDC_CHECK_TEST, m_chkTest);
	DDX_Control(pDX, IDC_CHECK_EJECT, m_chkEject);
	DDX_Control(pDX, IDC_COMBO_MODE, m_comboMode);
	DDX_Control(pDX, IDC_CHECK_CLOSE_DISK, m_chkCloseDisc);
	DDX_Control(pDX, IDC_CHECK_RAW, m_chkRaw);
	DDX_Control(pDX, IDC_COMBO_IMAGE_TYPE, m_comboImageType);
	DDX_Control(pDX, IDC_RADIO_JOLIET_ALL, m_radioJolietAll);
	DDX_Control(pDX, IDC_RADIO_ISO_ALL, m_radioIsoAll);
	DDX_Control(pDX, IDC_CHECK_TREE_DEPTH, m_checkIsoTreeDepth);
	DDX_Control(pDX, IDC_TRANSLATE_NAMES, m_checkIsoTranslateNames);
	DDX_Control(pDX, IDC_STATIC_ISO, m_groupIsoAll);
	DDX_Control(pDX, IDC_STATIC_JOLIET, m_groupJolietAll);
	DDX_Control(pDX, IDC_CHECK_LOAD_LAST_TRACK, m_chkLoadLastTrack);
	DDX_Control(pDX, IDC_COMBO_BOOT_EMULATION, m_comboBootEmulationType);
	DDX_Control(pDX, IDC_BUTTON_BROWSE_BOOT_IMAGE, m_buttonBrowseBootImage);
	DDX_Control(pDX, IDC_EDIT_BOOT_IMAGE, m_editBootImage);
	DDX_Check(pDX, IDC_CHECK_BOOT_IMAGE, m_bBootableCD);
	DDX_Control(pDX, IDC_CHECK_BOOT_IMAGE, m_checkBootImage);
	DDX_Control(pDX, IDC_CHECK_CD_ROM_XA, m_chkCdRomXa);
}

BEGIN_MESSAGE_MAP(CDataBurnerDlg, CDialog)
	//{{AFX_MSG_MAP(CDataBurnerDlg)
	ON_WM_SYSCOMMAND()
	ON_WM_PAINT()
	ON_WM_QUERYDRAGICON()
	ON_BN_CLICKED(IDC_BUTTON_BROWSE, OnButtonBrowse)
	ON_WM_CTLCOLOR()
	ON_EN_CHANGE(IDC_EDIT_ROOT, OnChangeEditRoot)
	ON_BN_CLICKED(IDC_BUTTON_CREATE, OnButtonCreate)
	ON_WM_DESTROY()
	ON_EN_CHANGE(IDC_EDIT_VOLUME, OnChangeEditVolume)
	ON_BN_CLICKED(IDC_BUTTON_BURN_IMAGE, OnButtonBurnImage)
	ON_BN_CLICKED(IDC_BUTTON_CREATE_IMAGE, OnButtonCreateImage)
	ON_BN_CLICKED(IDC_CHECK_CACHE_SMALL_FILES, OnCheckCacheSmallFiles)
	ON_MESSAGE( WM_DEVICECHANGE, OnDeviceChange)
	ON_CBN_SELCHANGE(IDC_COMBO_DEVICES, OnCbnSelchangeComboDevices)
	ON_CBN_SELCHANGE(IDC_COMBO_MODE, OnCbnSelchangeComboMode)
	ON_BN_CLICKED(IDC_BUTTON_EJECTIN, OnBnClickedButtonEjectin)
	ON_BN_CLICKED(IDC_BUTTON_EJECTOUT, OnBnClickedButtonEjectout)
	ON_BN_CLICKED(IDC_BUTTON_ERASE, OnBnClickedButtonErase)
	ON_BN_CLICKED(IDC_RADIO_ISO_LEVEL1, OnBnClickedRadioIsoLevel1)
	ON_BN_CLICKED(IDC_RADIO_ISO_LEVEL2, OnBnClickedRadioIsoLevel2)
	ON_BN_CLICKED(IDC_RADIO_ISO_ALL, OnBnClickedRadioIsoAll)
	ON_CBN_SELCHANGE(IDC_COMBO_IMAGE_TYPE, OnCbnSelchangeComboImageType)
	ON_BN_CLICKED(IDC_BUTTON_BROWSE_BOOT_IMAGE, OnBnClickedButtonBrowseBootImage)
	ON_BN_CLICKED(IDC_CHECK_BOOT_IMAGE, OnBnClickedCheckBootImage)
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CDataBurnerDlg message handlers

void CDataBurnerDlg::SetDeviceControls() 
{
	int nDevice = m_comboDevices.GetCurSel();
	if (-1 == nDevice)
		return;

	Device* pDevice= (Device*)m_pEnum->createDevice(m_arIndices[nDevice]);
	if (pDevice == NULL) 
	{
		TRACE(_T("Function CDataBurnerDlg::SetDeviceControls : Unable to get device object\nPossible reason : auto-insert notification enabled\nwhile writing (or any other) operation is in progress.\nThis is just a warning message - your CD is not damaged."));
		return; // Protection from AI notification...
	}

	m_nCapacity = pDevice->mediaFreeSpace();
	m_nCapacity *= BlockSize::CDRom;

	if (0 == m_nCapacity || m_nCapacity < m_nRequiredSpace) 
		m_btnCreate.EnableWindow(FALSE);
	else 
		m_btnCreate.EnableWindow();

	// Speeds
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

	BOOL bRW, bRWDisk;
	bRW		= isRewritePossible(pDevice);
	bRWDisk = pDevice->isMediaRewritable();

	if (bRW && bRWDisk) 
	{
		m_chkQuick.EnableWindow();
		m_btnErase.EnableWindow();
	} 
	else 
	{
		m_chkQuick.EnableWindow(FALSE);
		m_btnErase.EnableWindow(FALSE);
	}

	m_bRawDao = pDevice->cdFeatures()->canWriteRawDao();

	OnCbnSelchangeComboMode();

	CString str;
	str.Format(_T("Required space : %dMB"), m_nRequiredSpace / (1024*1024));
	m_staticRequiredSpace.SetWindowText(str);

	str.Format(_T("Free space : %dMB"), m_nCapacity / (1024*1024));
	m_staticFreeSpace.SetWindowText(str);

	pDevice->release();
}

void CDataBurnerDlg::EnableIsoGroup(BOOL bEnable)
{
	m_groupIsoAll.EnableWindow(bEnable);
	
	GetDlgItem(IDC_RADIO_ISO_ALL)->EnableWindow(bEnable);
	GetDlgItem(IDC_RADIO_ISO_LEVEL2)->EnableWindow(bEnable);
	GetDlgItem(IDC_RADIO_ISO_LEVEL1)->EnableWindow(bEnable);

    m_checkIsoTreeDepth.EnableWindow(bEnable);
	m_checkIsoTranslateNames.EnableWindow(bEnable);
}

void CDataBurnerDlg::EnableJolietGroup(BOOL bEnable)
{
	m_groupJolietAll.EnableWindow(bEnable);
	GetDlgItem(IDC_RADIO_JOLIET_ALL)->EnableWindow(bEnable);
	GetDlgItem(IDC_RADIO_JOLIET_SHORTNAMES)->EnableWindow(bEnable);
}

BOOL CDataBurnerDlg::OnInitDialog()
{
	CDialog::OnInitDialog();

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

	// Set the icon for this dialog.  The framework does this automatically
	//  when the application's main window is not a dialog
	SetIcon(m_hIcon, TRUE);			// Set big icon
	SetIcon(m_hIcon, FALSE);		// Set small icon
	
	// Add extra initialization here
	m_nRequiredSpace = 0;
	m_nDevicesCount = 0;

	// Session Mode
	m_comboMode.InsertString(0, _T("Disk-At-Once"));
	m_comboMode.InsertString(1, _T("Session-At-Once"));
	m_comboMode.InsertString(2, _T("Track-At-Once"));
	m_comboMode.InsertString(3, _T("Packet"));
	m_comboMode.SetCurSel(2);

	// Image Types
	m_comboImageType.InsertString(0, _T("ISO9660"));
	m_comboImageType.InsertString(1, _T("Joliet"));
	m_comboImageType.InsertString(2, _T("UDF"));
	m_comboImageType.InsertString(3, _T("UDF & ISO9660"));
	m_comboImageType.InsertString(4, _T("UDF & Joliet"));
	m_comboImageType.SetCurSel(1);

	OnCbnSelchangeComboImageType();

	m_comboBootEmulationType.InsertString(0, _T("No Emulation"));
	m_comboBootEmulationType.InsertString(1, _T("Floppy Emulation 1.20MB"));
	m_comboBootEmulationType.InsertString(2, _T("Floppy Emulation 1.44MB"));
	m_comboBootEmulationType.InsertString(3, _T("Floppy Emulation 2.88MB"));
	m_comboBootEmulationType.SetCurSel(0);

	OnBnClickedCheckBootImage();

	// ISO
	CheckRadioButton(IDC_RADIO_ISO_ALL, IDC_RADIO_ISO_LEVEL2, IDC_RADIO_ISO_ALL);
	CheckRadioButton(IDC_RADIO_JOLIET_ALL, IDC_RADIO_JOLIET_SHORTNAMES, IDC_RADIO_JOLIET_ALL);

	// Write parameters
	m_chkTest.SetCheck(FALSE);
	m_chkEject.SetCheck(FALSE);

	m_chkCloseDisc.SetCheck(TRUE);
	m_chkRaw.SetCheck(FALSE);
	m_chkCdRomXa.SetCheck(FALSE);

	// Erase
	m_chkQuick.SetCheck(TRUE);

	// Multisession
	m_chkLoadLastTrack.SetCheck(TRUE);

	// Fill in devices list	
	Library::enableTraceLog(NULL, TRUE);

	m_pEngine = Library::createEngine();
	BOOL bResult = m_pEngine->initialize();
	if (!bResult) 
	{
		AfxMessageBox(_T("Unable to initialize CD writing engine."));
		EndModalLoop(-1);
		if (m_pEnum) 
		{
			m_pEnum->release();
			m_pEnum=NULL;
		}

		return FALSE;
	}

	m_pEnum = m_pEngine->createDeviceEnumerator();
	int nDevices = m_pEnum->count();
	if (!nDevices) 
	{
		AfxMessageBox(_T("No CD devices available."));
		EndModalLoop(-1);
		if (m_pEnum) 
		{
			m_pEnum->release();
			m_pEnum=NULL;
		}
		return FALSE;
	}

	m_nDevicesCount = 0;
	for (int i = 0; i < nDevices; i++) 
	{
		Device* pDevice= m_pEnum->createDevice(i);
		if (!pDevice)
			continue;
		
		BOOL bWrite = isWritePossible(pDevice);
		if (bWrite) 
		{
			TCHAR tchLetter = pDevice->driveLetter();
			TCHAR tcsName[256] = {0};
			_stprintf(tcsName, _T("(%C:) - %s"), tchLetter, pDevice->description());

			m_arDeviceNames.Add(tcsName);
			m_arIndices.Add(i);

			m_comboDevices.AddString(m_arDeviceNames[m_nDevicesCount++]);
		}

		pDevice->release();
	}

	if (m_nDevicesCount)
	{
		m_comboDevices.SetCurSel(0);
		SetDeviceControls();
	}
	else 
	{
		AfxMessageBox(_T("Could not find any CD-R devices."));

		if (m_pEnum) 
		{
			m_pEnum->release();
			m_pEnum=NULL;
		}

		return FALSE;
	}

	return TRUE;  // return TRUE  unless you set the focus to a control
}

void CDataBurnerDlg::OnSysCommand(UINT nID, LPARAM lParam)
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

// Uses FindFile and WIN32_FIND_DATA to get the file list
void CDataBurnerDlg::ProcessInputTree(DWORD dwImageType, DataFile * pCurrentFile, CString & sCurrentPath)
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
			pDataFile->setShortFilename(FindFileData.cAlternateFileName);

			if(FindFileData.dwFileAttributes & FILE_ATTRIBUTE_HIDDEN)
				pDataFile->setHiddenMask(dwImageType);

			pDataFile->setCreationTime(FindFileData.ftCreationTime);

			// Search for all files in the directory
			ProcessInputTree(dwImageType, pDataFile, sFullPath);

			// Add this folder to the tree
			pCurrentFile->children()->add(pDataFile);

			pDataFile->release();
		} 
		else
		{
			// File
			DataFile * pDataFile = Library::createDataFile(); 

			pDataFile->setDirectory(FALSE);
			pDataFile->setPath((LPCTSTR)sFullPath);			
			pDataFile->setLongFilename((LPCTSTR)sFileName);
			pDataFile->setShortFilename(FindFileData.cAlternateFileName);

			if(FindFileData.dwFileAttributes & FILE_ATTRIBUTE_HIDDEN)
				pDataFile->setHiddenMask(dwImageType);

			pDataFile->setCreationTime(FindFileData.ftCreationTime);

			// Add this file to the tree
			pCurrentFile->children()->add(pDataFile);

			pDataFile->release();
		}

	} while (FindNextFile (hfind, &FindFileData));
	FindClose(hfind);
}

BOOL CDataBurnerDlg::SetImageLayoutFromFolder(DataDisc* pDataCD, LPCTSTR fname)
{
	DataFile * pDataFile = Library::createDataFile();

	// Entry for the root of the image file system
	pDataFile->setDirectory(TRUE);
	pDataFile->setPath(_T("\\"));			
	pDataFile->setLongFilename(_T("\\"));

	try
	{
		CString sFullPath = fname;
		ProcessInputTree(pDataCD->imageType(), pDataFile, sFullPath);
	}
	catch(ProcessInputTreeException &ex)
	{
		TRACE(_T("SetImageLayoutFromFolder Error: %s") , ex.what());
		
		pDataFile->release();
		return FALSE;
	}

	BOOL bRes = pDataCD->setImageLayout(pDataFile);

	pDataFile->release();
	return bRes;
}

void CDataBurnerDlg::OnButtonBrowse() 
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

	DataDisc* pDataCD = Library::createDataDisc();
	pDataCD->setImageType(GetImageType());
	
	CWaitCursor wc;

	BOOL bRes = SetImageLayoutFromFolder(pDataCD, m_strRootDir);
	if (!bRes) 
	{
		CString str; str.Format(_T("A problem occured when trying to set directory:\n%s"), m_strRootDir);
		AfxMessageBox(str);

		m_strRootDir = "";
	} 
	else 
	{
		m_nRequiredSpace = pDataCD->imageSizeInBytes();
	}

	SetDeviceControls();
	pDataCD->release();
}

BOOL CDataBurnerDlg::DirectoryExists()
{
	TCHAR tcs[1024];
	m_editRootDir.GetWindowText(tcs, 1024);

	if (_taccess(tcs, 0)!=-1)
		return TRUE;
	else
		return FALSE;
}

void CDataBurnerDlg::OnChangeEditRoot() 
{
	m_editRootDir.Invalidate(FALSE);
}

BOOL CDataBurnerDlg::ValidateMedia(int nOperation) 
{
	if (OPERATION_BURN_ON_THE_FLY == nOperation || 
		OPERATION_IMAGE_BURN == nOperation)
	{
		int nDevice = m_comboDevices.GetCurSel();

		// No devices and not an OPERATION_IMAGE_CREATE
		if (-1 == nDevice)
		{
			AfxMessageBox(_T("Please select a device."));
			return FALSE;
		}

		// Get device object. 
		Device* pDevice = m_pEnum->createDevice(m_arIndices[nDevice], FALSE);
		if (NULL == pDevice)
			return TRUE;

		// Verify that the media in the device is CD
		if (!pDevice->isMediaCD())
		{
			AfxMessageBox(_T("This samples works with CD media only. For DVD media, please see the DVDBurner sample. For Blu-Ray media, please see the BluRayBurner sample."));
			pDevice->release();
			return FALSE;
		}

		pDevice->release();
		return TRUE;
	}

	return TRUE;
}

BOOL CDataBurnerDlg::ValidateForm() 
{
	if (!DirectoryExists()) 
	{
		AfxMessageBox(_T("Please specify a valid source folder."));
		m_editRootDir.SetFocus();
		return FALSE;
	}

	if (m_strVolume.GetLength() > 16) 
	{
		AfxMessageBox(_T("Volume name can contain maximum 16 characters"));
		m_editVolume.SetFocus();
		return FALSE;
	}

	if (m_strRootDir[m_strRootDir.GetLength()-1] == '\\' || m_strRootDir[m_strRootDir.GetLength()-1] == '/') 
		m_strRootDir = m_strRootDir.Left(m_strRootDir.GetLength()-1);

	CString strBootImage;
	m_editBootImage.GetWindowText(strBootImage);

	
	if(m_bBootableCD && strBootImage.IsEmpty() && (ImageTypeFlags::Udf != GetImageType()))
	{
		AfxMessageBox(_T("Please specify a boot image."));
		return FALSE;
	}

	return TRUE;
}

void CDataBurnerDlg::RunOperation(int nOperation) 
{
	EnableWindow(FALSE);

	CDialogProgress dlg;
	dlg.Create();
	dlg.ShowWindow(SW_SHOW);
	dlg.UpdateWindow();

	int nSel;
	m_ctx.bStopRequest = FALSE;
	m_ctx.bErasing = FALSE;

	m_ctx.bSimulate = m_chkTest.GetCheck();
	m_ctx.bEject = m_chkEject.GetCheck();

	m_ctx.bCacheSmallFiles=m_bCacheSmallFiles;
	m_ctx.dwSmallFilesCacheLimit=m_dwSmallFilesCache;
	m_ctx.dwSmallFileSize=m_dwSmallFile;

	nSel = m_comboSpeed.GetCurSel();
	m_ctx.nSpeedKB = (INT32)m_comboSpeed.GetItemData(nSel);
	m_ctx.nOperation = nOperation;

	m_ctx.nMode = m_comboMode.GetCurSel();
	m_ctx.bCloseDisc = m_chkCloseDisc.GetCheck();
	m_ctx.bRaw = m_chkRaw.GetCheck();

	m_ctx.imageType = GetImageType();

	m_ctx.iso.nLimits = GetCheckedRadioButton(IDC_RADIO_ISO_ALL, IDC_RADIO_ISO_LEVEL1);
	m_ctx.iso.bTreeDepth = m_checkIsoTreeDepth.GetCheck();
	m_ctx.iso.bTranslateNames = m_checkIsoTranslateNames.GetCheck();

	m_ctx.joliet.nLimits = GetCheckedRadioButton(IDC_RADIO_JOLIET_ALL, IDC_RADIO_JOLIET_SHORTNAMES);

	m_ctx.bCdRomXa = m_chkCdRomXa.GetCheck();

	m_ctx.bLoadLastTrack = (BST_CHECKED == m_chkLoadLastTrack.GetCheck());

	m_editBootImage.GetWindowText(m_ctx.strBootImageFile);
	
	m_ctx.bBootableCD = m_bBootableCD && (m_ctx.imageType != ImageTypeFlags::Udf);

	m_ctx.bootEmulation = GetBootEmulationType();

	ResetEvent(m_hOperationStartedEvent);
	ResetEvent(m_hNotifyEvent);

	// Create thread to execute the current operation
	DWORD dwId;
	m_hThread=CreateThread(NULL, NULL, ProcessThreadProc, this, NULL, &dwId);
	WaitForSingleObject(m_hOperationStartedEvent, INFINITE);

	while (WaitForSingleObject(m_hThread, 50) == WAIT_TIMEOUT) 
	{
		___PumpMessages();

		if (dlg.m_bStopped)
			m_ctx.bStopRequest = TRUE;

		if (WAIT_OBJECT_0 == WaitForSingleObject(m_hNotifyEvent, 0)) 
		{
			ResetEvent(m_hNotifyEvent);
			EnterCriticalSection(&m_cs);

				dlg.SetStatus(m_notify.strText);
				dlg.SetProgress(m_notify.nPercent);
				dlg.SetInternalBuffer(m_notify.nUsedCachePercent);
				dlg.SetActualWriteSpeed(m_notify.nActualWriteSpeed);

			LeaveCriticalSection(&m_cs);
		}
	}

	dlg.DestroyWindow();
	EnableWindow();
	BringWindowToTop();
	SetActiveWindow();

	m_notify.nPercent = 0;
	m_notify.nUsedCachePercent = 0;
	m_notify.strText = "";

	SetDeviceControls();
}

void CDataBurnerDlg::___PumpMessages() 
{
	MSG msg;
	while (PeekMessage(&msg, 0, 0, 0, PM_REMOVE))
		DispatchMessage(&msg);
}

// Operation thread...
DWORD CDataBurnerDlg::ProcessThreadProc(LPVOID pParam) 
{
	CDataBurnerDlg* pThis = (CDataBurnerDlg* )pParam;
	return pThis->ProcessThread();

}

//  Thread main function
DWORD CDataBurnerDlg::ProcessThread() 
{
	SetEvent(m_hOperationStartedEvent);

	// Get a device 
	int nDevice = m_comboDevices.GetCurSel();
	if (-1 == nDevice && OPERATION_IMAGE_CREATE == m_ctx.nOperation)
	{	
		ImageCreate(NULL, 0);
		return 0;
	}

	// No devices and not an OPERATION_IMAGE_CREATE - nothing to do here
	if (-1 == nDevice)
		return -1;

	// Get device object. 
	m_pDevice = m_pEnum->createDevice(m_arIndices[nDevice], TRUE);
	if (NULL == m_pDevice)
		return -2;

	// Erase
	if (m_ctx.bErasing) 
	{
		m_ctx.bErasing = FALSE;
		m_pDevice->setWriteSpeedKB(m_ctx.nSpeedKB);
		m_pDevice->erase(m_ctx.bQuick);

		m_pDevice->release();
		m_pDevice = NULL;

		return 0;
	}


	// Burn
	switch (m_ctx.nOperation) 
	{
		case OPERATION_IMAGE_CREATE:
			ImageCreate(m_pDevice, 0);
		break;

		case OPERATION_IMAGE_BURN:
			ImageBurn(m_pDevice, 0);
		break;

		case OPERATION_BURN_ON_THE_FLY:
			// Get the last track number from the last session if multisession option was specified
			int nPrevTrackNumber = m_ctx.bLoadLastTrack ? GetLastCompleteTrack(m_pDevice) : 0;
			BurnOnTheFly(m_pDevice, nPrevTrackNumber);
		break;
	}

	if (NULL != m_pDevice)
	{
		// Call IDevice::Dismount to dismount the system volume. This works only on Windows NT, 2000 and XP.
		m_pDevice->dismount();

		m_pDevice->release();
		m_pDevice = NULL;
	}

	return 0;
}


void CDataBurnerDlg::OnButtonCreate() 
{
	if (!UpdateData())
		return;

	if (!ValidateForm())
		return;

	if (!ValidateMedia(OPERATION_BURN_ON_THE_FLY))
		return;

	RunOperation(OPERATION_BURN_ON_THE_FLY);
}

//------------------------------------------------------------------
//   FirstDriveFromMask (unitmask)
//
//   Description
//     Finds the first valid drive letter from a mask of drive letters.
//     The mask must be in the format bit 0 = A, bit 1 = B, bit 3 = C, 
//     etc. A valid drive letter is defined when the corresponding bit 
//     is set to 1.
//
//   Returns the first drive letter that was found.
//--------------------------------------------------------------------

char FirstDriveFromMask (ULONG unitmask)
{
   char i;

   for (i = 0; i < 26; ++i)
   {
      if (unitmask & 0x1)
         break;
      unitmask = unitmask >> 1;
   }

   return (i + 'A');
}

LRESULT CDataBurnerDlg::OnDeviceChange(WPARAM wParam, LPARAM lParam) 
{
	PDEV_BROADCAST_HDR lpdb = (PDEV_BROADCAST_HDR)lParam;

	TRACE(_T("WM_DEVICECHANGE: wParam: 0x%x, lParam: 0x%x \n"), wParam, lParam);
	switch(wParam)
	{
		case DBT_DEVICEARRIVAL:
			// Check whether a CD or DVD was inserted into a drive.
			if (lpdb -> dbch_devicetype == DBT_DEVTYP_VOLUME)
			{
				PDEV_BROADCAST_VOLUME lpdbv = (PDEV_BROADCAST_VOLUME)lpdb;

				if (lpdbv -> dbcv_flags & DBTF_MEDIA)
					TRACE(_T("Drive %c: Media has arrived.\n"), FirstDriveFromMask(lpdbv ->dbcv_unitmask));
			}
		break;

		case DBT_DEVICEREMOVECOMPLETE:
			// Check whether a CD or DVD was removed from a drive.
			if (lpdb -> dbch_devicetype == DBT_DEVTYP_VOLUME)
			{
				PDEV_BROADCAST_VOLUME lpdbv = (PDEV_BROADCAST_VOLUME)lpdb;
				if (lpdbv -> dbcv_flags & DBTF_MEDIA)
					TRACE(_T("Drive %c: Media was removed.\n"), FirstDriveFromMask(lpdbv ->dbcv_unitmask));
			}
		break;

		default:
			/*
			Process other WM_DEVICECHANGE notifications for other 
			devices or reasons.
			*/ 
		break;
	}

	SetDeviceControls();
	return TRUE;
}


void CDataBurnerDlg::OnDestroy() 
{
	CDialog::OnDestroy();

	if (m_pEnum)
		m_pEnum->release();

	if (m_pEngine)
	{
		m_pEngine->shutdown();
		m_pEngine->release();
	}

	Library::disableTraceLog();
}

void CDataBurnerDlg::OnChangeEditVolume() 
{
	m_editVolume.Invalidate(FALSE);
}

void CDataBurnerDlg::onProgress(int64_t bytesWritten, int64_t all) 
{
	EnterCriticalSection(&m_cs);
	
	double dd = (double)__int64(bytesWritten) * 100.0 / (double)__int64(all);
	m_notify.nPercent=(int)dd;

	if (m_pDevice)
	{
		dd = (double) m_pDevice->internalCacheUsedSpace();
		dd = dd * 100.0 / (double)m_pDevice->internalCacheCapacity();
		m_notify.nUsedCachePercent = (int)dd;

		m_notify.nActualWriteSpeed = m_pDevice->writeTransferRate(); 
	}
	else
		m_notify.nUsedCachePercent = 0;

	LeaveCriticalSection(&m_cs);
	SetEvent(m_hNotifyEvent);
}

CString CDataBurnerDlg::GetTextStatus(DataDiscStatus::Enum eStatus)
{
	switch(eStatus)
	{
		case DataDiscStatus::BuildingFileSystem:
			return CString("Building filesystem...");
		
		case DataDiscStatus::WritingFileSystem:
			return CString("Writing filesystem...");

		case DataDiscStatus::WritingImage:
			return CString("Writing image...");

		case DataDiscStatus::CachingSmallFiles:
			return CString("Caching small files...");

		case DataDiscStatus::CachingNetworkFiles:
			return CString("Caching network files...");

		case DataDiscStatus::CachingCDRomFiles:
			return CString("Caching CDROM files...");

		case DataDiscStatus::Initializing:
			return CString("Initializing and writing lead-in...");

		case DataDiscStatus::Writing:
			return CString("Writing...");

		case DataDiscStatus::WritingLeadOut:
			return CString("Writing lead-out and flushing cache...");
	}

	return CString("Unknown status...");
}

void CDataBurnerDlg::onStatus(DataDiscStatus::Enum eStatus) 
{
	EnterCriticalSection(&m_cs);

	m_notify.strText = GetTextStatus(eStatus);
	LeaveCriticalSection(&m_cs);
	SetEvent(m_hNotifyEvent);
}

void CDataBurnerDlg::onFileStatus(int32_t fileNumber, const char_t* filename, int32_t percentWritten)
{
	// ATLTRACE("%d\n", nPercent);
}

bool_t CDataBurnerDlg::onContinueWrite()
{
	if (m_ctx.bStopRequest)
		return FALSE;

	return TRUE;
}

DWORD GetConstraints(TOperationContext & ctx)
{
	DWORD dwRes = 0;
	if (0 == ctx.iso.nLimits - IDC_RADIO_ISO_ALL)
		dwRes |= ImageConstraintFlags::IsoLongLevel2;

	if (1 == ctx.iso.nLimits - IDC_RADIO_ISO_ALL)
		dwRes |= ImageConstraintFlags::IsoLevel2;

	if (2 == ctx.iso.nLimits - IDC_RADIO_ISO_ALL)
		dwRes |= ImageConstraintFlags::IsoLevel1;

	if (!ctx.iso.bTreeDepth)
		dwRes = dwRes & ~ImageConstraintFlags::IsoTreeDepth;

	if (1 == ctx.joliet.nLimits - IDC_RADIO_JOLIET_ALL)
		dwRes |= ImageConstraintFlags::JolietStandard;

	return dwRes;
}

void CDataBurnerDlg::ShowErrorMessage(const ErrorInfo *pErrInfo)
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

bool CDataBurnerDlg::isWritePossible(Device *device) const
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

bool CDataBurnerDlg::isRewritePossible(Device *device) const
{
	CDFeatures *cdfeatures = device->cdFeatures();
	DVDFeatures *dvdfeatures = device->dvdFeatures();
	BDFeatures *bdfeatures = device->bdFeatures();

	bool cdRewritePossible = cdfeatures->canWriteCDRW();
	bool dvdRewritePossible = dvdfeatures->canWriteDVDMinusRW() || dvdfeatures->canWriteDVDPlusRW() ||
		dvdfeatures->canWriteDVDRam();
	bool bdRewritePossible = bdfeatures->canWriteBDRE();
	return cdRewritePossible || dvdRewritePossible || bdRewritePossible;
}

void CDataBurnerDlg::SetVolumeProperties(DataDisc* pDataDisc, const CString& volumeLabel, primo::burner::ImageTypeFlags::Enum imageType)
{
	// set volume times
	SYSTEMTIME st;
	GetSystemTime(&st);

	FILETIME ft;
	SystemTimeToFileTime(&st, &ft);
	
	if((ImageTypeFlags::Iso9660 & imageType) ||
		(ImageTypeFlags::Joliet & imageType))
	{
		IsoVolumeProps *iso = pDataDisc->isoVolumeProps();
		
		iso->setVolumeLabel(volumeLabel);

		// Sample settings. Replace with your own data or leave empty
		iso->setSystemID(_T("WINDOWS"));
		iso->setVolumeSet(_TEXT("SET"));
		iso->setPublisher(_T("PUBLISHER"));
		iso->setDataPreparer(_T("PREPARER"));
		iso->setApplication(_T("DVDBURNER"));
		iso->setCopyrightFile(_T("COPYRIGHT.TXT"));
		iso->setAbstractFile(_T("ABSTRACT.TXT"));
		iso->setBibliographicFile(_T("BIBLIO.TXT"));

		iso->setVolumeCreationTime(ft);
	}

	if(ImageTypeFlags::Joliet & imageType)
	{
		JolietVolumeProps *joliet = pDataDisc->jolietVolumeProps();
		
		joliet->setVolumeLabel(volumeLabel);

		// Sample settings. Replace with your own data or leave empty
		joliet->setSystemID(_T("WINDOWS"));
		joliet->setVolumeSet(_TEXT("SET"));
		joliet->setPublisher(_T("PUBLISHER"));
		joliet->setDataPreparer(_T("PREPARER"));
		joliet->setApplication(_T("DVDBURNER"));
		joliet->setCopyrightFile(_T("COPYRIGHT.TXT"));
		joliet->setAbstractFile(_T("ABSTRACT.TXT"));
		joliet->setBibliographicFile(_T("BIBLIO.TXT"));

		joliet->setVolumeCreationTime(ft);
	}

	if(ImageTypeFlags::Udf & imageType)
	{
		UdfVolumeProps *udf = pDataDisc->udfVolumeProps();
		
		udf->setVolumeLabel(volumeLabel);

		// Sample settings. Replace with your own data or leave empty
		udf->setVolumeSet(_TEXT("SET"));
		udf->setCopyrightFile(_T("COPYRIGHT.TXT"));
		udf->setAbstractFile(_T("ABSTRACT.TXT"));

		udf->setVolumeCreationTime(ft);
	}
}

BOOL CDataBurnerDlg::ImageCreate(Device* pDevice, int nPrevTrackNumber) 
{
	DataDisc* pDataCD = Library::createDataDisc();

	SetVolumeProperties(pDataCD, m_strVolume, m_ctx.imageType);
	
	pDataCD->setImageType(m_ctx.imageType);
	pDataCD->setImageConstraints(GetConstraints(m_ctx));
	pDataCD->setFilenameTranslation(m_ctx.iso.bTranslateNames);

	// Bootable parameters
	pDataCD->setBootable(m_ctx.bBootableCD);

	if(m_ctx.bBootableCD)
	{
		pDataCD->bootProps()->setEmulation(m_ctx.bootEmulation);
		pDataCD->bootProps()->setImageFile(m_ctx.strBootImageFile);

		// Bootable discs for Windows need to disable this features
		// to make the disc compatible with the boot loader.

		pDataCD->isoVolumeProps()->setDotAppendedToNames(FALSE);
		pDataCD->jolietVolumeProps()->setDotAppendedToNames(FALSE);

		pDataCD->isoVolumeProps()->setVersionAppendedToNames(FALSE);
		pDataCD->jolietVolumeProps()->setVersionAppendedToNames(FALSE);
	}

	pDataCD->setSessionStartAddress(0);
	pDataCD->setCallback(this);

    BOOL bRes = SetImageLayoutFromFolder(pDataCD, m_strRootDir);
	if (!bRes) 
	{
		ShowErrorMessage(pDataCD->error());
		pDataCD->release();
		return FALSE;
	}

	bRes = pDataCD->writeToImageFile(m_ctx.strImageFile);
	if (!bRes) 
	{
		ShowErrorMessage(pDataCD->error());
		pDataCD->release();
		return FALSE;
	}

	pDataCD->release();
	return TRUE;
}

BOOL CDataBurnerDlg::ImageBurn(Device * pDevice, int nPrevTrackNumber) 
{
	pDevice->setWriteSpeedKB(m_ctx.nSpeedKB);

	DataDisc * pDataCD = Library::createDataDisc();
	pDataCD->setCallback(this);

	pDataCD->setDevice(pDevice);
	pDataCD->setSessionStartAddress(pDevice->newSessionStartAddress());

	pDataCD->cachePolicy()->setCacheSmallFiles(m_ctx.bCacheSmallFiles);

	if (m_ctx.bCacheSmallFiles) 
	{
		pDataCD->cachePolicy()->setSmallFileSizeThreshold(m_ctx.dwSmallFileSize);
		pDataCD->cachePolicy()->setSmallFilesCacheSize(m_ctx.dwSmallFilesCacheLimit);
	}

	pDataCD->setSimulateBurn(m_ctx.bSimulate);
	
	// Set write mode
	if (!m_ctx.bRaw && (0 == m_ctx.nMode || 1 == m_ctx.nMode))
		// Session-At-Once (also called Disc-At-Once)
		pDataCD->setWriteMethod(WriteMethod::Sao);
	else if (m_ctx.bRaw)
		// RAW Disc-At-Once
		pDataCD->setWriteMethod(WriteMethod::RawDao);
	else if (2 == m_ctx.nMode)
		// Track-At-Once
		pDataCD->setWriteMethod(WriteMethod::Tao);
	else
		// Packet
		pDataCD->setWriteMethod(WriteMethod::Packet);

	pDataCD->setCloseDisc(m_ctx.bCloseDisc);

	// Burn
	BOOL bRes = pDataCD->writeImageToDisc(m_ctx.strImageFile);
	if (!bRes) 
	{
		ShowErrorMessage(pDataCD->error());
		pDataCD->release();
		return FALSE;
	}

	if (m_ctx.bEject)
		pDevice->eject(TRUE);

	pDataCD->release();
	return TRUE;
}

BOOL CDataBurnerDlg::BurnOnTheFly(Device* pDevice, int nPrevTrackNumber) 
{
	if (!pDevice)
		return FALSE;

	pDevice->setWriteSpeedKB(m_ctx.nSpeedKB);

	// Create a data cd instance that we will use to burn
	DataDisc* pDataCD = Library::createDataDisc();
	pDataCD->setDevice(pDevice);

	// Set the session start address. Must do this before intializing the directory structure.
	pDataCD->setSessionStartAddress(pDevice->newSessionStartAddress());

	// Multi-session. Load previous track
	if (nPrevTrackNumber > 0)
		pDataCD->setLayoutLoadTrack(nPrevTrackNumber);

	// Set burning parameters
	pDataCD->setImageType(m_ctx.imageType);
	pDataCD->setImageConstraints(GetConstraints(m_ctx));
	pDataCD->setFilenameTranslation(m_ctx.iso.bTranslateNames);

	// Bootable parameters
	pDataCD->setBootable(m_ctx.bBootableCD);

	if(m_ctx.bBootableCD)
	{
		pDataCD->bootProps()->setEmulation(m_ctx.bootEmulation);
		pDataCD->bootProps()->setImageFile(m_ctx.strBootImageFile);

		// Bootable discs for Windows need to disable this features
		// to make the disc compatible with the boot loader.

		pDataCD->isoVolumeProps()->setDotAppendedToNames(FALSE);
		pDataCD->jolietVolumeProps()->setDotAppendedToNames(FALSE);

		pDataCD->isoVolumeProps()->setVersionAppendedToNames(FALSE);
		pDataCD->jolietVolumeProps()->setVersionAppendedToNames(FALSE);
	}

	SetVolumeProperties(pDataCD, m_strVolume, m_ctx.imageType);

	pDataCD->setCallback(this);

	pDataCD->setSimulateBurn(m_ctx.bSimulate);

	// Set write mode
	if (!m_ctx.bRaw && (0 == m_ctx.nMode || 1 == m_ctx.nMode))
		// Session-At-Once (also called Disc-At-Once)
		pDataCD->setWriteMethod(WriteMethod::Sao);
	else if (m_ctx.bRaw)
		// RAW Disc-At-Once
		pDataCD->setWriteMethod(WriteMethod::RawDao);
	else if (2 == m_ctx.nMode)
		// Track-At-Once
		pDataCD->setWriteMethod(WriteMethod::Tao);
	else
		// Packet
		pDataCD->setWriteMethod(WriteMethod::Packet);

	// CD-ROM XA
	pDataCD->setCDRomXA(m_ctx.bCdRomXa);

	// CloseDisc controls multi-session. Disc must be left open when multi-sessions are desired. 
	pDataCD->setCloseSession(TRUE);
	pDataCD->setCloseDisc(m_ctx.bCloseDisc);


	BOOL bRes = SetImageLayoutFromFolder(pDataCD, m_strRootDir);
	if (!bRes) 
	{
		ShowErrorMessage(pDataCD->error());
		pDataCD->release();
		return FALSE;
	}

	// Burn 
	while (true)
	{
		// Try to write the image
		bRes = pDataCD->writeToDisc();
		if (!bRes)
		{
			// Check if the error is: Cannot load image layout. 
			// If so most likely it is an empty formatted DVD+RW or empty formatted DVD-RW RO with one track. 
			if((pDataCD->error()->facility() == ErrorFacility::DataDisc) &&
				(pDataCD->error()->code() == DataDiscError::CannotLoadImageLayout))
			{
				// Set to 0 to disable previous data session loading
				pDataCD->setLayoutLoadTrack(0);

				// try to write it again
				continue;
			}
		}

		break;
	}

	// Check result and show error message
	if (!bRes) 
	{
		ShowErrorMessage(pDataCD->error());
		
		pDataCD->release();
		return FALSE;
	}

	if (m_ctx.bEject)
		pDevice->eject(TRUE);

	pDataCD->release();
	return TRUE;
}

int CDataBurnerDlg::GetLastCompleteTrack(Device * pDevice)
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
		// All other media is recorded using tracks and sessions and multi-session is no different 
		// than with the CD. 

		// Use the ReadDiskInfo method to get the last track number
		DiscInfo *pDI = pDevice->readDiscInfo();
		if(NULL != pDI)
		{
			nLastTrack = pDI->lastTrack();
			
			// readDiskInfo reports the empty space as a track too
			// That's why we need to go back one track to get the last completed track
			if ((DiscStatus::Open == pDI->discStatus()) || (DiscStatus::Empty == pDI->discStatus()))
				nLastTrack--;

			pDI->release();
		}
	}

	return nLastTrack;
}

ImageTypeFlags::Enum CDataBurnerDlg::GetImageType()
{
	switch(m_comboImageType.GetCurSel())
	{
		case 0:
			return ImageTypeFlags::Iso9660;
		case 1:
			return ImageTypeFlags::Joliet;
		case 2:
			return ImageTypeFlags::Udf;
		case 3:
			return ImageTypeFlags::UdfIso;
		case 4:
			return ImageTypeFlags::UdfJoliet;

		default:
			return ImageTypeFlags::Joliet;
	}
}

BootEmulation::Enum CDataBurnerDlg::GetBootEmulationType()
{
	switch(m_comboBootEmulationType.GetCurSel())
	{
	case 0:
		return BootEmulation::NoEmulation;
	case 1:
		return BootEmulation::Diskette120;
	case 2:
		return BootEmulation::Diskette144;
	case 3:
		return BootEmulation::Diskette288;
	default:
		return BootEmulation::NoEmulation;
	}
}

void CDataBurnerDlg::OnButtonBurnImage() 
{
	if (!UpdateData())
		return;

	CFileDialog dlg(TRUE, _T("*.iso"), NULL, OFN_HIDEREADONLY|OFN_OVERWRITEPROMPT, 
							_T("Image File (*.iso)|*.iso||"), NULL );
	if(IDOK!=dlg.DoModal())
		return;

	m_ctx.strImageFile = dlg.m_ofn.lpstrFile;

	HANDLE hFile = ::CreateFile(m_ctx.strImageFile, GENERIC_READ, 
									FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, 
									OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);

	if (INVALID_HANDLE_VALUE == hFile) 
	{
		CString str; str.Format(_T("Cannot open %s."), m_ctx.strImageFile);
		AfxMessageBox(str);
		return;
	}

	// This sample assumes CD only. For DVDs you will need to use the 
	// second parameter of GetFileSize
	DWORD dwFileSize = ::GetFileSize(hFile, NULL);
	CloseHandle(hFile);

	if (dwFileSize > (DWORD)m_nCapacity) 
	{
		CString str; str.Format(_T("Cannot write image file %s.\nThe file is too big."), m_ctx.strImageFile);
		AfxMessageBox(str);
		return;
	}

	if (!ValidateMedia(OPERATION_IMAGE_BURN))
		return;

	RunOperation(OPERATION_IMAGE_BURN);
}

void CDataBurnerDlg::OnButtonCreateImage() 
{
	if (!UpdateData())
		return;

	if (!ValidateForm())
		return;

	CFileDialog dlg(false, _T("*.iso"), NULL, OFN_HIDEREADONLY | OFN_OVERWRITEPROMPT | OFN_NONETWORKBUTTON, _T("Image File (*.iso)|*.iso||"), NULL );
	if(IDOK != dlg.DoModal())
		return;

	m_ctx.strImageFile = dlg.m_ofn.lpstrFile;
	RunOperation(OPERATION_IMAGE_CREATE);
}

void CDataBurnerDlg::OnCheckCacheSmallFiles()
{
	if (m_chkCacheSmallFiles.GetCheck()) 
	{
		GetDlgItem(IDC_EDIT_SMALL_FILE)->EnableWindow();
		GetDlgItem(IDC_EDIT_SMALL_FILES_CACHE)->EnableWindow();
		GetDlgItem(IDC_STATIC_SMALL_FILE)->EnableWindow();
		GetDlgItem(IDC_STATIC_SMALL_FILES_CACHE)->EnableWindow();
	}
	else
	{
		GetDlgItem(IDC_EDIT_SMALL_FILE)->EnableWindow(FALSE);
		GetDlgItem(IDC_EDIT_SMALL_FILES_CACHE)->EnableWindow(FALSE);
		GetDlgItem(IDC_STATIC_SMALL_FILE)->EnableWindow(FALSE);
		GetDlgItem(IDC_STATIC_SMALL_FILES_CACHE)->EnableWindow(FALSE);
	}
}

void CDataBurnerDlg::OnCbnSelchangeComboDevices()
{
	int nCurSel = m_comboDevices.GetCurSel();
	if (-1 != nCurSel)
		SetDeviceControls();
}

void CDataBurnerDlg::OnCbnSelchangeComboMode()
{
	// DAO
	if (m_bRawDao && 0 == m_comboMode.GetCurSel())
	{
		m_chkRaw.EnableWindow(TRUE);
		m_chkCloseDisc.SetCheck(1);
		m_chkCloseDisc.EnableWindow(FALSE);
	}
	// SAO
	else if (1 == m_comboMode.GetCurSel())
	{
		m_chkCloseDisc.EnableWindow(TRUE);

		m_chkRaw.SetCheck(0);
		m_chkRaw.EnableWindow(FALSE);
	}
	// TAO
	else
	{
		m_chkCloseDisc.EnableWindow(TRUE);

		m_chkRaw.SetCheck(0);
		m_chkRaw.EnableWindow(FALSE);
	}


	OnBnClickedCheckRaw();
}

void CDataBurnerDlg::OnBnClickedCheckRaw()
{
	if (0 == m_comboMode.GetCurSel())
	{
		if (1 == m_chkRaw.GetCheck())
			m_chkCloseDisc.SetCheck(1);

		m_chkCloseDisc.EnableWindow(0 == m_chkRaw.GetCheck());
	}
}

void CDataBurnerDlg::OnBnClickedButtonEjectin()
{
	int nDevice = m_comboDevices.GetCurSel();

	Device* pDevice = m_pEnum->createDevice(m_arIndices[nDevice]);
	if (pDevice)
	{
		pDevice->eject(0);
		pDevice->release();
	}
}

void CDataBurnerDlg::OnBnClickedButtonEjectout()
{
	int nDevice = m_comboDevices.GetCurSel();

	Device* pDevice = m_pEnum->createDevice(m_arIndices[nDevice]);
	if (pDevice)
	{	
		pDevice->eject(1);
		pDevice->release();
	}
}

void CDataBurnerDlg::OnBnClickedButtonErase()
{
	EnableWindow(FALSE);

	CDialogProgress dlg;
	dlg.Create();
	dlg.ShowWindow(SW_SHOW);
	dlg.UpdateWindow();
	dlg.GetDlgItem(IDOK)->EnableWindow(FALSE);

	dlg.SetStatus("Erasing disc. Please wait...");

	m_ctx.bErasing = TRUE;
	m_ctx.bQuick = m_chkQuick.GetCheck();

	ResetEvent(m_hOperationStartedEvent);
	
	DWORD dwId;
	m_hThread = CreateThread(NULL, NULL, ProcessThreadProc, this, NULL, &dwId);
	WaitForSingleObject(m_hOperationStartedEvent, INFINITE);

	while (WaitForSingleObject(m_hThread, 0)==WAIT_TIMEOUT) 
	{
		___PumpMessages();
		Sleep(50);
	}

	dlg.DestroyWindow();
	
	EnableWindow();
	BringWindowToTop();
	SetActiveWindow();

	SetDeviceControls();
}

void CDataBurnerDlg::OnBnClickedRadioIsoLevel1()
{
}

void CDataBurnerDlg::OnBnClickedRadioIsoLevel2()
{
}

void CDataBurnerDlg::OnBnClickedRadioIsoAll()
{
}

void CDataBurnerDlg::OnCbnSelchangeComboImageType()
{
	ImageTypeFlags::Enum nImageType = GetImageType();
	
	bool bIso = (nImageType & ImageTypeFlags::Iso9660) > 0;
	bool bJoliet = (nImageType & ImageTypeFlags::Joliet) > 0;

	EnableJolietGroup(bJoliet);
	EnableIsoGroup(bIso);
	EnableBootGroup(bIso || bJoliet);
}

void CDataBurnerDlg::OnBnClickedButtonBrowseBootImage()
{
	if(!UpdateData())
		return;

	CFileDialog dlg(TRUE, NULL, NULL, OFN_HIDEREADONLY|OFN_OVERWRITEPROMPT, 
		_T("All Files (*.*)|*.*||"), NULL );

	if(IDOK != dlg.DoModal())
		return;

	m_editBootImage.SetWindowText(dlg.GetPathName());
	UpdateData(FALSE);
}

void CDataBurnerDlg::OnBnClickedCheckBootImage()
{
	UpdateData(TRUE);
	EnableBootGroup(TRUE);;
}

void CDataBurnerDlg::EnableBootGroup(BOOL bImageSupportBoot)
{
	m_buttonBrowseBootImage.EnableWindow(m_bBootableCD && bImageSupportBoot);
	m_editBootImage.EnableWindow(m_bBootableCD && bImageSupportBoot);
	m_comboBootEmulationType.EnableWindow(m_bBootableCD && bImageSupportBoot);
	m_checkBootImage.EnableWindow(bImageSupportBoot);
}