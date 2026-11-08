"""Blender: convert the selected local world exports, keeping gameplay clips."""
import bpy,json,pathlib,re,shutil,sys
ROOT=pathlib.Path(__file__).resolve().parent.parent
for entry in json.loads((ROOT/'artifacts/minify-world/manifest.json').read_text()):
 if '--only' in sys.argv and entry['key'] not in sys.argv[sys.argv.index('--only')+1:]:continue
 bpy.ops.wm.read_factory_settings(use_empty=True)
 source=ROOT/entry['gltf'];data=json.loads(source.read_text());clips=[]
 for term,limit in [('idle',1),('run',1),('walk',1),('attack',2),('death',1)]:
  choices=[a for a in data.get('animations',[]) if term in a.get('name','').lower() and not a['name'].startswith('@') and not any(t in a['name'].lower() for t in ['additive','gesture','portrait','loadout','injured','turn','90','180','179','fast','rare'])]
  choices.sort(key=lambda a:('haste' in a['name'],len(a['name']),a['name']))
  for a in choices[:limit]:
   if a not in clips:clips.append(a)
 data['animations']=clips;compact=source.with_name('gameplay.gltf');compact.write_text(json.dumps(data))
 bpy.ops.import_scene.gltf(filepath=str(compact),import_pack_images=False)
 for o in list(bpy.context.scene.objects):
  if o.type=='MESH' and any(t in o.name.lower() for t in ['physics','collision','hitbox']):bpy.data.objects.remove(o,do_unlink=True)
 if entry['key'].startswith('Tree'):
  clips=[]
  for o in list(bpy.context.scene.objects):
   if o.type=='MESH' and any(t in o.name.lower() for t in ['stump','inspector']):bpy.data.objects.remove(o,do_unlink=True)
  for o in list(bpy.context.scene.objects):
   if o.type!='MESH':continue
   bpy.context.view_layer.objects.active=o
   if o.data.shape_keys:o.shape_key_clear()
   for modifier in list(o.modifiers):
    if modifier.type=='ARMATURE':bpy.ops.object.modifier_apply(modifier=modifier.name)
   world=o.matrix_world.copy();o.parent=None;o.matrix_world=world
  for o in list(bpy.context.scene.objects):
   if o.type=='ARMATURE':bpy.data.objects.remove(o,do_unlink=True)
 out=ROOT/'Assets/Arena/Resources/DotaImported/World'/entry['key'];out.mkdir(parents=True,exist_ok=True)
 materials=[]
 def texture(slot):
  return data['images'][data['textures'][slot['index']]['source']]['uri'] if slot else ''
 for mat in data.get('materials',[]):
  albedo=texture(mat.get('pbrMetallicRoughness',{}).get('baseColorTexture'));normal=texture(mat.get('normalTexture'))
  for name in [albedo,normal]:
   if name:shutil.copy2(source.parent/name,out/pathlib.Path(name).name)
  vmat=mat.get('extras',{}).get('vmat',{})
  materials.append(dict(name=mat['name'],albedo=pathlib.Path(albedo).name,normal=pathlib.Path(normal).name if normal else '',alphaMode=mat.get('alphaMode','OPAQUE'),cutoff=mat.get('alphaCutoff',.5),hidden='nothing' in albedo.lower() or vmat.get('Name')=='blank.vmat'))
 bpy.ops.object.select_all(action='SELECT')
 bpy.ops.export_scene.fbx(filepath=str(out/'Model.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=bool(clips),bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=.2,path_mode='RELATIVE',axis_forward='-Z',axis_up='Y')
 (out/'source.json').write_text(json.dumps(dict(**entry,materials=materials,animations=[a['name'] for a in clips]),indent=2))
 print('WORLD_CONVERT_OK',entry['key'],[a['name'] for a in clips],flush=True)
