#pragma once

#define _CRT_NON_CONFORMING_SWPRINTFS
#define _CRT_SECURE_NO_WARNINGS
#define _CRT_SECURE_CPP_OVERLOAD_SECURE_NAMES 1

#define _BIND_TO_CURRENT_CRT_VERSION 1

#include <tchar.h>

#include <conio.h>
#include <stdio.h>
#include <windows.h>
#include <assert.h>

#include <string>
#include <vector>
#include <algorithm>

#if defined(UNICODE) || defined(_UNICODE)
    typedef std::wstring tstring;
#else
    typedef std::string tstring;
#endif

#include "PrimoBurner.h"
using namespace primo::burner;
