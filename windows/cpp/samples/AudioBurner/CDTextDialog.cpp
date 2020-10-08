	
// CDTextDialog.cpp : implementation file
//

#include "stdafx.h"
#include "Burner.h"
#include "BurnerApp.h"
#include "CDTextDialog.h"

// CCDTextDialog dialog

IMPLEMENT_DYNAMIC(CCDTextDialog, CDialog)
CCDTextDialog::CCDTextDialog(CWnd* pParent /*=NULL*/)
	: CDialog(CCDTextDialog::IDD, pParent)
	, m_sTitle(_T(""))
	, m_Performer(_T(""))
	, m_sSongWriter(_T(""))
	, m_sComposer(_T(""))
	, m_sArranger(_T(""))
	, m_sMessage(_T(""))
	, m_sGenre(_T("0"))
	, m_sUpcIsrc(_T(""))
	, m_nGenreSel(0)
	, m_sDiskId(_T(""))
{

	m_bEditAlbum = TRUE;
}

CCDTextDialog::~CCDTextDialog()
{
}

void CCDTextDialog::AddGenres()
{
	m_comboGenre.AddString(_T("Not used"));
	m_comboGenre.AddString(_T("Not defined"));
	m_comboGenre.AddString(_T("Adult Contemporary"));
	m_comboGenre.AddString(_T("Alternative Rock"));
	m_comboGenre.AddString(_T("Childrens Music"));
	m_comboGenre.AddString(_T("Classical"));
	m_comboGenre.AddString(_T("Contemporary Christian"));
	m_comboGenre.AddString(_T("Country"));
	m_comboGenre.AddString(_T("Dance"));
	m_comboGenre.AddString(_T("Easy Listening"));
	m_comboGenre.AddString(_T("Erotic"));
	m_comboGenre.AddString(_T("Folk"));
	m_comboGenre.AddString(_T("Gospel"));
	m_comboGenre.AddString(_T("Hip Hop"));
	m_comboGenre.AddString(_T("Jazz"));
	m_comboGenre.AddString(_T("Latin"));
	m_comboGenre.AddString(_T("Musical"));
	m_comboGenre.AddString(_T("New Age"));
	m_comboGenre.AddString(_T("Opera"));
	m_comboGenre.AddString(_T("Operetta"));
	m_comboGenre.AddString(_T("Pop Music"));
	m_comboGenre.AddString(_T("RAP"));
	m_comboGenre.AddString(_T("Reggae"));
	m_comboGenre.AddString(_T("Rock Music"));
	m_comboGenre.AddString(_T("Rhythm & Blues"));
	m_comboGenre.AddString(_T("Sound Effects"));
	m_comboGenre.AddString(_T("Soundtrack"));
	m_comboGenre.AddString(_T("Spoken Word"));
	m_comboGenre.AddString(_T("World Music"));
	m_comboGenre.AddString(_T("Reserved"));

	// CDTG_RIAA				= 32768	/* Registration by RIAA required 32768..65535 */
}

BOOL CCDTextDialog::OnInitDialog()
{
	CDialog::OnInitDialog();
	AddGenres();

	if (0 == m_sGenre.GetLength())
		m_nGenreSel = 0;
	else
		m_nGenreSel = _ttoi(m_sGenre);

	m_comboGenre.SetCurSel(m_nGenreSel);

	m_comboGenre.EnableWindow(m_bEditAlbum);
	m_editDiskId.EnableWindow(m_bEditAlbum);

	return TRUE;  // return TRUE  unless you set the focus to a control
}


void CCDTextDialog::DoDataExchange(CDataExchange* pDX)
{
	CDialog::DoDataExchange(pDX);

	// Update combo selection
	if (!pDX->m_bSaveAndValidate)
	{
		if (0 == m_sGenre.GetLength())
			m_nGenreSel = 0;
		else
			m_nGenreSel = _ttoi(m_sGenre);
	}

	DDX_Text(pDX, IDC_EDIT_ALBUM_TITLE, m_sTitle);
	DDV_MaxChars(pDX, m_sTitle, 100);
	DDX_Text(pDX, IDC_EDIT_ALBUM_PERFORMER, m_Performer);
	DDV_MaxChars(pDX, m_Performer, 100);
	DDX_Text(pDX, IDC_EDIT_ALBUM_SONGWRITER, m_sSongWriter);
	DDV_MaxChars(pDX, m_sSongWriter, 100);
	DDX_Text(pDX, IDC_EDIT_ALBUM_COMPOSER, m_sComposer);
	DDV_MaxChars(pDX, m_sComposer, 100);
	DDX_Text(pDX, IDC_EDIT_ALBUM_ARRANGER, m_sArranger);
	DDV_MaxChars(pDX, m_sArranger, 100);
	DDX_Text(pDX, IDC_EDIT_ALBUM_MESSAGE, m_sMessage);
	DDV_MaxChars(pDX, m_sMessage, 100);
	DDX_Text(pDX, IDC_EDIT_ALBUM_UPC_ISRC, m_sUpcIsrc);
	DDV_MaxChars(pDX, m_sUpcIsrc, 14);
	DDX_CBIndex(pDX, IDC_COMBO_GENRE, m_nGenreSel);
	DDX_Control(pDX, IDC_COMBO_GENRE, m_comboGenre);

	// Update m_sGenre
	if (pDX->m_bSaveAndValidate)
		m_sGenre.Format(_T("%d"), m_nGenreSel);
	DDX_Text(pDX, IDC_EDIT_DISK_ID, m_sDiskId);
	DDX_Control(pDX, IDC_EDIT_DISK_ID, m_editDiskId);
	int selection = m_comboGenre.GetCurSel();
	if (CB_ERR != selection)
	{
		m_comboGenre.GetLBText(m_comboGenre.GetCurSel(), m_sGenreText);
	}
	else
	{
		m_sGenreText = "";
	}
}


BEGIN_MESSAGE_MAP(CCDTextDialog, CDialog)
END_MESSAGE_MAP()


void CCDTextDialog::SetCDText(const CDTextEntry &cdText)
{
	m_sTitle  = cdText.Title.c_str();
	m_Performer = cdText.Performer.c_str();
	m_sSongWriter = cdText.SongWriter.c_str();
	m_sComposer = cdText.Composer.c_str();
	m_sArranger = cdText.Arranger.c_str();
	m_sMessage = cdText.Message.c_str();
	m_sDiskId = cdText.DiskId.c_str();
	m_sGenre = cdText.Genre.c_str();
	m_sUpcIsrc = cdText.UpcIsrc.c_str();
}

void CCDTextDialog::GetCDText(CDTextEntry &cdText)
{
	cdText.Title = (LPCTSTR)m_sTitle;
	cdText.Performer = (LPCTSTR)m_Performer;
	cdText.SongWriter = (LPCTSTR)m_sSongWriter;
	cdText.Composer = (LPCTSTR)m_sComposer;
	cdText.Arranger = (LPCTSTR)m_sArranger;
	cdText.Message = (LPCTSTR)m_sMessage;
	cdText.DiskId = (LPCTSTR)m_sDiskId;
	cdText.Genre = (LPCTSTR)m_sGenre;
	cdText.GenreText = (LPCTSTR)m_sGenreText;
	cdText.UpcIsrc = (LPCTSTR)m_sUpcIsrc;
}

