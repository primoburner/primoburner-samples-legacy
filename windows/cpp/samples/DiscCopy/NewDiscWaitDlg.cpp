// NewDiscWaitDlg.cpp : implementation file
//

#include "stdafx.h"
#include "NewDiscWaitDlg.h"

// CNewDiscWaitDlg dialog

IMPLEMENT_DYNAMIC(CNewDiscWaitDlg, CDialog)

CNewDiscWaitDlg::CNewDiscWaitDlg(CWnd* pParent /*=NULL*/)
	: CDialog(CNewDiscWaitDlg::IDD, pParent)
	, m_nSelectedCleanMethod(0)
{

}

CNewDiscWaitDlg::~CNewDiscWaitDlg()
{
}

void CNewDiscWaitDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);
	DDX_Control(pDX, IDC_QUICKERASEFORMAT, m_chkQuick);
	DDX_Control(pDX, IDC_PROCEED, m_btnContinue);
	DDX_Radio(pDX, IDC_ERASE,m_nSelectedCleanMethod);
	DDX_Check(pDX, IDC_QUICKERASEFORMAT, m_bQuick);
}


BEGIN_MESSAGE_MAP(CNewDiscWaitDlg, CDialog)
	ON_MESSAGE( WM_DEVICECHANGE, OnDeviceChange)
	ON_BN_CLICKED(IDC_CANCELWAIT, &CNewDiscWaitDlg::OnBnClickedCancelwait)
	ON_BN_CLICKED(IDC_PROCEED, &CNewDiscWaitDlg::OnBnClickedContinue)
END_MESSAGE_MAP()

BOOL CNewDiscWaitDlg::OnInitDialog()
{
	//this->WindowProc
	CDialog::OnInitDialog();
	SetDeviceControls();

	return TRUE;
}

BOOL CNewDiscWaitDlg::Create(CWnd* pParentWnd/*=NULL*/)
{
	return CDialog::Create(CNewDiscWaitDlg::IDD, pParentWnd);
}

// CNewDiscWaitDlg message handlers

void CNewDiscWaitDlg::OnBnClickedCancelwait()
{
	CDialog::EndDialog(0);
}

void CNewDiscWaitDlg::OnBnClickedContinue()
{
	UpdateData();

	switch(m_nSelectedCleanMethod)
	{
		case 0:
			m_SelectedCleanMethod = CM_Erase;
			break;
		case 1:
			m_SelectedCleanMethod = CM_Format;
			break;
		default:
			m_SelectedCleanMethod = CM_None;
			break;
	}
	// DiscCopy returns DiscCopyError.IncompatibleMedia result when trying to burn an image created from
	// DVD-RW RO to DVD-RW Seq or vice versa. It is possible to convert RO to Seq as well as Seq to RO.
	// All that needs to be done is use the correct Device method - use Erase to convert RO to Seq and
	// Format to convert Seq to RO
	if (NULL != m_pBurner)
	{
		if (MediaProfile::DVDMinusRWRO == m_pBurner->get_OriginalMediaProfile() &&
			MediaProfile::DVDMinusRWSeq == m_pBurner->get_SrcMediaProfile() &&
			CM_Erase == m_SelectedCleanMethod)
		{
			if (IDCANCEL != AfxMessageBox(_TEXT("To use the medium currently inserted in the device it must be formatted. Do you want to continue and format the medium?"), MB_OKCANCEL))
			{
				// quick format should be enough
				m_bQuick = true;
				m_SelectedCleanMethod = CM_Format;
			}
			else
			{
				return;
			}
		}
		else if (MediaProfile::DVDMinusRWSeq == m_pBurner->get_OriginalMediaProfile() &&
			MediaProfile::DVDMinusRWRO == m_pBurner->get_SrcMediaProfile())
		{
			if (CM_Format == m_SelectedCleanMethod)
			{
				if (IDCANCEL != AfxMessageBox(_TEXT("To use the medium currently inserted in the device it must be fully erased. Do you want to continue and erase the medium?"), MB_OKCANCEL))
				{
					// full erase is needed
					m_bQuick = false;
					m_SelectedCleanMethod = CM_Erase;
				}
				else
				{
					return;
				}
			}
			else
			{
				// full erase is needed
				m_bQuick = false;
			}
		}
	}
	else
	{
		m_SelectedCleanMethod = CM_None;
		m_bQuick = false;
	}

	CDialog::EndDialog(1);	
}

