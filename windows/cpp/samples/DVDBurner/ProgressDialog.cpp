#include "stdafx.h"
#include "BurnerApp.h"
#include "ProgressDialog.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#undef THIS_FILE
static char THIS_FILE[] = __FILE__;
#endif

/////////////////////////////////////////////////////////////////////////////
// CProgressDialog dialog

CProgressDialog::CProgressDialog(CWnd* pParent /*=NULL*/)
	: CDialog(CProgressDialog::IDD, pParent)
{
	//{{AFX_DATA_INIT(CProgressDialog)
		// NOTE: the ClassWizard will add member initialization here
	//}}AFX_DATA_INIT
}


void CProgressDialog::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	//{{AFX_DATA_MAP(CProgressDialog)
	DDX_Control(pDX, IDC_STATIC_STATUS, m_staticStatus);
	DDX_Control(pDX, IDC_STATIC_STATUS2, m_ctrlActualWriteSpeed);
	DDX_Control(pDX, IDC_PROGRESS_BAR, m_ctrlProgress);
	DDX_Control(pDX, IDC_PROGRESS_INTERNAL_BUFFER, m_ctrlInternalBuffer);
	//}}AFX_DATA_MAP
}


BEGIN_MESSAGE_MAP(CProgressDialog, CDialog)
	//{{AFX_MSG_MAP(CProgressDialog)
	//}}AFX_MSG_MAP
END_MESSAGE_MAP()


BOOL CProgressDialog::Create(CWnd* pParentWnd/*=NULL*/) 
{
	return CDialog::Create(CProgressDialog::IDD, pParentWnd);
}

/////////////////////////////////////////////////////////////////////////////
// CProgressDialog message handlers

BOOL CProgressDialog::OnInitDialog() 
{
	CDialog::OnInitDialog();

	m_ctrlProgress.SetRange(0, 100);
	m_ctrlInternalBuffer.SetRange(0, 100);
	m_bStopped = FALSE;

	return TRUE;  // return TRUE unless you set the focus to a control
	              // EXCEPTION: OCX Property Pages should return FALSE
}


void CProgressDialog::SetStatus(CString strStatus) 
{
	m_staticStatus.SetWindowText(strStatus);
}

void CProgressDialog::SetProgress(int nPos) 
{
	m_ctrlProgress.SetPos(nPos);
}

void CProgressDialog::SetInternalBuffer(int nPos) 
{
	m_ctrlInternalBuffer.SetPos(nPos);

	CString sText;
	sText.Format(_TEXT("%d%%"), nPos);
	m_ctrlInternalBuffer.SetWindowText(sText);
}

void CProgressDialog::SetActualWriteSpeed(int nSpeed)
{
	CString sText;
	sText.Format(_TEXT("%d KB/s (CD: %.02fx DVD: %.02fx)"), nSpeed, (float)nSpeed / 175.0, (float)nSpeed / 1350.0);
	m_ctrlActualWriteSpeed.SetWindowText(sText);
}

void CProgressDialog::OnOK() 
{
	m_bStopped = TRUE;	
}

void CProgressDialog::OnCancel() 
{
}

