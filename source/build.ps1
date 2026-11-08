# SPDX-License-Identifier: GPL-3.0-or-later
$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
Push-Location $PSScriptRoot
try {
 & $compiler /nologo /target:winexe /platform:anycpu /optimize+ /utf8output /out:..\SLViewerAssociation.exe /win32manifest:app.manifest /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Xml.Linq.dll Association.cs Program.cs
 if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
} finally { Pop-Location }

