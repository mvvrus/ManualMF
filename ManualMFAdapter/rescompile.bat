call "%VS110COMNTOOLS%VsDevCmd.bat"
for /F %%a in ('dir /b %1\resources\*.txt') do resgen %1\resources\%%a  %1\%%~na.resx
