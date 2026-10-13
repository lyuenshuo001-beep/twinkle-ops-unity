param([switch]$Push)
$ErrorActionPreference='Stop'
$project=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$gitCommand=Get-Command git -ErrorAction SilentlyContinue
$gitExe=if($gitCommand) {$gitCommand.Source} else {Join-Path $env:USERPROFILE '.cache\codex-runtimes\codex-primary-runtime\dependencies\native\git\cmd\git.exe'}
if(!(Test-Path -LiteralPath $gitExe)) { throw 'Git not found. Install Git for Windows including Git Credential Manager.' }
# User completes device authorization at GitHub; passwords/tokens are never passed to this script.
& $gitExe credential-manager github login --username lyuenshuo001-beep --device
if($LASTEXITCODE -ne 0) { throw 'GitHub authorization was not completed.' }
if($Push) {
    & $gitExe -C $project push -u origin main
    if($LASTEXITCODE -ne 0) { throw 'Push failed. Review the Git error; do not force-push.' }
}
