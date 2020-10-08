// This is a part of the High Performance CD Engine Library.
// Copyright (C) 2001-2003 Primo Software Corporation
// All rights reserved.
//
// This source code is only intended as a supplement to the
// High Performance CD Engine Reference and related
// electronic documentation provided with the library.
// See these sources for detailed information regarding the	
// High Performance CD Engine product.

#pragma once
#include "PrimoBurner.h"

/**
	This simple file based stream provides implementation of primo::Stream interface. 
	It does nothing more than giving access to the original file through primo::Stream
	This implementation is for demonstration purposes only. 
*/

class CFileStream: public primo::Stream
{
	HANDLE m_hFile;
	std::string m_fileExt;
	CString m_filePath;

public:

	CFileStream(const CString& path, const char* ext = NULL)
	{
		m_hFile = INVALID_HANDLE_VALUE;
		m_filePath = path;

		if(ext)
		{
			// explicitly set the file extension
			m_fileExt = ext; 
		}
		else
		{
			// implicitly set the file extension getting it from the file path
			LPCTSTR pExt = PathFindExtension(path);
			if (pExt)
			{
				std::wstring wext = pExt;
				m_fileExt.assign(wext.begin()+1, wext.end()); // skip the '.' in the extension
			}
			else
			{
				m_fileExt.clear();
			}
		}
	}

	~CFileStream()
	{
		close();
	}

	// primo::codecs::Stream interface

	bool_t open()
	{
		if (isOpen())
			return seek(0);

		m_hFile = CreateFile(m_filePath, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);

		if (!isOpen())
			return FALSE;

		return TRUE;
	}

	void  close()
	{
		if(isOpen())
		{
			CloseHandle(m_hFile);
			m_hFile = INVALID_HANDLE_VALUE;
		}
	}

	bool_t read(void* pBuffer, int32_t dwBufferSize, int32_t *pdwTotalRead)
	{
		if(!isOpen())
			return FALSE;

		return TRUE == ReadFile(m_hFile, pBuffer, (DWORD)dwBufferSize, (DWORD *)pdwTotalRead, NULL);
	}

	int64_t size() const
	{
		//bool closeWhenDone = false;
		
		const int64_t error = -1LL;

		if(!isOpen())
		{
			//if (!open())
				return error;

			//closeWhenDone = true;
		}

		LARGE_INTEGER fileSize;
		BOOL res = GetFileSizeEx(m_hFile, &fileSize);

		//if (closeWhenDone)
		//	close();

		if (!res)
			return error;

		return fileSize.QuadPart;
	}

	int64_t position() const
	{
		const int64_t error = -1LL;

		if(!isOpen())
			return error;

		LARGE_INTEGER newPosition;
		LARGE_INTEGER offset;
		offset.QuadPart = 0;
		BOOL res = SetFilePointerEx(m_hFile, offset, &newPosition, FILE_CURRENT);

		if (!res)
			return error;

		return newPosition.QuadPart;
	}

	bool_t seek(int64_t ddwOffset)
	{
		if(!isOpen())
			return false;

		LARGE_INTEGER offset;
		offset.QuadPart = ddwOffset;
		return SetFilePointerEx(m_hFile, offset, NULL, FILE_BEGIN);
	}

	bool_t canRead() const
	{
		return TRUE;
	}

	bool_t canWrite() const
	{
		return FALSE;
	}

	bool_t canSeek() const
	{
		return TRUE;
	}

	bool_t write(const void* /*buffer*/, int32_t /*dataSize*/)
	{
		return FALSE;
	}

	bool_t isOpen() const { return (m_hFile != INVALID_HANDLE_VALUE); }

};
