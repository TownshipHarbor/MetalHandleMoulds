$ErrorActionPreference = 'Stop'

$here    = Split-Path -Parent $MyInvocation.MyCommand.Path
$out     = Join-Path $here 'build'
$pc      = Join-Path $here 'res\!metalhandlemoulds'
$android = Join-Path $out 'bundle\android\!metalhandlemoulds'

if (-not (Test-Path (Join-Path $here 'lib\CustomRecipesAPI.dll'))) { throw 'put the Quest build of CustomRecipesAPI in lib\' }

& dotnet build (Join-Path $here 'tools\questbundle\QuestBundle.csproj') -c Release -o (Join-Path $out 'questbundle') -nologo -v q
if ($LASTEXITCODE -ne 0) { throw 'questbundle build failed' }
New-Item -ItemType Directory -Force (Split-Path -Parent $android) | Out-Null
& (Join-Path $out 'questbundle\QuestBundle.exe') $pc $android
if ($LASTEXITCODE -ne 0) { throw 'Android bundle failed' }

& dotnet build (Join-Path $here 'quest\MetalHandleMoulds.Quest.csproj') -c Release -o $out -nologo -v q
if ($LASTEXITCODE -ne 0) { throw 'build failed' }
$dll = Join-Path $out 'MetalHandleMoulds.dll'
Write-Host ("{0} {1}" -f $dll, (Get-FileHash $dll -Algorithm SHA256).Hash.Substring(0, 8).ToLower())
