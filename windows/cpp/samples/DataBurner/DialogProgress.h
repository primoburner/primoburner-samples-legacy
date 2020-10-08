// This is a part of the High Performance CD Engine Library.
// Copyright (C) 2001-2003 Primo Software Corporation
// All rights reserved.
//
// This source code is only intended as a supplement to the
// High Performance CD Engine Reference and related
// electronic documentation provided with the library.
// See these sources for detailed information regarding the	
// High Performance CD Engine product.

// DialogProgress.h : header file
//

#pragma once

#include "afxcmn.h"
#include "afxwin.h"


/////////////////////////////////////////////////////////////////////////////
// CDialogProgress dialog

class CDialogProgress : public CDialog
{
// Construction
public:
	CDialogProgress(CWnd* pParent = NULL);   // standard constructor

// Dialog Data
	//{{AFX_DATA(CDialogProgress)
	enum { IDD = IDD_DIALOG_BURNING };
	CStatic	m_staticStatus;
	CProgressCtrl	m_ctrlProgress;
	//}}AFX_DATA


// Overrides
	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(CDialogProgress)
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
	//{{AFX_MSG(CDialogProgress)
	virtual BOOL OnInitDialog();
	virtual void OnOK();
	//}}AFX_MSG
	virtual void OnCancel();
	DECLARE_MESSAGE_MAP()
public:
	CProgressCtrl m_ctrlInternalBuffer;
	CStatic m_ctrlActualWriteSpeed;
};

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

