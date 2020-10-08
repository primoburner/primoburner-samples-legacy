#pragma once

#include "afxwin.h"
#include "BurnerSettings.h"

class CCDTextDialog : public CDialog
{
	DECLARE_DYNAMIC(CCDTextDialog)

public:
	CCDTextDialog(CWnd* pParent = NULL);   // standard constructor
	virtual ~CCDTextDialog();

// Dialog Data
	enum { IDD = IDD_CDTEXT_DIALOG };

protected:
	virtual BOOL OnInitDialog();
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support
	DECLARE_MESSAGE_MAP()


	CString m_sTitle;
	CString m_Performer;
	CString m_sSongWriter;
	CString m_sComposer;
	CString m_sArranger;
	CString m_sMessage;
	CString m_sGenre;
	CString m_sGenreText;
	CString m_sUpcIsrc;
	CString m_sDiskId;

	CComboBox m_comboGenre;
	int m_nGenreSel;

	
public:
	BOOL m_bEditAlbum;
	void SetCDText(const CDTextEntry &cdText);
	void GetCDText(CDTextEntry &cdText);

protected:
	void AddGenres();
public:
	CEdit m_editDiskId;
};
