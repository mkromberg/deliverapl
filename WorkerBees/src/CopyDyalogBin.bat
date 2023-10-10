set FROMDIR=%1
set DIR=%FROMDIR:"=%
set TODIR=.\Dyalog
mkdir %TODIR%
COPY /b/v/y "%DIR%\bridge*-64_unicode.dll" %TODIR%
COPY /b/v/y "%DIR%\dyalog*_64rt_unicode.dll" %TODIR%
COPY /b/v/y "%DIR%\dyalog*_64_unicode.dll" %TODIR%
COPY /b/v/y "%DIR%\dyalogc64_unicode.exe" %TODIR%
COPY /b/v/y "%DIR%\dyalognet.dll" %TODIR%