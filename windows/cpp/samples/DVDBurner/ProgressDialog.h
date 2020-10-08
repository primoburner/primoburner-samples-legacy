#pragma once

#include "afxcmn.h"
#include "afxwin.h"

/////////////////////////////////////////////////////////////////////////////
// CProgressDialog dialog

class CProgressDialog : public CDialog
{
// Construction
public:
	CProgressDialog(CWnd* pParent = NULL);   // standard constructor

// Dialog Data
	//{{AFX_DATA(CProgressDialog)
	enum { IDD = IDD_PROGRESS_DIALOG };
	CStatic	m_staticStatus;
	CStatic m_ctrlActualWriteSpeed;
	CProgressCtrl	m_ctrlProgress;
	CProgressCtrl m_ctrlInternalBuffer;
	//}}AFX_DATA


// Overrides
	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(CProgressDialog)
	protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	//}}AFX_VIRTUAL

public:
	BOOL Create(CWnd* pParentWnd=NULL);
	void SetStatus(CString strStatus);
	void SetProgress(int nPos);
	void SetInternalBuffer(int nPos);
	void SetActualWriteSpeed(int nSpeed);

	BOOL m_bStopped;

// Implementation
protected:
	// Generated message map functions
	//{{AFX_MSG(CProgressDialog)
	virtual BOOL OnInitDialog();
	virtual void OnOK();
	virtual void OnCancel();
	//}}AFX_MSG
	DECLARE_MESSAGE_MAP()
public:
};

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

