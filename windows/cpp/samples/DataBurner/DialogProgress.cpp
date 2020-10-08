// This is a part of the High Performance CD Engine Library.
// Copyright (C) 2001-2003 Primo Software Corporation
// All rights reserved.
//
// This source code is only intended as a supplement to the
// High Performance CD Engine Reference and related
// electronic documentation provided with the library.
// See these sources for detailed information regarding the	
// High Performance CD Engine product.

// DialogProgress.cpp : implementation file
//

#include "stdafx.h"
#include "DataBurner.h"
#include "DialogProgress.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CDialogProgress dialog


CDialogProgress::CDialogProgress(CWnd* pParent /*=NULL*/)
	: CDialog(CDialogProgress::IDD, pParent)
{
	//{{AFX_DATA_INIT(CDialogProgress)
		// NOTE: the ClassWizard will add member initialization here
	//}}AFX_DATA_INIT
}


void CDialogProgress::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	//{{AFX_DATA_MAP(CDialogProgress)
	DDX_Control(pDX, IDC_STATIC_STATUS, m_staticStatus);
	DDX_Control(pDX, IDC_PROGRESS_BAR, m_ctrlProgress);
	//}}AFX_DATA_MAP
	DDX_Control(pDX, IDC_PROGRESS_INTERNAL_BUFFER, m_ctrlInternalBuffer);
	DDX_Control(pDX, IDC_STATIC_STATUS2, m_ctrlActualWriteSpeed);
}


BEGIN_MESSAGE_MAP(CDialogProgress, CDialog)
	//{{AFX_MSG_MAP(CDialogProgress)
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()


BOOL CDialogProgress::Create(CWnd* pParentWnd/*=NULL*/) {
	return CDialog::Create(CDialogProgress::IDD, pParentWnd);
}

/////////////////////////////////////////////////////////////////////////////
// CDialogProgress message handlers



BOOL CDialogProgress::OnInitDialog() 
{
	CDialog::OnInitDialog();
	m_ctrlProgress.SetRange(0, 100);
	m_ctrlInternalBuffer.SetRange(0, 100);
	m_bStopped=FALSE;

	return TRUE;  // return TRUE unless you set the focus to a control
	              // EXCEPTION: OCX Property Pages should return FALSE
}


void CDialogProgress::SetStatus(CString strStatus) 
{
	m_staticStatus.SetWindowText(strStatus);
}

void CDialogProgress::SetProgress(int nPos) 
{
	m_ctrlProgress.SetPos(nPos);
}

void CDialogProgress::SetInternalBuffer(int nPos) 
{
	m_ctrlInternalBuffer.SetPos(nPos);

	CString sText;
	sText.Format(_T("%d%%"), nPos);
	m_ctrlInternalBuffer.SetWindowText(sText);
}

void CDialogProgress::SetActualWriteSpeed(int nSpeed)
{
	CString sText;
	sText.Format(_T("%d KB/s (CD: %.02fx DVD: %.02fx)"), nSpeed, (float)nSpeed / (176400 / 1024.0), (float)nSpeed / 1350.0);
	m_ctrlActualWriteSpeed.SetWindowText(sText);
}

void CDialogProgress::OnOK() 
{
	m_bStopped=TRUE;	
}

void CDialogProgress::OnCancel() 
{
}

