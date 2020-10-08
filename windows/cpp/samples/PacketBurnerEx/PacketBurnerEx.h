// This is a part of the High Performance CD Engine Library.
// Copyright (C) 2001-2003 Primo Software Corporation
// All rights reserved.
//
// This source code is only intended as a supplement to the
// High Performance CD Engine Reference and related
// electronic documentation provided with the library.
// See these sources for detailed information regarding the	
// High Performance CD Engine product.

// PacketBurner.h : main header file for the DATABURNER application
//

#pragma once

#include "resource.h"		// main symbols

/////////////////////////////////////////////////////////////////////////////
// CPacketBurnerApp:
// See PacketBurner.cpp for the implementation of this class
//

class CPacketBurnerApp : public CWinApp
{
public:
	CPacketBurnerApp();

// Overrides
	// ClassWizard generated virtual function overrides
	//{{AFX_VIRTUAL(CPacketBurnerApp)
	public:
	virtual BOOL InitInstance();
	//}}AFX_VIRTUAL

// Implementation

	//{{AFX_MSG(CPacketBurnerApp)
		// NOTE - the ClassWizard will add and remove member functions here.
		//    DO NOT EDIT what you see in these blocks of generated code !
	//}}AFX_MSG
	DECLARE_MESSAGE_MAP()
};


/////////////////////////////////////////////////////////////////////////////

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

