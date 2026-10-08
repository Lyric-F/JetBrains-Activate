param()
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent $PSScriptRoot
$taskFramework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$taskArgs = @('/nologo','/target:exe','/langversion:5','/codepage:65001',
    "/out:$taskRoot\dist\Activate.UiSmoke.exe", "/r:$taskRoot\dist\Activate.exe",
    '/r:System.dll','/r:System.Core.dll','/r:System.Xaml.dll',
    "/r:$taskFramework\WPF\PresentationFramework.dll", "/r:$taskFramework\WPF\PresentationCore.dll", "/r:$taskFramework\WPF\WindowsBase.dll",
    "$taskRoot\tests\UiSmoke.cs")
& "$taskFramework\csc.exe" @taskArgs
if ($LASTEXITCODE -ne 0) { throw 'UI test compilation failed.' }
& "$taskRoot\dist\Activate.UiSmoke.exe" "$taskRoot\reports"
if ($LASTEXITCODE -ne 0) { throw 'UI smoke test failed.' }
