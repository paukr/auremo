; AuremoInstaller.nsi
;
; Based largely on the Example2.nsi script that ships with NSIS.
; To be improved later.
;
; To build the installer, copy Auremo.exe into the same direcory
; with this file and compile this file with the NSIS compiler.
;---------------------------------------------------------------

Name "AuremoInstaller"
OutFile "AuremoInstaller.exe"
InstallDir $PROGRAMFILES\Auremo
InstallDirRegKey HKLM "Software\Auremo" "Install_Dir"
RequestExecutionLeveL admin

;------------------------------

Page directory
Page instfiles

UninstPage uninstConfirm
UninstPage instfiles

;------------------------------

Section "Auremo"
	SectionIn RO

	SetOutPath $INSTDIR
	File "Auremo.exe"

	; Write the installation path into the registry
	WriteRegStr HKLM SOFTWARE\Auremo "Install_Dir" "$INSTDIR"

	; Write the uninstall keys for Windows
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Auremo" "DisplayName" "Auremo"
	WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Auremo" "UninstallString" '"$INSTDIR\uninstall.exe"'
	WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Auremo" "NoModify" 1
	WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Auremo" "NoRepair" 1
	WriteUninstaller "uninstall.exe"
SectionEnd

Section "Start Menu Shortcuts"
	CreateDirectory "$SMPROGRAMS\Auremo"
	CreateShortCut "$SMPROGRAMS\Auremo\Uninstall.lnk" "$INSTDIR\uninstall.exe" "" "$INSTDIR\uninstall.exe" 0
	CreateShortCut "$SMPROGRAMS\Auremo\Auremo.lnk" "$INSTDIR\Auremo.exe" "" "$INSTDIR\Auremo.exe" 0  
SectionEnd

Section "Uninstall"
	DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\Auremo"
	DeleteRegKey HKLM SOFTWARE\Auremo

	; Remove files
	Delete $INSTDIR\Auremo.exe
	Delete $INSTDIR\uninstall.exe

	; Remove shortcuts
	Delete "$SMPROGRAMS\Auremo\*.*"

	RMDir "$SMPROGRAMS\Auremo"
	RMDir "$INSTDIR"
SectionEnd
