$ErrorActionPreference='Stop'
$project=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$root=Join-Path $project 'SourceAssets\PolyHaven'
$headers=@{'User-Agent'='TwinkleOps-AssetSetup/0.1 (personal game project)'}
$ids=@('concrete_road_barrier','barrel_03','old_military_crate')
$recordPath=Join-Path $root 'download-checksums.json'
$records=if(Test-Path -LiteralPath $recordPath) { @(Get-Content -LiteralPath $recordPath -Raw | ConvertFrom-Json) } else { @() }
foreach($id in $ids) {
    $metadata=Invoke-RestMethod -Uri "https://api.polyhaven.com/info/$id" -Headers $headers
    $files=Invoke-RestMethod -Uri "https://api.polyhaven.com/files/$id" -Headers $headers
    $folder=Join-Path $root $id
    New-Item -ItemType Directory -Path $folder -Force | Out-Null
    $downloads=@{ 'model.fbx'=$files.fbx.'2k'.fbx }
    foreach($kind in @('Diffuse','nor_gl','Rough','Metal','AO')) {
        $map=$files.$kind.'2k'.jpg
        if($map) { $downloads["$kind.jpg"]=$map }
    }
    foreach($entry in $downloads.GetEnumerator()) {
        if(!$entry.Value.url) { throw "Missing file URL for $id / $($entry.Key)" }
        $out=Join-Path $folder $entry.Key
        if(Test-Path -LiteralPath $out) {
            if((Get-FileHash -LiteralPath $out -Algorithm MD5).Hash.ToLowerInvariant() -eq $entry.Value.md5) { continue }
            throw "Existing file hash mismatch: $out. Retained for inspection."
        }
        Invoke-WebRequest -Uri $entry.Value.url -Headers $headers -OutFile $out
        if((Get-FileHash -LiteralPath $out -Algorithm MD5).Hash.ToLowerInvariant() -ne $entry.Value.md5) { throw "Download checksum failed: $out" }
        $records += [pscustomobject]@{asset=$id;file=$entry.Key;url=$entry.Value.url;md5=$entry.Value.md5;sha256=(Get-FileHash -LiteralPath $out -Algorithm SHA256).Hash}
    }
    $metadata | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $folder 'source-info.json') -Encoding UTF8
    @("Source: https://polyhaven.com/a/$id","License: CC0 1.0 https://creativecommons.org/publicdomain/zero/1.0/","Publisher license: https://polyhaven.com/license","Downloaded: 2026-10-12","Authors: $(($metadata.authors.PSObject.Properties.Name) -join ', ')","Changes: texture selection at 2K; URP material conversion in project.") | Set-Content -LiteralPath (Join-Path $folder 'LICENSE-AND-SOURCE.txt') -Encoding UTF8
    Write-Output "Verified CC0 model: $id"
}
$records | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $root 'download-checksums.json') -Encoding UTF8
