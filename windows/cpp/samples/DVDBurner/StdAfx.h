// stdafx.h : include file for standard system include files,
//  or project specific include files that are used frequently, but
//      are changed infrequently
//

#pragma once

#define _CRT_NON_CONFORMING_SWPRINTFS
#define _CRT_SECURE_NO_WARNINGS
#define _CRT_SECURE_CPP_OVERLOAD_SECURE_NAMES 1

#define _BIND_TO_CURRENT_MFC_VERSION 1
#define _BIND_TO_CURRENT_CRT_VERSION 1

// Target Windows Server 2003 SP1, Windows XP SP2
// See http://msdn.microsoft.com/en-us/library/aa383745(VS.85).aspx
#define _WIN32_WINNT 0x0502
#define WINVER 0x0502 

#define VC_EXTRALEAN		// Exclude rarely-used stuff from Windows headers

#include <afxwin.h>         // MFC core and standard components
#include <afxext.h>         // MFC extensions
#include <afxdisp.h>        // MFC Automation classes
#include <afxdtctl.h>		// MFC support for Internet Explorer 4 Common Controls
#ifndef _AFX_NO_AFXCMN_SUPPORT
	#include <afxcmn.h>		// MFC support for Windows Common Controls
#endif // _AFX_NO_AFXCMN_SUPPORT

// enable XP / Vista style
#if defined _M_IX86
	#pragma comment(linker,"/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='x86' publicKeyToken='6595b64144ccf1df' language='*'\"")
#elif defined _M_IA64
	#pragma comment(linker,"/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='ia64' publicKeyToken='6595b64144ccf1df' language='*'\"")
#elif defined _M_X64
	#pragma comment(linker,"/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='amd64' publicKeyToken='6595b64144ccf1df' language='*'\"")
#else
	#pragma comment(linker,"/manifestdependency:\"type='win32' name='Microsoft.Windows.Common-Controls' version='6.0.0.0' processorArchitecture='*' publicKeyToken='6595b64144ccf1df' language='*'\"")
#endif

#include "PrimoBurner.h"

using namespace primo::burner;

#include <io.h>

#include <exception>
#include <string>
#include <vector>

// Introduce "stl" namespace to allow implementation with different STL libraries
namespace stl = std;

// Make sure UNICODE and _UNICODE are defined for UTF-8 and UTF-16 configurations.
#if defined(UTF8) || defined(UTF16)
	#if !defined(UNICODE) && !defined(_UNICODE)
		#error "UNICODE and _UNICODE must be defined when UTF8 or UTF16 is defined."
	#endif
#endif

#if defined(UTF8) || defined(UTF16) || defined(UNICODE) || defined(_UNICODE)
    typedef stl::wstring tstring;

    inline tstring toStdTString(const CStringW& qs)
    {
        return tstring(qs.GetString());
    }

    inline CStringW fromStdTString(const stl::wstring& ts)
    {
        return CStringW(ts.c_str());
    }

    inline CStringW fromStdTString(const wchar_t* ts)
    {
        return CStringW(ts);
    }

    inline CStringW fromUtf8String(const utf8_t* ts)
    {
		// Figure out how many characters we are going to get 
		int nChars = MultiByteToWideChar(CP_UTF8, 0, (LPCSTR)ts, -1, NULL, 0); 
		if (0 == nChars)
			return CStringW();
		
		CStringW ret(wchar_t(0), nChars + 1);
		MultiByteToWideChar(CP_UTF8, 0, (LPCSTR)ts, -1, ret.GetBuffer(), nChars); 
		ret.ReleaseBuffer();

        return ret;
    }

    inline CStringA toUtf8String(const wchar_t* ts)
    {
		// Figure out how many characters we are going to get 
		int nChars = WideCharToMultiByte(CP_UTF8, 0, ts, -1, NULL, 0, NULL, NULL); 
		if (0 == nChars)
			return CStringA();
		
		CStringA ret(char(0), nChars + 1);
		WideCharToMultiByte(CP_UTF8 , 0, ts, -1, ret.GetBuffer(), nChars, NULL, NULL); 
		ret.ReleaseBuffer();

        return ret;
    }

	inline CStringA toUtf8String(const stl::wstring& ts)
    {
		return toUtf8String(ts.c_str());
    }

    inline CStringW fromUtf16String(const utf16_t* ts)
    {
        return CStringW((const wchar_t*)ts);
    }

    inline CStringW toUtf16String(const wchar_t* ts)
    {
        return CStringW(ts);
    }

	inline CStringW toUtf16String(const stl::wstring& ts)
    {
        return toUtf16String(ts.c_str());
    }

#else
    typedef stl::string tstring;

    inline tstring toStdTString(const CStringA& qs)
    {
        return tstring(qs.GetString());
    }

    inline CStringA fromStdTString(const stl::string& ts)
    {
        return CStringA(ts.c_str());
    }

    inline CStringA fromStdTString(const char* ts)
    {
        return CStringA(ts);
    }
#endif

#if defined(UTF8)
    #define UTF_TO_CHAR(x) ((char)x)
    #define UTF_TO_STR(x) (fromUtf8String(x).toStdString())
    
    #define UTF_TO_TCHAR(x) ((TCHAR)x)
    #define UTF_TO_TSTR(x) toStdTString(fromUtf8String(x))

    #define TSTR_TO_UTF(x) ((const char_t*)toUtf8String(x).GetString())
#elif defined(UTF16) || defined(UNICODE) || defined(_UNICODE)
    #define UTF_TO_CHAR(x) ((char)x)
    #define UTF_TO_STR(x) (fromUtf16String(x).GetString())

    #define UTF_TO_TCHAR(x) ((TCHAR)x)
    #define UTF_TO_TSTR(x) toStdTString(fromUtf16String(x))

    #define TSTR_TO_UTF(x) ((const char_t*)toUtf16String(x).GetString())
#else
    #define UTF_TO_CHAR(x) ((char)x)
    #define UTF_TO_STR(x) (toStdTString(x))

    #define UTF_TO_TCHAR(x) ((TCHAR)x)
    #define UTF_TO_TSTR(x) (toStdTString(x))

    #define TSTR_TO_UTF(x) (fromStdTString(x).GetString())
#endif

//{{AFX_INSERT_LOCATION}}
// Microsoft Visual C++ will insert additional declarations immediately before the previous line.

