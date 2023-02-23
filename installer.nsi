; The name of the installer
Name Dnskeeper

; The file to write
OutFile dnskeeper-install.exe

; Request application privileges for Windows Vista
RequestExecutionLevel admin

; Build Unicode installer
Unicode True

; The default installation directory
InstallDir $PROGRAMFILES\Dnskeeper

; Pages to display
Page directory
Page instfiles

; Size of the installation folder (in kb)
!define INSTALLSIZE 205

; Registry path to uninstaller
!define UNINSTREGKEY SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Dnskeeper

;--------------------------------
; The stuff to install
Section "" ;No components page, name is not important

  ; Set output path to the installation directory.
  SetOutPath $INSTDIR
  
  ; Put file there
  File dnskeeper\bin\Debug\dnskeeper.exe
  File icons8-crab-96.ico
  
  ; Write the uninstall keys for Windows
  WriteRegStr HKLM ${UNINSTREGKEY} "DisplayName" "Dnskeeper"
  WriteRegStr HKLM ${UNINSTREGKEY} "UninstallString" '"$INSTDIR\uninstall.exe"'
  WriteRegDWORD HKLM ${UNINSTREGKEY} "NoModify" 1
  WriteRegDWORD HKLM ${UNINSTREGKEY} "NoRepair" 1
  WriteRegDWORD HKLM ${UNINSTREGKEY} "EstimatedSize" ${INSTALLSIZE}
  WriteRegStr HKLM ${UNINSTREGKEY} "DisplayIcon" '"$INSTDIR\icons8-crab-96.ico"'
  
  
  ; Write the uninstaller
  WriteUninstaller $INSTDIR\uninstall.exe
  
SectionEnd


; Create start menu components
Section "Start Menu Shortcuts"

  CreateShortcut $SMPROGRAMS\Dnskeeper.lnk $INSTDIR\dnskeeper.exe

SectionEnd

;--------------------------------
; Uninstaller
Section "Uninstall"
  
  ; Remove registry keys
  DeleteRegKey HKLM ${UNINSTREGKEY}
  DeleteRegKey HKLM SOFTWARE\Dnskeeper

  ; Remove files and uninstaller
  Delete $INSTDIR\dnskeeper.exe
  Delete $INSTDIR\uninstall.exe
  Delete $INSTDIR\icons8-crab-96.ico

  ; Remove start menu shortcuts
  Delete $SMPROGRAMS\Dnskeeper.lnk

  ; Remove install directory
  RMDir $INSTDIR

SectionEnd
