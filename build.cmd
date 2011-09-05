@echo off
cls
"packages\FAKE.1.58.9\tools\Fake.exe" "build.fsx" target=%1
pause
