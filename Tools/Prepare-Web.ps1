param([string]$BuildDirectory='')
$ErrorActionPreference='Stop'
$project=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if(!$BuildDirectory) { $BuildDirectory=Join-Path $project 'Builds\Game\Web' }
foreach($name in @('Web.loader.js','Web.data.unityweb','Web.framework.js.unityweb','Web.wasm.unityweb')) { if(!(Test-Path (Join-Path $BuildDirectory "Build\$name"))) { throw "Missing Web build file: $name" } }
$version=(Get-FileHash (Join-Path $BuildDirectory 'Build\Web.data.unityweb') -Algorithm SHA256).Hash.Substring(0,12).ToLower()
$html=(Get-Content (Join-Path $PSScriptRoot 'WebTemplate.html') -Raw).Replace('__VERSION__',$version)
[IO.File]::WriteAllText((Join-Path $BuildDirectory 'index.html'),$html,[Text.UTF8Encoding]::new($false))
$credits=[Net.WebUtility]::HtmlEncode((Get-Content (Join-Path $project 'Docs\CREDITS.md') -Raw))
$credits=[regex]::Replace($credits,'https?://[^\s]+','<a href="$0">$0</a>')
$page='<!doctype html><html lang="zh-CN"><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>TWINKLE OPS · 资源署名</title><style>body{background:#10191c;color:#dbe5e1;font:15px/1.8 system-ui;max-width:920px;margin:40px auto;padding:24px}a{color:#acd7bd}pre{white-space:pre-wrap;overflow-wrap:anywhere}</style><a href="./">← 返回游戏</a><h1>资源来源与授权</h1><pre>'+$credits+'</pre></html>'
[IO.File]::WriteAllText((Join-Path $BuildDirectory 'credits.html'),$page,[Text.UTF8Encoding]::new($false))
[IO.File]::WriteAllText((Join-Path $BuildDirectory '.nojekyll'),'')
Write-Output "Web shell ready: $version"
