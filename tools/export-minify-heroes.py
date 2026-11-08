"""Read installed VPK overlays without modifying the game, then export six heroes.
The private search path gives Minify's package precedence over the base game.
"""
import argparse,json,pathlib,re,struct,subprocess,os,zlib
ROOT=pathlib.Path(__file__).resolve().parent.parent
GAME=pathlib.Path('E:/SteamLibrary/steamapps/common/dota 2 beta/game')
class Vpk:
 def __init__(self,path):
  self.path=path;self.entries={}
  data=path.read_bytes();signature,version,size=struct.unpack_from('<III',data)
  assert signature==0x55aa1234 and version in (1,2),path
  self.header=28 if version==2 else 12;self.tree_size=size;p=self.header
  def string():
   nonlocal p
   end=data.index(0,p);value=data[p:end].decode('utf-8');p=end+1;return value
  while True:
   extension=string()
   if not extension:break
   while True:
    directory=string()
    if not directory:break
    while True:
     name=string()
     if not name:break
     crc,preload,archive,offset,length,terminator=struct.unpack_from('<IHHIIH',data,p);p+=18
     assert terminator==65535
     prefix=data[p:p+preload];p+=preload
     key=((directory+'/' if directory!=' ' else '')+name+'.'+extension).replace('\\','/')
     self.entries[key]=(crc,archive,offset,length,prefix)
 def read(self,name):
  crc,archive,offset,length,prefix=self.entries[name]
  if archive==0x7fff:source=self.path;offset+=self.header+self.tree_size
  else:source=self.path.with_name(self.path.name.replace('_dir.vpk',f'_{archive:03}.vpk'))
  with source.open('rb') as stream:stream.seek(offset);content=prefix+stream.read(length)
  assert zlib.crc32(content)&0xffffffff==crc,name
  return content
def main():
 parser=argparse.ArgumentParser();parser.add_argument('--prepare-only',action='store_true');args=parser.parse_args()
 private=ROOT/'artifacts/minify-game';exports=ROOT/'artifacts/dota-skins-export';private.mkdir(parents=True,exist_ok=True);exports.mkdir(parents=True,exist_ok=True)
 base=Vpk(GAME/'dota/pak01_dir.vpk');mod_folder=GAME/'dota_russian'
 overlays=[Vpk(p) for p in sorted(mod_folder.glob('*_dir.vpk'),reverse=True) if p.name.startswith('pak')]
 assert overlays,'No Minify VPK packages found'
 # The viewer preloads pak01, so expose the effective overlay under that name.
 effective=overlays[0].path
 for source in effective.parent.glob(effective.name.replace('_dir.vpk','_*.vpk')):
  destination=private/source.name.replace(effective.stem.removesuffix('_dir'),'pak01',1)
  if not destination.exists():os.link(source,destination)
 gameinfo='"GameInfo"\n{\n FileSystem\n {\n  SearchPaths\n  {\n'
 for path in [private,mod_folder,GAME/'dota',GAME/'core']:gameinfo+='   Game "'+path.as_posix()+'"\n'
 gameinfo+='  }\n }\n}\n';(private/'gameinfo.gi').write_text(gameinfo)
 paths=re.findall(r"'(models/[^']+\.vmdl_c)'",(ROOT/'tools/export-dota-heroes.ps1').read_text())
 manifest=[]
 for name in paths:
  archive=next((v for v in overlays if name in v.entries),base)
  destination=private/name;destination.parent.mkdir(parents=True,exist_ok=True);destination.write_bytes(archive.read(name))
  entry={'model':name,'package':str(archive.path),'modified':archive is not base,'crc':hex(archive.entries[name][0])};manifest.append(entry)
 (ROOT/'artifacts/minify-skins-manifest.json').write_text(json.dumps(manifest,indent=2))
 print('MINIFY_PREPARED',len(paths),'models;',sum(e['modified'] for e in manifest),'overrides;',effective,flush=True)
 if args.prepare_only:return
 cli=ROOT/'artifacts/tools/source2viewer20/Source2Viewer-CLI.exe'
 for entry in manifest:
  name=entry['model'];log=exports/(pathlib.Path(name).stem+'.log')
  with log.open('w',encoding='utf-8') as output:
   result=subprocess.run([str(cli),'-i',str(private/name),'--game',str(private/'gameinfo.gi'),'-o',str(exports/name.removesuffix('_c')),'-d','--gltf_export_format','gltf','--gltf_export_materials','--gltf_textures_adapt','--gltf_export_animations'],stdout=output,stderr=subprocess.STDOUT)
  if result.returncode:raise RuntimeError(f'Export failed: {name}; see {log}')
  print('MINIFY_EXPORTED',name,flush=True)
 print('MINIFY_EXPORT_OK',flush=True)
if __name__=='__main__':main()
