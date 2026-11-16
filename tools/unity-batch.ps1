<#
.SYNOPSIS
    Ejecuta Unity en modo batch sobre el proyecto AcrofobiaVR.

.EXAMPLE
    ./tools/unity-batch.ps1                       # Reconstruye escenas y assets
    ./tools/unity-batch.ps1 -Method Acrofobia.Editor.Build.PlayerBuilder.BuildWindows
    ./tools/unity-batch.ps1 -Tests                # Ejecuta las pruebas EditMode
    ./tools/unity-batch.ps1 -Tests -Platform PlayMode

.NOTES
    El editor de Unity debe estar cerrado: el modo batch no puede abrir un proyecto en uso.
#>
param(
    [string]$Method = "Acrofobia.Editor.Build.ProjectBuilder.BuildBatch",
    [switch]$Tests,
    [ValidateSet("EditMode", "PlayMode")]
    [string]$Platform = "EditMode"
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "unity/AcrofobiaVR"
$versionFile = Join-Path $projectPath "ProjectSettings/ProjectVersion.txt"

$version = "6000.6.0f1"
if (Test-Path $versionFile) {
    $match = Select-String -Path $versionFile -Pattern "m_EditorVersion:\s*(\S+)"
    if ($match) { $version = $match.Matches[0].Groups[1].Value }
}

$unity = if ($env:UNITY_EXE) { $env:UNITY_EXE } else { "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe" }
if (-not (Test-Path $unity)) { throw "No se encontró Unity en '$unity'. Define la variable UNITY_EXE." }

if (Test-Path (Join-Path $projectPath "Temp/UnityLockfile")) {
    Write-Warning "Parece que el editor tiene el proyecto abierto. Ciérralo antes de continuar."
}

if ($Tests) {
    $results = Join-Path $repoRoot "builds/test-results-$Platform.xml"
    New-Item -ItemType Directory -Force (Split-Path $results) | Out-Null
    & $unity -batchmode -projectPath $projectPath -runTests -testPlatform $Platform -testResults $results -logFile - | Out-Host
} else {
    & $unity -batchmode -projectPath $projectPath -executeMethod $Method -quit -logFile - | Out-Host
}
exit $LASTEXITCODE
