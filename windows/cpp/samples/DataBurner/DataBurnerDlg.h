// This is a part of the High Performance CD Engine Library.
// Copyright (C) 2001-2003 Primo Software Corporation
// All rights reserved.
//
// This source code is only intended as a supplement to the
// High Performance CD Engine Reference and related
// electronic documentation provided with the library.
// See these sources for detailed information regarding the	
// High Performance CD Engine product.

// DataBurnerDlg.h : header file
//
#pragma once
#include "afxwin.h"
#include <exception>
#include <string>


/////////////////////////////////////////////////////////////////////////////
// CDataBurnerDlg dialog

#define OPERATION_INVALID					(-1)
#define OPERATION_IMAGE_CREATE				1
#define OPERATION_IMAGE_BURN				2
#define OPERATION_BURN_ON_THE_FLY			3


struct TOperationContext 
{
	BOOL bErasing;
	BOOL bQuick;

	int nOperation;

	BOOL bSimulate;
	BOOL bEject;

	int nMode;
	BOOL bRaw;
	BOOL bCdRomXa;
	BOOL bCloseDisc;

	int nSpeedKB;
	BOOL bStopRequest;

	BOOL bCacheSmallFiles;
	DWORD dwSmallFilesCacheLimit;
	DWORD dwSmallFileSize;

	ImageTypeFlags::Enum imageType;
	CString strImageFile;

	struct 
	{	int  nLimits;
		BOOL bTreeDepth;
		BOOL bTranslateNames;
	} iso;

	struct 
	{
		int nLimits;
	} joliet;


	CString strBootImageFile;
	BootEmulation::Enum  bootEmulation;
	BOOL    bBootableCD;
	BOOL bLoadLastTrack;
};

struct TNotifyStruct
{
	CString strText;
	int nPercent;
	int nUsedCachePercent;
	int nActualWriteSpeed;
};

class ProcessInputTreeException: public std::exception
{
public:
	ProcessInputTreeException(){}
	ProcessInputTreeException(const std::string &msg):std::exception(msg.c_str()) {}
};

class CDataBurnerDlg : public CDialog, public DataDiscCallback
{
// Construction
public:
	CDataBurnerDlg(CWnd* pParent = NULL);	// standard constructor
	BOOL DirectoryExists();

// Dialog Data
	enum { IDD = IDD_DATABURNER_DIALOG };
	CButton	m_chkCacheSmallFiles;
	CComboBox	m_comboSpeed;
	CStatic	m_staticRequiredSpace;
	CStatic	m_staticFreeSpace;
	CEdit	m_editVolume;
	CButton	m_btnCreate;
	CEdit	m_editRootDir;
	BOOL	m_bTest;
	CString	m_strRootDir;
	CString	m_strVolume;
	CString	m_strFreeSpace;
	CString	m_strRequiredSpace;
	BOOL	m_bEject;
	BOOL	m_bCacheSmallFiles;
	DWORD	m_dwSmallFile;
	DWORD	m_dwSmallFilesCache;

	CComboBox m_comboMode;
	CButton m_chkCloseDisc;
	CButton m_chkRaw;

	CButton	m_btnErase;
	CButton	m_chkQuick;
	CComboBox	m_comboDevices;

	CButton	m_chkTest;
	CButton	m_chkEject;

	CComboBox m_comboImageType;

	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(CDataBurnerDlg)
	protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV support
	//}}AFX_VIRTUAL

// Implementation
protected:
	HICON m_hIcon;
	void SetDeviceControls();

	Engine* m_pEngine;
	DeviceEnum* m_pEnum;
	int m_nDevicesCount;

	DWORD64 m_nCapacity;
	DWORD64 m_nRequiredSpace;

	BOOL m_bRawDao;

	CStringArray m_arDeviceNames;
	CDWordArray m_arIndices;

