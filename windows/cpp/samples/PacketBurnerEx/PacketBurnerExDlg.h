// This is a part of the High Performance CD Engine Library.
// Copyright (C) 2001-2003 Primo Software Corporation
// All rights reserved.
//
// This source code is only intended as a supplement to the
// High Performance CD Engine Reference and related
// electronic documentation provided with the library.
// See these sources for detailed information regarding the	
// High Performance CD Engine product.

// PacketBurnerDlg.h : header file
//

#pragma once
#include "afxwin.h"
#include <exception>
#include <string>


/////////////////////////////////////////////////////////////////////////////
// CPacketBurnerExDlg dialog

enum EAction
{
	ACTION_NONE	   = 0,	
	ACTION_BURN_START,
	ACTION_BURN_APPEND,
	ACTION_BURN_FINALIZE,
	ACTION_ERASE,
	ACTION_FORMAT,
};

struct TProcessContext 
{
	EAction eAction;		//current action
	BOOL bQuick;			// indicates quick format or erase

	BOOL bEject;			// eject device after burning
	int nSpeedKB;				// burning speed
	BOOL bStopRequest;		// indicates whether user pressed the stop button on the progress dialog
};

struct TNotifyStruct
{
	CString strText;
	int nPercent;
	int nUsedCachePercent;
};

class ProcessInputTreeException: public std::exception
{
public:
	ProcessInputTreeException(){}
	ProcessInputTreeException(const std::string &msg):std::exception(msg.c_str()) {}
};


class CPacketBurnerExDlg : public CDialog, public DataDiscCallback, public DeviceCallback
{
// Construction
public:
	CPacketBurnerExDlg(CWnd* pParent = NULL);	// standard constructor

// Dialog Data
	enum { IDD = IDD_DATABURNER_DIALOG };

	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(CPacketBurnerExDlg)
	protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV support
	//}}AFX_VIRTUAL

// DataDiscCallback
public:
	void onProgress(int64_t bytesWritten, int64_t all);
	void onStatus(DataDiscStatus::Enum eStatus);
	void onFileStatus(int32_t fileNumber, const char_t* filename, int32_t percentWritten);
	BOOL onContinueWrite();

// DeviceCallback
public:
	void onFormatProgress(double fPercentCompleted);
	void onEraseProgress(double fPercentCompleted);


// Implementation
protected:
	static LPCTSTR GetProfileDescription(WORD wProfile, Device* pDevice);
	void SetDeviceControls();
	BOOL DirectoryExists();

protected:
	// Generated message map functions
	virtual BOOL OnInitDialog();

	afx_msg void OnSysCommand(UINT nID, LPARAM lParam);
	afx_msg void OnDestroy();

	afx_msg void OnButtonBrowse();
	afx_msg void OnChangeEditRoot();

	afx_msg void OnButtonStart();
	afx_msg void OnButtonAppend();
	afx_msg void OnButtonFinalize();

	afx_msg LRESULT OnDeviceChange(WPARAM wParam, LPARAM lParam);
	
	afx_msg void OnCbnSelchangeComboDevices();
	afx_msg void OnBnClickedButtonCloseTray();
	afx_msg void OnBnClickedButtonEject();

	afx_msg void OnBnClickedButtonErase();
	afx_msg void OnBnClickedButtonFormat();

	DECLARE_MESSAGE_MAP()

protected:
	void ___PumpMessages();

	// Erasing/writing thread...
	static DWORD WINAPI ProcessThreadProc(LPVOID pParam);
	DWORD ProcessThread();

	void ProcessInputTree(DataFile * pCurrentFile, CString & sCurrentPath);

	BOOL SetImageLayoutFromFolder(DataDisc* pDataCD, LPCTSTR fname);
	BOOL SetEmptyImageLayout(DataDisc* pDataCD);

	void StartProcess(EAction eAction);
	BOOL ValidateForm();

	void SetParameters(DataDisc* pDataCD, BOOL bCloseSessionAndDisk);
	int GetLastTrack(Device * pDevice);

	BOOL Burn(Device* pDevice, BOOL fFinalize, BOOL fLoadLastTrack);
	BOOL Erase(Device* pDevice);
	BOOL Format(Device* pDevice);

	void ShowErrorMessage(const ErrorInfo *pErrInfo);

	// Progress...
	CString GetTextStatus(DataDiscStatus::Enum eStatus);

	bool isWritePossible(Device *device) const;

protected:
	HANDLE m_hThread;
	HANDLE m_hProcessStartedEvent;
	HANDLE m_hNotifyEvent;
	CRITICAL_SECTION m_cs;

	TProcessContext m_ctx;
	TNotifyStruct m_notify;

	Device* m_pDevice;

	MediaProfile::Enum m_MediaProfile; 

protected:
	Engine*			m_pEngine;
	DeviceEnum*		m_pDeviceEnum;
	int				m_nDeviceCount;

	DWORD64 m_ddwCapacity;
	DWORD64 m_ddwRequiredSpace;

	CStringArray m_arDeviceNames;
	CDWordArray m_arIndices;

// MFC Controls
protected:
	CComboBox	m_comboSpeed;
	CStatic		m_staticRequiredSpace;
	CStatic		m_staticFreeSpace;
	CEdit		m_editRootDir;
	CString		m_strRootDir;
	CString		m_strFreeSpace;
	CString		m_strRequiredSpace;
	BOOL		m_bEject;

	CButton		m_chkQuickErase;
	CButton		m_chkQuickFormat;

	CComboBox	m_comboDevices;

	CButton		m_chkEject;
	
	CButton		m_btnStart;
	CButton		m_btnAppend;
	CButton		m_btnFinalize;

	CStatic		m_staticMediaType;

	CButton		m_chkLoadLastTrack;
};

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.
