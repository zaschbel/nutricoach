@echo off
echo NutriCoach wird aufgeraeumt...
echo.

cd /d "%~dp0"

if exist ".vs" (
    echo Loesche .vs Cache-Ordner...
    rmdir /s /q ".vs"
)

if exist "src\NutriCoach.App\bin" (
    echo Loesche bin-Ordner...
    rmdir /s /q "src\NutriCoach.App\bin"
)

if exist "src\NutriCoach.App\obj" (
    echo Loesche obj-Ordner...
    rmdir /s /q "src\NutriCoach.App\obj"
)

echo.
echo Fertig! Jetzt NutriCoach.sln oeffnen und in Visual Studio:
echo 1. Erstellen -^> Projektmappe neu erstellen
echo 2. F5 druecken
echo.
pause
