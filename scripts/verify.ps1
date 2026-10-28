#Requires -Version 5.1
<#
.SYNOPSIS
  로컬 검증: PrepareDemo → Edit Mode tests → Play Mode tests (단계별 Unity batchmode, 실패 시 중단).

.DESCRIPTION
  CI·Addressables 콘텐츠 빌드는 포함하지 않습니다. UNITY_EXE가 없으면 Hub Editor 경로(ProjectVersion.txt 버전)를 사용합니다.
#>
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$RepoRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$ProjectPath = Join-Path $RepoRoot 'project/unity-addressable'
$LogsDir = Join-Path $RepoRoot 'logs/verify'

function Get-EditorVersion {
    param([string]$ProjectRoot)
    $versionFile = Join-Path $ProjectRoot 'ProjectSettings/ProjectVersion.txt'
    if (-not (Test-Path -LiteralPath $versionFile)) {
        return '6000.3.9f1'
    }
    foreach ($line in Get-Content -LiteralPath $versionFile) {
        if ($line -match '^\s*m_EditorVersion:\s*(\S+)\s*$') {
            return $Matches[1]
        }
    }
    return '6000.3.9f1'
}

function Resolve-UnityExe {
    if ($env:UNITY_EXE) {
        if (Test-Path -LiteralPath $env:UNITY_EXE) {
            return (Resolve-Path -LiteralPath $env:UNITY_EXE).Path
        }
        Write-Host "UNITY_EXE path not found: $env:UNITY_EXE" -ForegroundColor Red
        exit 1
    }

    $version = Get-EditorVersion -ProjectRoot $ProjectPath
    $hubExe = Join-Path ${env:ProgramFiles} "Unity\Hub\Editor\$version\Editor\Unity.exe"
    if (Test-Path -LiteralPath $hubExe) {
        return (Resolve-Path -LiteralPath $hubExe).Path
    }

    Write-Host "Unity Editor not found (version $version)." -ForegroundColor Red
    Write-Host "Install that version via Hub, or set UNITY_EXE to the Unity.exe path."
    exit 1
}

function Invoke-UnityStep {
    param(
        [Parameter(Mandatory = $true)][string]$StepName,
        [Parameter(Mandatory = $true)][string[]]$StepArgs
    )

    $logFile = Join-Path $LogsDir "$StepName.log"
    $arguments = @(
        '-batchmode',
        '-nographics',
        '-projectPath', $ProjectPath,
        '-logFile', $logFile
    ) + $StepArgs

    Write-Host ""
    Write-Host "==> $StepName" -ForegroundColor Cyan
    Write-Host "    Log: $logFile"

    $process = Start-Process -FilePath $UnityExe -ArgumentList $arguments -Wait -PassThru -NoNewWindow
    $exitCode = $process.ExitCode
    if ($exitCode -ne 0) {
        Write-Host "FAILED: $StepName (exit $exitCode). Log: $logFile" -ForegroundColor Red
        exit $exitCode
    }
    Write-Host "OK: $StepName" -ForegroundColor Green
}

# -runTests는 테스트 후 스스로 종료한다. -quit를 함께 주면 테스트 전에 exit 0으로 끝나므로 결과 파일로 실행 여부를 확인한다.
function Assert-TestResults {
    param(
        [Parameter(Mandatory = $true)][string]$StepName,
        [Parameter(Mandatory = $true)][string]$ResultsFile
    )

    if (-not (Test-Path -LiteralPath $ResultsFile)) {
        Write-Host "FAILED: $StepName produced no test results: $ResultsFile" -ForegroundColor Red
        exit 1
    }

    $run = ([xml](Get-Content -LiteralPath $ResultsFile -Raw)).'test-run'
    Write-Host "    Tests: total=$($run.total) passed=$($run.passed) failed=$($run.failed) skipped=$($run.skipped)"
    if ([int]$run.total -eq 0) {
        Write-Host "FAILED: $StepName ran 0 tests." -ForegroundColor Red
        exit 1
    }
}

if (-not (Test-Path -LiteralPath $ProjectPath)) {
    Write-Host "Unity project path not found: $ProjectPath" -ForegroundColor Red
    exit 1
}

New-Item -ItemType Directory -Force -Path $LogsDir | Out-Null

$UnityExe = Resolve-UnityExe
Write-Host "Unity: $UnityExe"
Write-Host "Project: $ProjectPath"

Invoke-UnityStep -StepName 'prepare-demo' -StepArgs @(
    '-executeMethod', 'AddressableLayoutVerify.PrepareDemo',
    '-quit'
)

$editResults = Join-Path $LogsDir 'editmode.xml'
Invoke-UnityStep -StepName 'editmode' -StepArgs @(
    '-runTests',
    '-testPlatform', 'editmode',
    '-assemblyNames', 'AddressableLayout.EditorTests',
    '-testResults', $editResults
)
Assert-TestResults -StepName 'editmode' -ResultsFile $editResults

$playResults = Join-Path $LogsDir 'playmode.xml'
Invoke-UnityStep -StepName 'playmode' -StepArgs @(
    '-runTests',
    '-testPlatform', 'playmode',
    '-assemblyNames', 'AddressableLayout.PlayModeTests',
    '-testResults', $playResults
)
Assert-TestResults -StepName 'playmode' -ResultsFile $playResults

Write-Host ""
Write-Host "Verify finished. Output: $LogsDir" -ForegroundColor Green
