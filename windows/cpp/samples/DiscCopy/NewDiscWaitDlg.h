#pragma once
#include "stdafx.h"
#include "BurnerApp.h"
#include "Burner.h"

// CNewDiscWaitDlg dialog

class CNewDiscWaitDlg : public CDialog
{
	DECLARE_DYNAMIC(CNewDiscWaitDlg)

public:
	CNewDiscWaitDlg(CWnd* pParent = NULL);   // standard constructor
	BOOL Create(CWnd* pParentWnd=NULL);
	virtual ~CNewDiscWaitDlg();

// Dialog Data
	enum { IDD = IDD_NEW_DISC_DIALOG };

protected:
	virtual BOOL OnInitDialog();
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	DECLARE_MESSAGE_MAP()
public:
	afx_msg LRESULT OnDeviceChange(WPARAM wParam, LPARAM lParam);
	afx_msg void OnBnClickedCancelwait();
	afx_msg void OnBnClickedContinue();

	void SetBurner(Burner &pDevice);

	const BOOL get_QuickClean() const;
	const ECleanMethod get_SelectedCleanMethod() const;
private:
	void SetDeviceControls();
private:
	CButton m_chkQuick;

	Burner *m_pBurner;
public:
	CButton m_btnContinue;

protected:
	BOOL m_bQuick;
	int m_nSelectedCleanMethod;
	ECleanMethod m_SelectedCleanMethod;
};
