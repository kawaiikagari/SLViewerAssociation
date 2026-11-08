# SPDX-License-Identifier: GPL-3.0-or-later
$ErrorActionPreference = 'Stop'
$sourceRoot = Split-Path -Parent $PSScriptRoot
$repositoryRoot = Split-Path -Parent $sourceRoot
& (Join-Path $sourceRoot 'build.ps1')
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe' }
$testRoot = Join-Path ([System.IO.Path]::GetTempPath()) ('SLViewerAssociation-tests-' + [guid]::NewGuid().ToString('N'))
[void](New-Item -ItemType Directory -Path $testRoot)
$fakeFile = Join-Path $testRoot 'Fake Viewer.exe'
$testFile = Join-Path $testRoot 'Tests.exe'
$appFile = Join-Path $repositoryRoot 'SLViewerAssociation.exe'
& $compiler /nologo "/out:$fakeFile" (Join-Path $PSScriptRoot 'FakeViewer.cs')
if ($LASTEXITCODE -ne 0) { throw 'Fake viewer build failed' }
& $compiler /nologo "/out:$testFile" "/reference:$appFile" (Join-Path $PSScriptRoot 'Tests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Tests build failed' }
Copy-Item -LiteralPath $appFile -Destination (Join-Path $testRoot 'SLViewerAssociation.exe')
& $testFile $testRoot $fakeFile
if ($LASTEXITCODE -ne 0) { throw "Tests failed. Temporary files: $testRoot" }
Write-Output "Temporary test files: $testRoot"
