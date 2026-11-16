$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:windir 'Microsoft.NET\Framework64\v4.0.30319'
$runtime = Join-Path ${env:ProgramFiles(x86)} 'Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8\Facades\System.Runtime.dll'
$metadata = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits\10\UnionMetadata'
$winmd = Get-ChildItem "$metadata\*\Windows.winmd" | Where-Object { $_.Directory.Name -match '^10\.' } | Sort-Object { [version]$_.Directory.Name } -Descending | Select-Object -First 1
if (!$winmd -or !(Test-Path $runtime)) { throw 'Install Windows 10/11 SDK and .NET Framework 4.8 targeting pack.' }
$out = Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Force $out | Out-Null
$sources = (Get-ChildItem "$PSScriptRoot\src\*.cs").FullName
& "$compiler\csc.exe" /nologo /optimize+ /target:winexe "/out:$out\LockScreen.exe" "/r:$runtime" "/r:$compiler\System.Runtime.WindowsRuntime.dll" "/r:$($winmd.FullName)" /r:System.Security.dll /r:System.Net.Http.dll /r:System.Web.dll /r:System.Web.Extensions.dll $sources
if ($LASTEXITCODE) { throw 'Compilation failed.' }

