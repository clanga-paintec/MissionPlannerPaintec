@echo off
setlocal

REM ─────────────────────────────────────────────────────────────────
REM  run.bat  —  Crea/actualiza entorno conda y ejecuta el
REM             script de reemplazo de color verde -> ambar.
REM
REM  Uso:
REM    .\run.bat            -> procesa todos los assets y guarda en output\
REM    .\run.bat --dry-run  -> solo reporta, no escribe nada
REM    .\run.bat --preview  -> abre ventana comparacion del primer asset verde
REM ─────────────────────────────────────────────────────────────────

set SCRIPT_DIR=%~dp0
set ENV_NAME=img-recolor
set CONDA_ROOT=C:\ProgramData\miniconda3

REM --- Verificar que conda existe ---
if not exist "%CONDA_ROOT%\Scripts\activate.bat" (
    echo ERROR: No se encontro conda en %CONDA_ROOT%
    echo Verifica la ruta de instalacion de Miniconda3.
    pause
    exit /b 1
)

REM --- Activar conda base ---
call "%CONDA_ROOT%\Scripts\activate.bat" "%CONDA_ROOT%"

REM --- Crear o actualizar el entorno desde environment.yml ---
conda env list | findstr /i "%ENV_NAME%" >nul 2>&1
if errorlevel 1 (
    echo [1/2] Creando entorno conda "%ENV_NAME%"...
    conda env create -f "%SCRIPT_DIR%environment.yml"
    if errorlevel 1 (
        echo ERROR: No se pudo crear el entorno conda.
        pause
        exit /b 1
    )
) else (
    echo [1/2] Entorno "%ENV_NAME%" ya existe. Actualizando dependencias...
    conda env update -f "%SCRIPT_DIR%environment.yml" --prune
)

REM --- Activar entorno del proyecto ---
call conda activate %ENV_NAME%

echo [2/2] Ejecutando script de recoloreo...
python "%SCRIPT_DIR%recolor_green_to_amber.py" %*

echo.
echo Presiona cualquier tecla para cerrar...
pause >nul
