"""Read cosmetic particle definitions and textures from the local Minify overlays.
Source 2 definitions are kept for reference; Unity emitters are authored separately.
"""
import importlib.util,pathlib,re,json,subprocess
spec=importlib.util.spec_from_file_location('local_vpk',pathlib.Path(__file__).with_name('export-minify-heroes.py'))
vpk=importlib.util.module_from_spec(spec);spec.loader.exec_module(vpk)
ROOT=vpk.ROOT
archives=[vpk.Vpk(vpk.GAME/p) for p in ['dota_russian/pak66_dir.vpk','dota_russian/pak01_dir.vpk','dota/pak01_dir.vpk']]
roots=[
 'particles/units/heroes/hero_broodmother/broodmother_loadout.vpcf',
 'particles/units/heroes/hero_nevermore/nevermore_loadout.vpcf',
 'particles/units/heroes/hero_pudge/pudge_loadout.vpcf',
 'particles/units/heroes/hero_templar_assassin/templar_assassin_ambient.vpcf',
 'particles/econ/items/shadow_fiend/sf_fire_arcana/sf_fire_arcana_ambient.vpcf',
 'particles/econ/items/broodmother/bm_fall20_witches_grasp/bm_fall20_witches_grasp_ambient_candle.vpcf',
 'particles/econ/items/natures_prophet/natures_prophet_weapon_sufferwood/natures_prophet_sufferwood_ambient.vpcf',
 'particles/units/heroes/hero_spirit_breaker/spirit_breaker_ambient_head.vpcf',
 'materials/particle/snowflake/snowflakes_01.vtex',
]
destination=ROOT/'artifacts/particle-raw';cli=ROOT/'artifacts/tools/source2viewer20/Source2Viewer-CLI.exe'
for model in json.loads((ROOT/'artifacts/minify-skins-manifest.json').read_text()):
 result=subprocess.run([str(cli),'-i',str(ROOT/'artifacts/minify-game'/model['model']),'-b','DATA'],capture_output=True,text=True)
 roots.extend(re.findall(r'(?:particles|kisilev_ind)/[^\s\"]+\.vpcf',result.stdout))
(ROOT/'artifacts/cosmetic-particle-roots.json').write_text(json.dumps(sorted(set(roots)),indent=2))
queue=roots[:];manifest=[];seen=set()
while queue:
 name=queue.pop(0).replace('\\','/').removesuffix('_c');compiled=name+'_c'
 if name in seen:continue
 seen.add(name)
 if not name.endswith(('.vpcf','.vmat','.vtex')):continue
 archive=next((a for a in archives if compiled in a.entries),None)
 if not archive:continue
 path=destination/compiled;path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(archive.read(compiled))
 result=subprocess.run([str(cli),'-i',str(path),'-o',str(destination/name),'-d'],capture_output=True,text=True)
 if result.returncode:raise RuntimeError(name+' '+result.stdout[-500:])
 manifest.append({'asset':name,'package':str(archive.path),'crc':hex(archive.entries[compiled][0])})
 text_file=destination/name
 if text_file.exists():
  content=text_file.read_text(errors='replace')
  queue.extend(re.findall(r'(?:resource:)?"([^"\n]+\.(?:vpcf|vmat|vtex)(?:_c)?)"',content))
 if len(seen)>900:raise RuntimeError('Unexpected particle dependency fan-out')
(ROOT/'artifacts/cosmetic-particle-manifest.json').write_text(json.dumps(manifest,indent=2))
print('COSMETIC_EXPORT_OK',len(manifest),'resources;',sum(m['asset'].endswith('.vtex') for m in manifest),'textures')
