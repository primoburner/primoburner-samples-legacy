// This is a part of the High Performance CD Engine Library.
// Copyright (C) 2001-2003 Primo Software Corporation
// All rights reserved.
//
// This source code is only intended as a supplement to the
// High Performance CD Engine Reference and related
// electronic documentation provided with the library.
// See these sources for detailed information regarding the	
// High Performance CD Engine product.

// DiscCopyDlg.h : header file
//
#pragma once
#include "afxwin.h"

#include "Burner.h"
#include "ProgressDialog.h"

#define THREAD_EXCEPTION (1)
#define OPERATION_FAILED (2)
#define DISCCOPY_ONNEWDISCWAIT_CANCEL (3)
#define DISCCOPY_MEDIA_CLEAN_CANCEL (4)

class CBurnerDialog;
struct ProgressInfo
{
	CString Message;
	int Percent;
	int UsedCachePercent;
	int ActualWriteSpeed;
};

// Base for all thread parameters structures
struct ThreadParam
{
	CBurnerDialog* pThis;
	ThreadParam() : pThis(NULL)
	{
	}
};

struct CreateImageThreadParam : ThreadParam
{
	CreateImageSettings Settings;
};

struct BurnImageThreadParam : ThreadParam
{
	BurnImageSettings Settings;
};
struct DirectCopyThreadParam : ThreadParam
{
	DirectCopySettings Settings;
};

struct CleanMediaThreadParam : ThreadParam
{
	CleanMediaSettings Settings;
};

/////////////////////////////////////////////////////////////////////////////
// BurnerDialog dialog
class CBurnerDialog : public CDialog, public IBurnerCallback
{
// Construction
public:
	CBurnerDialog(CWnd* pParent = NULL);	// standard constructor

// Dialog Data
	//{{AFX_DATA(CBurnerDialog)
	enum { IDD = IDD_BURNER_DIALOG };
	//}}AFX_DATA
	
// Overrides
	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(CBurnerDialog)
	protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV support
	//}}AFX_VIRTUAL

// Implementation
protected:
	// Generated message map functions
	//{{AFX_MSG(CBurnerDialog)
	virtual BOOL OnInitDialog();
	afx_msg void OnDestroy();
	
	afx_msg void OnCbnSelectionChangeSrcDevices();

	afx_msg void OnCbnSelectionChangeDstDevices();

	afx_msg void OnButtonBrowseClicked();

	afx_msg LRESULT OnDeviceChange(WPARAM wParam, LPARAM lParam);

	afx_msg void OnButtonCopyClicked();

	afx_msg void OnBnClickedRadioCopyMode();

	//}}AFX_MSG
	DECLARE_MESSAGE_MAP()

// IBurnerCallback
protected:
	void OnStatus(const tstring& message);

	void OnCopyProgress(DWORD64 ddwPos, DWORD64 ddwAll); 
	void OnTrackStatus(int nTrack, int nPercent);

	void OnFormatProgress(double percentCompleted);
	void OnEraseProgress(double percentCompleted);

	bool OnContinue();

// BurnerDialog
protected:
	BOOL ValidateForm();

	void UpdateDeviceInformation();

	void UpdateProgress(ProgressInfo& notify);
	void CreateProgressWindow();
	void DestroyProgressWindow();

	DWORD CreateImage();
	static DWORD WINAPI CreateImageThread(LPVOID pParam);

	DWORD BurnImage();
	static DWORD WINAPI BurnImageThread(LPVOID pParam);

	DWORD CleanMedia(const CleanMediaSettings& settings);
	static DWORD WINAPI CleanMediaThread(LPVOID pParam);

	void RunSimpleCopy();
	void RunDirectCopy();
	static DWORD WINAPI DirectCopyThread(LPVOID pParam);

	DWORD WaitForThreadToFinish();
	void ProcessMessages();

	void ShowErrorMessage(BurnerException& burnerException);
	CDCopyWriteMethod::Enum GetWriteMethod();

	BOOL DirectoryExists();
	DWORD PrepareNewMedium();
	int GetSelectedDeviceIndex();
	int GetSelectedDeviceIndex(bool dstDevice);

// MFC controls
protected:
	CComboBox			m_comboSrcDevices;
	CComboBox			m_comboDstDevices;
	CComboBox			m_cmbWriteMethods;
	CEdit				m_editRootDir;
	CButton				m_btnCopy;
	CButton				m_chkReadSubChannel;
	CButton				m_chkUseTemporaryFiles;
	CButton				m_btnBrowse;

private:
	HANDLE				m_hCommandThreadStartedEvent;
	HANDLE				m_hCommandThread;

	CProgressDialog		m_ProgressWindow;
	ProgressInfo		m_ProgressInfo;

	CRITICAL_SECTION	m_csProgressUpdateGuard;
	HANDLE				m_hProgressAvailableEvent;

	BurnerException		m_ThreadException;
	Burner				m_Burner;

	CString				m_strImagePath;
	BOOL				m_bInProcess;
	int					m_nSelectedCopyMode;
};

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

