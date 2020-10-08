#include "stdafx.h"

#include "Burner.h"
#include "BurnerApp.h"
#include "BurnerDialog.h"

BEGIN_MESSAGE_MAP(CBurnerApp, CWinApp)
	//{{AFX_MSG_MAP(CBurnerApp)
		// NOTE - the ClassWizard will add and remove mapping macros here.
		//    DO NOT EDIT what you see in these blocks of generated code!
	//}}AFX_MSG
	ON_COMMAND(ID_HELP, CWinApp::OnHelp)
END_MESSAGE_MAP()

/////////////////////////////////////////////////////////////////////////////
// CBurnerApp construction

CBurnerApp::CBurnerApp()
{
	// add construction code here,
	// Place all significant initialization in InitInstance
}

/////////////////////////////////////////////////////////////////////////////
// The one and only CBurnerApp object

CBurnerApp theApp;

/////////////////////////////////////////////////////////////////////////////
// CBurnerApp initialization

BOOL CBurnerApp::InitInstance()
{
	INITCOMMONCONTROLSEX InitCtrls;
	InitCtrls.dwSize = sizeof(InitCtrls);
	InitCtrls.dwICC = ICC_WIN95_CLASSES; // include all the common control classes
	InitCommonControlsEx(&InitCtrls);

	CoInitialize(NULL);

	// Standard initialization
	// If you are not using these features and wish to reduce the size
	//  of your final executable, you should remove from the following
	//  the specific initialization routines you do not need.

	CBurnerDialog dlg;
	m_pMainWnd = &dlg;

	INT32 nResponse = (INT32)dlg.DoModal();
	if (nResponse == IDOK)
	{
		// Place code here to handle when the dialog is
		//  dismissed with OK
	}
	else if (nResponse == IDCANCEL)
	{
		// Place code here to handle when the dialog is
		//  dismissed with Cancel
	}

	// Since the dialog has been closed, return FALSE so that we exit the
	//  application, rather than start the application's message pump.
	return FALSE;
}
