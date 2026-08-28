@echo off
rem Joins the installer parts back into MenuBoardSetup.exe, then removes the parts.
copy /b MenuBoardSetup.part00+MenuBoardSetup.part01+MenuBoardSetup.part02+MenuBoardSetup.part03 MenuBoardSetup.exe
if exist MenuBoardSetup.exe (
  del MenuBoardSetup.part00 MenuBoardSetup.part01 MenuBoardSetup.part02 MenuBoardSetup.part03
  echo Done - MenuBoardSetup.exe is ready. Double-click it to install.
) else (
  echo Join failed - parts left in place.
)
pause