	// Generated message map functions
	virtual BOOL OnInitDialog();
	afx_msg void OnSysCommand(UINT nID, LPARAM lParam);
	afx_msg void OnButtonBrowse();
	afx_msg void OnChangeEditRoot();
	afx_msg void OnButtonCreate();
	afx_msg void OnDestroy();
	afx_msg void OnChangeEditVolume();
	afx_msg void OnButtonBurnImage();
	afx_msg void OnButtonCreateImage();
	afx_msg void OnCheckCacheSmallFiles();
	afx_msg void OnBnClickedCheckRaw();
	afx_msg void OnCbnSelchangeComboDevices();
	afx_msg void OnCbnSelchangeComboMode();
	afx_msg void OnBnClickedButtonEjectin();
	afx_msg void OnBnClickedButtonEjectout();
	afx_msg void OnBnClickedButtonErase();
	afx_msg void OnBnClickedButtonBrowseBootImage();
	afx_msg void OnBnClickedCheckBootImage();

	DECLARE_MESSAGE_MAP()

	afx_msg LRESULT OnDeviceChange(WPARAM wParam, LPARAM lParam);

protected:
	void ___PumpMessages();

	// Erasing/writing thread...
	static DWORD WINAPI ProcessThreadProc(LPVOID pParam);
	DWORD ProcessThread();

	HANDLE m_hOperationStartedEvent;
	HANDLE m_hNotifyEvent;
	CRITICAL_SECTION m_cs;

	TOperationContext m_ctx;
	TNotifyStruct m_notify;
	HANDLE m_hThread;

	Device* m_pDevice;
	void ProcessInputTree(DWORD dwImageType, DataFile * pCurrentFile, CString & sCurrentPath);

	BOOL SetImageLayoutFromFolder(DataDisc* pDataCD, LPCTSTR fname);

	void SetVolumeProperties(DataDisc* pDataDisc, const CString& volumeLabel, primo::burner::ImageTypeFlags::Enum imageType);

	// Progress...
	CString GetTextStatus(DataDiscStatus::Enum eStatus);

	// DataDiscCallback
	void onProgress(int64_t bytesWritten, int64_t all);
	void onStatus(DataDiscStatus::Enum eStatus);
	void onFileStatus(int32_t fileNumber, const char_t* filename, int32_t percentWritten);
	bool_t onContinueWrite();

	void RunOperation(int nOperation);
	BOOL ValidateMedia(int nOperation);
	BOOL ValidateForm();

	BOOL ImageCreate(Device* pDevice, int nPrevTrackNumber);
	BOOL ImageBurn(Device* pDevice, int nPrevTrackNumber);
	BOOL BurnOnTheFly(Device* pDevice, int nPrevTrackNumber);

	int GetLastCompleteTrack(Device * pDevice);
	BootEmulation::Enum GetBootEmulationType();
	ImageTypeFlags::Enum GetImageType();
	void EnableIsoGroup(BOOL bEnable);
	void EnableJolietGroup(BOOL bEnable);
	void EnableBootGroup(BOOL bImageSupportBoot);
	
	void ShowErrorMessage(const ErrorInfo *pErrInfo);

	bool isWritePossible(Device *device) const;
	bool isRewritePossible(Device *device) const;

public:
	CButton m_radioJolietAll;
	CButton m_radioIsoAll;
	CButton m_checkIsoTreeDepth;
	CButton m_checkIsoTranslateNames;
	afx_msg void OnBnClickedRadioIsoLevel1();
	afx_msg void OnBnClickedRadioIsoLevel2();
	afx_msg void OnBnClickedRadioIsoAll();
	afx_msg void OnCbnSelchangeComboImageType();
	CStatic m_groupIsoAll;
	CStatic m_groupJolietAll;
	CButton m_chkLoadLastTrack;
	CComboBox m_comboBootEmulationType;
	CButton m_buttonBrowseBootImage;
	CEdit m_editBootImage;
	BOOL m_bBootableCD;
	CButton m_checkBootImage;
	CButton m_chkCdRomXa;
};

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

