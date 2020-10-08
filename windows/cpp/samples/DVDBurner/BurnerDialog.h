// This is a part of the High Performance CD Engine Library.
// Copyright (C) 2001-2003 Primo Software Corporation
// All rights reserved.
//
// This source code is only intended as a supplement to the
// High Performance CD Engine Reference and related
// electronic documentation provided with the library.
// See these sources for detailed information regarding the	
// High Performance CD Engine product.

// DVDBurnerDlg.h : header file
//
#pragma once
#include "afxwin.h"

#include "Burner.h"
#include "ProgressDialog.h"

#define THREAD_EXCEPTION (1)

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

struct BurnThreadParam : ThreadParam
{
	BurnSettings Settings;
};

struct FormatThreadParam : ThreadParam
{
	FormatSettings Settings;
};

struct EraseThreadParam : ThreadParam
{
	EraseSettings Settings;
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
	
	afx_msg void OnChangeEditRoot();
	afx_msg void OnChangeEditVolume();
	
	afx_msg void OnCbnSelchangeComboDevices();
	afx_msg void OnCbnSelchangeComboMode();

	afx_msg void OnButtonBrowseClicked();
	afx_msg void OnButtonBurnClicked();
	afx_msg void OnButtonBurnImageClicked();
	afx_msg void OnButtonCreateImageClicked();
	afx_msg void OnButtonCheckRawClicked();
	afx_msg void OnButtonEjectClicked();
	afx_msg void OnButtonCloseTrayClicked();
	afx_msg void OnButtonEraseClicked();
	afx_msg void OnButtonFormatClicked();
	afx_msg void OnCheckDvdVideoClicked();

	afx_msg LRESULT OnDeviceChange(WPARAM wParam, LPARAM lParam);
	//}}AFX_MSG
	DECLARE_MESSAGE_MAP()

// IBurnerCallback
protected:
	void OnStatus(const tstring& message);

	void OnImageProgress(DWORD64 ddwPos, DWORD64 ddwAll); 
	void OnFileProgress(int file, const tstring& fileName, int percentCompleted);

	void OnFormatProgress(double percentCompleted);
	void OnEraseProgress(double percentCompleted);

	bool OnContinue();

// BurnerDialog
protected:
	BOOL DirectoryExists();
	BOOL ValidateForm();

	void UpdateDeviceInformation();

	void UpdateProgress(ProgressInfo& notify);
	void CreateProgressWindow();
	void DestroyProgressWindow();

	void CreateImage(CString strImageFile);
	static DWORD WINAPI CreateImageThread(LPVOID pParam);

	void BurnImage(CString strImageFile);
	static DWORD WINAPI BurnImageThread(LPVOID pParam);

	void Burn();
	static DWORD WINAPI BurnThread(LPVOID pParam);

	void Erase();
	static DWORD WINAPI EraseThread(LPVOID pParam);

	void Format();
	static DWORD WINAPI FormatThread(LPVOID pParam);

	void WaitForThreadToFinish();
	void ProcessMessages();

	void ShowErrorMessage(BurnerException& burnerException);

protected:
	CDWordArray m_DeviceIndexArray;

	DWORD64 m_ddwCapacity;
	DWORD64 m_ddwRequiredSpace;

// MFC controls
protected:
	CStatic		m_staticMediaType;
	CComboBox	m_comboSpeed;
	CStatic		m_staticRequiredSpace;
	CStatic		m_staticFreeSpace;
	CEdit		m_editVolume;
	CButton		m_btnCreate;
	CEdit		m_editRootDir;

	CComboBox	m_comboWriteMethod;
	CButton		m_chkCloseDisc;

	CComboBox	m_comboDevices;

	CButton		m_chkTest;
	CButton		m_chkEject;

	CComboBox	m_comboImageType;

	BOOL		m_bTest;
	BOOL		m_bEject;

	CString		m_strRootDir;
	CString		m_strVolume;
	CString		m_strFreeSpace;
	CString		m_strRequiredSpace;

	CButton		m_chkQuickErase;
	CButton		m_chkQuickFormat;
	CButton		m_chkLoadLastTrack;
	CButton		m_chkVideoDVD;

private:
	HANDLE m_hCommandThreadStartedEvent;
	HANDLE m_hCommandThread;

	CProgressDialog m_ProgressWindow;
	ProgressInfo m_ProgressInfo;

	CRITICAL_SECTION m_csProgressUpdateGuard;
	HANDLE m_hProgressAvailableEvent;

	BurnerException m_ThreadException;
	Burner m_Burner;
};

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

