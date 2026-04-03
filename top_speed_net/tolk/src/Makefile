!ifndef OUTDIR
OUTDIR = $(MAKEDIR)
!endif

# Referece "/I" flag: 
# https://docs.microsoft.com/en-us/cpp/build/reference/i-additional-include-directories?view=msvc-170
CFLAGS = /nologo /O2 /EHsc /LD /Gw /W4 /WL \
	/D_EXPORTING /DUNICODE
RFLAGS = /nologo

SYSLIBS = User32.Lib Ole32.Lib OleAut32.Lib

SOURCES = Tolk.cpp \
	ScreenReaderDriverBOY.cpp ScreenReaderDriverJAWS.cpp ScreenReaderDriverNVDA.cpp \
	ScreenReaderDriverSA.cpp ScreenReaderDriverSNova.cpp \
	ScreenReaderDriverWE.cpp ScreenReaderDriverZDSR.cpp \
	ScreenReaderDriverZT.cpp ScreenReaderDriverSAPI.cpp \
	fsapi.c wineyes.c zt.c

$(OUTDIR)\Tolk.dll: $(SOURCES) Tolk.res
	$(CC) $(CFLAGS) /Fe:$@ $** $(SYSLIBS)

Tolk.res:
	$(RC) $(RFLAGS) /fo $@ $*.rc

clean:
	del Tolk.res *.obj
