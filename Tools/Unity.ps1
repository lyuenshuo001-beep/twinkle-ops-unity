param([ValidateSet('Setup','Test','PlayTest','ValidateArt','Assemble','Windows','Web','ValidateWindowsBuild','ValidateWebBuild')][string]$Task='Setup')
$ErrorActionPreference='Stop'
$project=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$unity='D:\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe'
New-Item -ItemType Directory -Path (Join-Path $project 'Logs') -Force | Out-Null
$methods=@{Setup='Initialize';ValidateArt='ValidateContent';Assemble='AssembleRaid';Windows='BuildWindows';Web='BuildWeb';ValidateWindowsBuild='BuildWindowsFoundation';ValidateWebBuild='BuildWebFoundation'}
$arguments=@('-batchmode','-projectPath',$project,'-logFile',(Join-Path $project "Logs\$Task.log"))
if($Task -eq 'Test') { $arguments+=@('-nographics','-runTests','-testPlatform','EditMode','-testResults',(Join-Path $project 'Logs\editmode-results.xml')) }
elseif($Task -eq 'PlayTest') { $arguments+=@('-nographics','-runTests','-testPlatform','PlayMode','-testResults',(Join-Path $project 'Logs\playmode-results.xml')) }
else { $arguments+=@('-quit','-executeMethod',"TwinkleOps.Editor.ProjectSetup.$($methods[$Task])") }
if($Task -match 'Web') { $arguments+=@('-buildTarget','WebGL') }
elseif($Task -match 'Windows') { $arguments+=@('-buildTarget','Win64') }
$proc=Start-Process -FilePath $unity -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait
if($proc.ExitCode -ne 0) { throw "Unity task $Task failed: $($proc.ExitCode). See Logs\$Task.log" }
if($Task -in @('Test','PlayTest')) {
    $resultFile=if($Task -eq 'Test') {'editmode-results.xml'} else {'playmode-results.xml'}
    [xml]$results=Get-Content -LiteralPath (Join-Path $project ('Logs\'+$resultFile))
    if($results.'test-run'.result -ne 'Passed') { throw 'Tests did not pass.' }
}
Write-Output "Unity $Task completed."
