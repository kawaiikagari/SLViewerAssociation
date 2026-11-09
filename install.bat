@echo off
chcp 65001 > nul
echo ========================================================
echo   SLViewerAssociation セットアップ
echo ========================================================
echo.
echo ツールを専用フォルダに配置し、ショートカットを作成します。
echo.

:: ユーザーの AppData\Local 内に安全に配置
set "INSTALL_DIR=%USERPROFILE%\AppData\Local\SLViewerAssociation"
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

copy /y "%~dp0SLViewerAssociation.exe" "%INSTALL_DIR%\" > nul
if exist "%~dp0SLViewerAssociation.exe.config" copy /y "%~dp0SLViewerAssociation.exe.config" "%INSTALL_DIR%\" > nul

echo [1/2] デスクトップにショートカットを作成しています...
powershell -Command "$ws = New-Object -ComObject WScript.Shell; $sc = $ws.CreateShortcut([Environment]::GetFolderPath('Desktop') + '\SLViewerAssociation.lnk'); $sc.TargetPath = '%INSTALL_DIR%\SLViewerAssociation.exe'; $sc.Save()"

echo [2/2] 準備が完了しました！
echo.
echo --------------------------------------------------------
echo 【ご案内】
echo これからツールが起動します。
echo 一覧からビューアーを確認し、「選んだ設定を適用」を押してください。
echo --------------------------------------------------------
echo.
pause

:: 本体を起動
start "" "%INSTALL_DIR%\SLViewerAssociation.exe"