LRESULT CNewDiscWaitDlg::OnDeviceChange(WPARAM wParam, LPARAM lParam)
{
	SetDeviceControls();
	return 0;
}

void CNewDiscWaitDlg::SetBurner(Burner &burner)
{
	m_pBurner = &burner;
}
void CNewDiscWaitDlg::SetDeviceControls()
{
	BOOL bUsableMediumPresent = FALSE;
	BOOL bEnableErase = FALSE;
	BOOL bEnableFormat = FALSE;
	BOOL bQuickEnabled = TRUE;
	if (NULL != m_pBurner)
	{
		m_pBurner->RefreshSrcDevice();
		if (m_pBurner->get_MediaIsValidProfile())
		{
			MediaProfile::Enum originalProfile = m_pBurner->get_OriginalMediaProfile();
			MediaProfile::Enum profile = m_pBurner->get_SrcMediaProfile();
			// Note: A BD-R SRM disc may be formatted to BD-R SRM+POW using IDevice::FormatBD(EBDFormatType::BDFT_BD_FULL, EBDFormatSubType::BDFST_BD_R_SRM_POW)
			if (m_pBurner->get_MediaIsRewritable() ||
				(MediaProfile::BDRSrmPow == originalProfile && MediaProfile::BDRSrm == profile))
			{
				if (m_pBurner->get_MediaIsCD())
				{
					bEnableErase = TRUE;
					bEnableFormat = FALSE;
					m_nSelectedCleanMethod = 0;
				}
				else if (m_pBurner->get_MediaIsDVD())
				{
					if (profile == MediaProfile::DVDMinusRWRO ||
						profile == MediaProfile::DVDMinusRWSeq)
					{
						bEnableErase = TRUE;
						bEnableFormat = TRUE;
					}
					else if (profile == MediaProfile::DVDPlusRW ||
						profile == MediaProfile::DVDRam)
					{
						bEnableErase = FALSE;
						bEnableFormat = TRUE;
						m_nSelectedCleanMethod = 1;
					}

				}
				else if (m_pBurner->get_MediaIsBD())
				{
					if (profile == MediaProfile::BDRE ||
						(profile == MediaProfile::BDRSrm && originalProfile == MediaProfile::BDRSrmPow))
					{
						bEnableErase = FALSE;
						bEnableFormat = TRUE;
						bQuickEnabled = FALSE;
						m_nSelectedCleanMethod = 1;
					}
				}
				bUsableMediumPresent = TRUE;
			}
			else if (m_pBurner->get_MediaIsBlank())
			{
				bUsableMediumPresent = TRUE;
			}
		}
	}
	m_btnContinue.EnableWindow(bUsableMediumPresent);
	m_chkQuick.EnableWindow((bEnableErase || bEnableFormat) && bQuickEnabled);
	GetDlgItem(IDC_ERASE)->EnableWindow(bEnableErase);
	GetDlgItem(IDC_FORMAT)->EnableWindow(bEnableFormat);

	if (!bEnableErase && !bEnableFormat)
	{
		m_nSelectedCleanMethod = -1;
	}
	else if (-1 == m_nSelectedCleanMethod)
	{
		m_nSelectedCleanMethod = 0;
	}
	UpdateData(FALSE);

}

const BOOL CNewDiscWaitDlg::get_QuickClean() const
{
	return m_bQuick;
}
const ECleanMethod CNewDiscWaitDlg::get_SelectedCleanMethod() const
{
	return m_SelectedCleanMethod;
}