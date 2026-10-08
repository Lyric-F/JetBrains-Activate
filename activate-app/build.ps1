param([switch]$WithTests, [switch]$TestsOnly)
$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
$taskFramework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$taskCompiler = Join-Path $taskFramework 'csc.exe'
if (-not (Test-Path -LiteralPath $taskCompiler)) { throw 'The Windows .NET Framework C# compiler is unavailable.' }
$taskDist = Join-Path $taskRoot 'dist'
New-Item -ItemType Directory -Path $taskDist -Force | Out-Null
foreach ($taskResource in @('payload.zip','payload.manifest.json','app.manifest')) {
    if (-not (Test-Path -LiteralPath (Join-Path $taskRoot "resources\$taskResource"))) { throw "Missing resource: $taskResource" }
}
$taskCommon = @('/nologo','/optimize+','/codepage:65001','/langversion:5','/platform:anycpu',
    '/r:System.dll','/r:System.Core.dll','/r:System.Web.Extensions.dll','/r:System.IO.Compression.dll','/r:System.IO.Compression.FileSystem.dll',
    "/resource:$taskRoot\resources\payload.zip,Activate.Payload.zip",
    "/resource:$taskRoot\resources\payload.manifest.json,Activate.Payload.manifest.json")
$taskCoreSources = @("$taskRoot\src\Models.cs","$taskRoot\src\Payload.cs","$taskRoot\src\Deployment.cs")
$taskAppArgs = $taskCommon + @('/target:winexe',"/out:$taskDist\Activate.exe", "/win32manifest:$taskRoot\resources\app.manifest",
    "/r:$taskFramework\WPF\PresentationFramework.dll", "/r:$taskFramework\WPF\PresentationCore.dll", "/r:$taskFramework\WPF\WindowsBase.dll", '/r:System.Xaml.dll',
    "/resource:$taskRoot\src\MainWindow.xaml,Activate.MainWindow.xaml") + $taskCoreSources + @("$taskRoot\src\App.cs","$taskRoot\src\AssemblyInfo.cs")
if (Test-Path -LiteralPath "$taskRoot\resources\activate.ico") { $taskAppArgs += "/win32icon:$taskRoot\resources\activate.ico" }
if (-not $TestsOnly) {
    & $taskCompiler @taskAppArgs
    if ($LASTEXITCODE -ne 0) { throw "Application compilation failed: $LASTEXITCODE" }
}
if ($WithTests -or $TestsOnly) {
    $taskTestArgs = $taskCommon + @('/target:exe',"/out:$taskDist\Activate.Tests.exe") + $taskCoreSources + @("$taskRoot\tests\IntegrationTests.cs")
    & $taskCompiler @taskTestArgs
    if ($LASTEXITCODE -ne 0) { throw "Test compilation failed: $LASTEXITCODE" }
}
$taskFile = Get-Item -LiteralPath "$taskDist\Activate.exe"
$taskHash = Get-FileHash -LiteralPath $taskFile.FullName -Algorithm SHA256
[pscustomobject]@{ Path = $taskFile.FullName; Bytes = $taskFile.Length; SHA256 = $taskHash.Hash } | Format-List
