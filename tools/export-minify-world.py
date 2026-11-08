"""Read-only export of the installed Minify map/unit assets. Never patches Dota."""
import importlib.util,json,pathlib,subprocess,argparse
spec=importlib.util.spec_from_file_location('heroes',pathlib.Path(__file__).with_name('export-minify-heroes.py'))
h=importlib.util.module_from_spec(spec);spec.loader.exec_module(h)
ROOT=h.ROOT;private=ROOT/'artifacts/minify-game';out=ROOT/'artifacts/minify-world'
MODELS={
 'Tower0':('models/props_structures/radiant_tower002',6),
 'Tower1':('models/props_structures/dire_tower002',6),
 'Ancient0':('models/props_structures/radiant_ancient001',8),
 'Ancient1':('models/props_structures/dire_ancient_base001',7),
 'Barracks0':('models/props_structures/radiant_melee_barracks001',3.5),
 'Barracks1':('models/props_structures/dire_barracks_melee001',3.5),
 'Creep0M':('models/creeps/lane_creeps/creep_radiant_melee/radiant_melee',2.1),
 'Creep0R':('models/creeps/lane_creeps/creep_radiant_ranged/radiant_ranged',2.2),
 'Creep1M':('models/creeps/lane_creeps/creep_bad_melee/creep_bad_melee',2.1),
 'Creep1R':('models/creeps/lane_creeps/creep_bad_ranged/lane_dire_ranged',2.2),
 'Roshan':('models/creeps/roshan/roshan',6),
 'Courier':('models/props_gameplay/donkey',1.7),
 'Ward':('models/props_gameplay/default_ward',2.4),
 'Tree0A':('models/props_tree/tree_oak_01',5.8),
 'Tree0B':('models/props_tree/tree_pine_01',6.4),
 'Tree1A':('models/props_tree/dire_tree004',5.8),
 'Tree1B':('models/props_tree/dire_tree007',6.4),
}
def main():
 parser=argparse.ArgumentParser();parser.add_argument('--only',nargs='*');args=parser.parse_args()
 archives=[h.Vpk(p) for p in sorted((h.GAME/'dota_russian').glob('pak*_dir.vpk'),reverse=True)]+[h.Vpk(h.GAME/'dota/pak01_dir.vpk')]
 out.mkdir(parents=True,exist_ok=True);manifest=json.loads((out/'manifest.json').read_text()) if args.only and (out/'manifest.json').exists() else []
 for key,(name,height) in MODELS.items():
  if args.only and key not in args.only:continue
  compiled=name+'.vmdl_c';archive=next(a for a in archives if compiled in a.entries)
  search=private
  if key.startswith('Tree'):
   # Minify's remove-foliage mod intentionally blanks leaves. Restore the tree's original dependencies.
   archive=archives[-1];search=out/'base-game';search.mkdir(exist_ok=True)
   (search/'gameinfo.gi').write_text('"GameInfo" { FileSystem { SearchPaths { Game "'+(h.GAME/'dota').as_posix()+'" Game "'+(h.GAME/'core').as_posix()+'" } } }')
  file=search/compiled;file.parent.mkdir(parents=True,exist_ok=True);file.write_bytes(archive.read(compiled))
  dest=out/key;dest.mkdir(exist_ok=True)
  log=dest/'export.log'
  with log.open('w',encoding='utf8') as stream:
   result=subprocess.run([str(ROOT/'artifacts/tools/source2viewer20/Source2Viewer-CLI.exe'),'-i',str(file),'--game',str(search/'gameinfo.gi'),'-o',str(dest/'model.vmdl'),'-d','--gltf_export_format','gltf','--gltf_export_materials','--gltf_textures_adapt','--gltf_export_animations'],stdout=stream,stderr=subprocess.STDOUT)
  if result.returncode:raise RuntimeError(str(log))
  gltf=next(dest.glob('*.gltf'));data=json.loads(gltf.read_text())
  manifest=[e for e in manifest if e['key']!=key];manifest.append(dict(key=key,model=compiled,package=str(archive.path),crc=hex(archive.entries[compiled][0]),gltf=str(gltf.relative_to(ROOT)),height=height))
  print('WORLD_EXPORT',key,'meshes',len(data.get('meshes',[])),'animations',len(data.get('animations',[])),flush=True)
 (out/'manifest.json').write_text(json.dumps(manifest,indent=2))
 print('WORLD_EXPORT_OK',len(manifest),flush=True)
if __name__=='__main__':main()
