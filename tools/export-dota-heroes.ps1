param([string]$Package='E:/SteamLibrary/steamapps/common/dota 2 beta/game/dota/pak01_dir.vpk')
$ErrorActionPreference='Stop'
$projectRoot=Split-Path $PSScriptRoot -Parent
$cli=Join-Path $projectRoot 'artifacts/tools/source2viewer20/Source2Viewer-CLI.exe'
$target=Join-Path $projectRoot 'artifacts/dota-export'
$paths=@(
 'models/heroes/broodmother/broodmother.vmdl_c',
 'models/heroes/broodmother/broodmother_abdomen.vmdl_c',
 'models/heroes/broodmother/broodmother_hair.vmdl_c',
 'models/heroes/broodmother/broodmother_legs.vmdl_c',
 'models/heroes/shadow_fiend/shadow_fiend.vmdl_c',
 'models/heroes/shadow_fiend/shadow_fiend_arms.vmdl_c',
 'models/heroes/shadow_fiend/shadow_fiend_head.vmdl_c',
 'models/heroes/shadow_fiend/shadow_fiend_shoulders.vmdl_c',
 'models/heroes/shadow_fiend/shadow_fiend_wings.vmdl_c',
 'models/heroes/lanaya/lanaya.vmdl_c',
 'models/heroes/lanaya/lanaya_bracers_skirt.vmdl_c',
 'models/heroes/lanaya/lanaya_cowl_shoulder.vmdl_c',
 'models/heroes/lanaya/lanaya_hair.vmdl_c',
 'models/heroes/pudge/pudge.vmdl_c',
 'models/heroes/pudge/back.vmdl_c',
 'models/heroes/pudge/belt.vmdl_c',
 'models/heroes/pudge/bracer.vmdl_c',
 'models/heroes/pudge/hair.vmdl_c',
 'models/heroes/pudge/leftarm.vmdl_c',
 'models/heroes/pudge/leftweapon.vmdl_c',
 'models/heroes/pudge/righthook.vmdl_c',
 'models/heroes/pudge/weapon.vmdl_c',
 'models/heroes/spirit_breaker/spirit_breaker.vmdl_c',
 'models/heroes/spirit_breaker/spirit_breaker_bracers.vmdl_c',
 'models/heroes/spirit_breaker/spirit_breaker_head.vmdl_c',
 'models/heroes/spirit_breaker/spirit_breaker_horns.vmdl_c',
 'models/heroes/spirit_breaker/spirit_breaker_lower_armor.vmdl_c',
 'models/heroes/spirit_breaker/spirit_breaker_shoulders.vmdl_c',
 'models/heroes/spirit_breaker/spirit_breaker_tail.vmdl_c',
 'models/heroes/spirit_breaker/spirit_breaker_weapon.vmdl_c',
 'models/heroes/furion/furion.vmdl_c',
 'models/heroes/furion/furion_beard.vmdl_c',
 'models/heroes/furion/furion_bracer.vmdl_c',
 'models/heroes/furion/furion_cape.vmdl_c',
 'models/heroes/furion/furion_horns.vmdl_c',
 'models/heroes/furion/furion_necklace.vmdl_c',
 'models/heroes/furion/furion_staff.vmdl_c'
)
New-Item -ItemType Directory $target -Force | Out-Null
foreach($modelPath in $paths){
 Write-Output "EXPORT $modelPath"
 & $cli -i $Package -f $modelPath -o $target -d --gltf_export_format gltf --gltf_export_materials --gltf_textures_adapt --gltf_export_animations *> (Join-Path $target ((Split-Path $modelPath -Leaf)+'.log'))
 if($LASTEXITCODE -ne 0){throw "Export failed: $modelPath"}
}
Write-Output 'DOTA_EXPORT_OK'
