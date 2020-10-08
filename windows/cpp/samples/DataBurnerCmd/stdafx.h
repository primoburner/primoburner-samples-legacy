#pragma once

#define _CRT_NON_CONFORMING_SWPRINTFS
#define _CRT_SECURE_NO_WARNINGS
#define _CRT_SECURE_CPP_OVERLOAD_SECURE_NAMES 1

#define _BIND_TO_CURRENT_CRT_VERSION 1

#include <tchar.h>

#include <conio.h>
#include <stdio.h>
#include <windows.h>
#include <shlwapi.h>
#include <assert.h>
#include <string>
#include <vector>

// Introduce "stl" namespace to allow implementation with different STL libraries
namespace stl = std;

#if defined(UTF8) || defined(UTF16) || defined(UNICODE) || defined(_UNICODE)
    typedef stl::wstring tstring;
#else
    typedef stl::string tstring;
#endif

#include "PrimoBurner.h"
using namespace primo::burner;
