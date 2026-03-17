@echo off
setlocal

REM ─────────────────────────────────────────────────────────────────
REM  run.bat  —  Crea el entorno conda (si no existe) y ejecuta el
REM             script de reemplazo de color verde → ámbar.
REM
REM  Uso:
REM    run.bat            → procesa todos los assets y guarda en output\
REM    run.bat --dry-run  → solo reporta, no escribe nada
REM    run.bat --preview  → abre ventana comparación del primer asset verde
REM ─────────────────────────────────────────────────────────────────

set ENV_NAME=img-recolor
set SCRIPT_DIR=%~dp0
set CONDA_ROOT=C:\Users\SALBRI~1\miniconda3
set CONDA_BAT=%CONDA_ROOT%\condabin\conda.bat

REM Inicializar conda en esta sesion CMD
call "%CONDA_ROOT%\condabin\conda_hook.bat" 2>nul

echo [1/3] Verificando entorno conda "%ENV_NAME%"...
call "%CONDA_BAT%" env list | findstr /C:"%ENV_NAME%" >nul 2>&1
if errorlevel 1 (
    echo      Entorno no encontrado. Creando desde environment.yml...
    call "%CONDA_BAT%" env create -f "%SCRIPT_DIR%environment.yml"
    if errorlevel 1 (
        echo ERROR: No se pudo crear el entorno conda.
        pause
        exit /b 1
    )
    echo      Entorno creado exitosamente.
) else (
    echo      Entorno ya existe, omitiendo creacion.
)

echo [2/3] Activando entorno...
call "%CONDA_BAT%" activate %ENV_NAME%
if errorlevel 1 (
    echo ERROR: No se pudo activar el entorno.
    pause
    exit /b 1
)

echo [3/3] Ejecutando script de recoloreo...
python "%SCRIPT_DIR%recolor_green_to_amber.py" %*

echo.
echo Presiona cualquier tecla para cerrar...
pause >nul
