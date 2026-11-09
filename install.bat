@echo off
chcp 65001 > nul
setlocal

echo ========================================================
echo   SLViewerAssociation セットアップ
echo ========================================================
echo.
echo ツールを専用フォルダに配置し、ショートカットを作成します。
echo.

:: ユーザーの AppData\Local 内に安全に配置
set "INSTALL_DIR=%USERPROFILE%\AppData\Local\SLViewerAssociation"
if not exist "%INSTALL_DIR%" mkdir "%INSTALL_DIR%"

echo [1/2] ファイルを配置しています...
if not exist "%~dp0SLViewerAssociation.exe" (
    echo 【エラー】SLViewerAssociation.exe が見つかりません。
    goto :error
)

copy /y "%~dp0SLViewerAssociation.exe" "%INSTALL_DIR%\" > nul
if exist "%~dp0SLViewerAssociation.exe.config" (
    copy /y "%~dp0SLViewerAssociation.exe.config" "%INSTALL_DIR%\" > nul
)

echo [2/2] デスクトップにショートカットを作成しています...
powershell -NoProfile -Command "$ws = New-Object -ComObject WScript.Shell; $sc = $ws.CreateShortcut([Environment]::GetFolderPath('Desktop') + '\SLViewerAssociation.lnk'); $sc.TargetPath = '%INSTALL_DIR%\SLViewerAssociation.exe'; $sc.WorkingDirectory = '%INSTALL_DIR%'; $sc.Save()"

echo.
echo --------------------------------------------------------
echo 【ご案内】
echo 準備が完了しました！これからツールが起動します。
echo 一覧からビューアーを確認し、「選んだ設定を適用」を押してください。
echo --------------------------------------------------------
echo.
pause

:: 本体を起動
start "" "%INSTALL_DIR%\SLViewerAssociation.exe"
goto :eof

:error
echo.
echo インストールに失敗しました。何かキーを押すと終了します。
pause > nul
exit /b 1
