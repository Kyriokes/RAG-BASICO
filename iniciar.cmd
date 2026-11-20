@echo off
rem Doble clic para arrancar todo: Qdrant, si esta en esta PC y no esta andando, y la API, que abre el navegador.
rem El trabajo lo hace scripts\iniciar.ps1. Se frena con Ctrl+C en esta ventana.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0scripts\iniciar.ps1"
if errorlevel 1 pause
