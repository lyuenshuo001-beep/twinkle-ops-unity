$ErrorActionPreference='Stop'
$unity='D:\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe'
foreach($mode in @('EditMode','PlayMode')) {
    $result="D:\GameProjects\TwinkleOps\Logs\v11-release-$mode.xml"
    $args="-batchmode -projectPath D:\GameProjects\TwinkleOps -runTests -testPlatform $mode -testResults $result -logFile D:\GameProjects\TwinkleOps\Logs\v11-release-$mode.log"
    if($mode -eq 'EditMode'){$args+=' -nographics'}
    $proc=Start-Process -FilePath $unity -ArgumentList $args -WindowStyle Hidden -PassThru -Wait
    if($proc.ExitCode -ne 0){throw "$mode Unity exit $($proc.ExitCode)"}
    [xml]$report=Get-Content -LiteralPath $result
    if($report.'test-run'.result -ne 'Passed'){throw "$mode tests: $($report.'test-run'.result)"}
    Write-Output "$mode passed: $($report.'test-run'.passed) tests"
}
