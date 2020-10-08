// BurnerDialog.h : header file

#pragma once

#include "Burner.h"
#include "ProgressDialog.h"


//////////////////////////////////
// CBurnerDialog dialog

#define THREAD_EXCEPTION (1)

struct ProgressInfo
{
	CString Message;
	int Percent;
	int UsedCachePercent;
	int ActualWriteSpeed;
};

class CBurnerDialog;


// Base for all thread parameters structures
struct ThreadParam
{
	CBurnerDialog* pThis;
	ThreadParam() : pThis(NULL)
	{
	}
};

struct BurnThreadParam : ThreadParam
{
	BurnSettings Settings;
};

struct EraseThreadParam : ThreadParam
{
	EraseSettings Settings;
};

class CBurnerDialog: public CDialog, public AudioCDCallback
{
	struct FileEntry
	{
		tstring		FilePath;
		CDTextEntry CDText;
	};

	typedef stl::vector<FileEntry> FileEntryVect;

// Construction
public:
	CBurnerDialog(CWnd* pParent = NULL);	// standard constructor
	~CBurnerDialog();

// Dialog Data
	enum { IDD = IDD_AUDIOBURNER_DIALOG };
	CButton		m_btnErase;
	CButton		m_chkQuickErase;
	CStatic		m_staticFreeSpace;
	CListBox	m_listPlaylist;
	CComboBox	m_comboSpeed;
	CComboBox	m_comboDevices;
	CButton		m_chkTest;
	CButton		m_chkEject;
	CButton		m_chkCDText;
	CButton		m_chkHiddenTrack;
	CButton		m_chkCloseDisc;
	CButton		m_chkDecodeTempFile;
	CButton		m_chkUseAudioStream;

	CButton		m_btnStart;
	CComboBox   m_comboMode;
	CButton		m_btnCDTextTrack;
	CButton		m_btnCDTextAlbum;
	CButton		m_btnAddFiles;
	CButton		m_btnRemoveFiles;

	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(CBurnerDialog)
	protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV support
	//}}AFX_VIRTUAL

// Implementation
protected:
	HICON m_hIcon;

	// Generated message map functions
	//{{AFX_MSG(CBurnerDialog)
	virtual BOOL OnInitDialog();
	afx_msg void OnSysCommand(UINT nID, LPARAM lParam);
	afx_msg void OnDropFiles(HDROP hDropInfo);
	afx_msg int OnVKeyToItem(UINT nKey, CListBox* pListBox, UINT nIndex);
	afx_msg void OnDestroy();
	afx_msg void OnButtonStart();
	afx_msg void OnButtonEjectin();
	afx_msg void OnButtonEjectout();
	afx_msg void OnButtonErase();
	afx_msg void OnCbnSelchangeComboDevices();
	//}}AFX_MSG

	afx_msg LRESULT OnDeviceChange(WPARAM wParam, LPARAM lParam);
	afx_msg void OnBnClickedTrackCdtext();
	afx_msg void OnBnClickedAlbumCdtext();
	afx_msg void OnButtonAddFiles();
	afx_msg void OnButtonRemoveFiles();

	DECLARE_MESSAGE_MAP()

protected:
	Burner m_burner;

	afx_msg void OnBnClickedCheckCdtext();

	void ShowErrorMessage(BurnerException& burnerException);
	void UpdateDeviceInformation();

	IntVect			m_deviceIndexArray;

	CCriticalSection m_csProgressUpdateGuard;
	CEvent		     m_eProgressAvailable;
	CWinThread		*m_pWorkThread;
	CEvent		     m_eCommandThreadStarted;

	CProgressDialog m_ProgressWindow;
	ProgressInfo	m_ProgressInfo;

	BurnerException m_ThreadException;
	
	void WaitForThreadToFinish();
	void DestroyProgressWindow();
	void CreateProgressWindow();
	void UpdateProgress(ProgressInfo& progressInfo);
	void ProcessMessages();

	void Burn();
	static UINT BurnThread(LPVOID pVoid);

	void Erase();
	static UINT EraseThread(LPVOID pVoid);

	CDTextEntry		m_AlbumCDText;
	FileEntryVect	m_Files;

	void AddFileToPlayList(LPCTSTR file);
	void RemoveFilesFromPlayList(CListBox* pListBox);

public:
	// AudioCDCallback
	virtual void onWriteProgress(uint32_t current, uint32_t all);
	virtual void onWriteStatus(AudioCDStatus::Enum status);
	virtual void onWriteTrack(int32_t trackIndex, int32_t percentWritten);
	virtual bool_t onContinueWrite();
};
