@echo off
:: Di chuyen den thu muc chua noi dung game
cd /d "C:\Users\Thanh\source\repos\boom-main\Project2\BomberClient\Content"

:: Chay lenh build content voi mgcb
"C:\Users\Thanh\.dotnet\tools\mgcb.exe" /@:Content.mgcb /platform:Windows

:: Tam dung de ban co the xem ket qua (thanh cong hay loi)
pause