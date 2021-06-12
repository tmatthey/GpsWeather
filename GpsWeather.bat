if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\" (
  call "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Common7\Tools\VsDevCmd.bat"
) else (
  call "C:\Program Files (x86)\Microsoft Visual Studio\2017\Enterprise\Common7\Tools\VsDevCmd.bat"
)
msbuild /v:m  /t:"Clean;Build" /p:Configuration=Release GpsWeather.sln
cd
.\GpsWeather\bin\Release\net461\GpsWeather.exe .\TestFiles\test.tcx 50 25 "20-jun-21 13:00:00" > .\forecast.txt
pause
