"""Run with Blender --background --factory-startup --python this-file.
Converts local Source 2 Viewer glTF exports to Unity FBX, retaining a shared rig.
"""
import bpy, json, pathlib, shutil, re
ROOT=pathlib.Path(__file__).resolve().parent.parent
SOURCE=ROOT/'artifacts/dota-export/models/heroes'
OUTPUT=ROOT/'Assets/Arena/Resources/DotaImported'
HEROES=[
 ('broodmother',['broodmother_abdomen','broodmother_hair','broodmother_legs']),
 ('shadow_fiend',['shadow_fiend_arms','shadow_fiend_head','shadow_fiend_shoulders','shadow_fiend_wings']),
 ('lanaya',['lanaya_bracers_skirt','lanaya_cowl_shoulder','lanaya_hair']),
 ('pudge',['back','belt','bracer','hair','leftarm','leftweapon','righthook','weapon']),
 ('spirit_breaker',['spirit_breaker_bracers','spirit_breaker_head','spirit_breaker_horns','spirit_breaker_lower_armor','spirit_breaker_shoulders','spirit_breaker_tail','spirit_breaker_weapon']),
 ('furion',['furion_beard','furion_bracer','furion_cape','furion_horns','furion_necklace','furion_staff'])
]
PREFERRED={
 'broodmother':['broodmother_idle_2022','broodmother_run_2022','broodmother_attack_2022','broodmother_attack1_right_2022','broodmother_attack2_left_2022','broodmother_cast1_spawn_spiderlings_2022','broodmother_cast2_spin_web_2022','bm_2022_death','bm_2022_generic_channel'],
 'shadow_fiend':['idle_anim','run','attack01_anim','attack02_anim','cast1_shadowraze_a_anim','cast1_shadowraze_b_anim','cast1_shadowraze_c_anim','cast6_requiem_anim','death','generic_channel'],
 'lanaya':['idle_anim','run','attack_01_anim','attack_L_anim','lanaya_attackb_anim','meld_attack_anim','death','generic_channel'],
 'pudge':['pudge_idle_anim','pudge_walkN_anim','pudge_attack1_anim','pudge_attack2_anim','pudge_meathook_start_anim','pudge_meathook_end','pudge_death','generic_channel'],
 'spirit_breaker':['idle_anim','run_anim','attack_anim','attack2_anim','attack_backhand_anim','charge_anim','ultimate_anim','death','generic_channel'],
 'furion':['idle_anim','run_anim','attack_anim','attack_altB_anim','attack_altC_anim','cast_sprout','cast_teleportation','cast_wrath_of_nature','death','generic_channel']
}
for hero,(name,parts) in enumerate(HEROES):
 bpy.ops.wm.read_factory_settings(use_empty=True)
 folder=SOURCE/name
 data=json.loads((folder/(name+'.gltf')).read_text())
 animations=data.get('animations',[])
 # Exclude additive gestures/taunts. Keep the full poses used by gameplay.
 wanted=[]
 for category,limit in [('idle',2),('run',1),('walk',1),('attack',3),('cast',2),('die',1),('death',1),('channel',1)]:
  choices=[a for a in animations if category in a.get('name','').lower() and not a.get('name','').startswith('@') and not any(t in a.get('name','').lower() for t in ['additive','gesture','taunt','injured','loadout','portrait','fast','versus','charge_attack'])]
  choices.sort(key=lambda a:(0 if name=='broodmother' and '2022' in a['name'] else 1,0 if a['name'] in [category,category+'_anim',name+'_'+category+'_anim'] else 1,len(a['name'])))
  for animation in choices[:limit]:
   if animation not in wanted:wanted.append(animation)
 preferred=[a for key in PREFERRED[name] for a in animations if a.get('name')==key]
 data['animations']=preferred if len(preferred)>=5 else wanted
 compact=folder/(name+'_gameplay.gltf');compact.write_text(json.dumps(data))
 bpy.ops.import_scene.gltf(filepath=str(compact),import_pack_images=False)
 rig=next(o for o in bpy.context.scene.objects if o.type=='ARMATURE')
 base_actions=list(bpy.data.actions)
 # All wearable parts use the hero's skeleton and bone names.
 for part in parts:
  before=set(bpy.context.scene.objects)
  bpy.ops.import_scene.gltf(filepath=str(folder/(part+'.gltf')),import_pack_images=False)
  added=set(bpy.context.scene.objects)-before
  for obj in added:
   if obj.type!='MESH':continue
   world=obj.matrix_world.copy()
   for mod in obj.modifiers:
    if mod.type=='ARMATURE':mod.object=rig
   obj.parent=rig;obj.matrix_world=world
  for obj in list(added):
   if obj.type=='ARMATURE':bpy.data.objects.remove(obj,do_unlink=True)
 for action in list(bpy.data.actions):
  if action not in base_actions:bpy.data.actions.remove(action)
 # Remove physics / no-render guide meshes, should a format version include them.
 for obj in list(bpy.context.scene.objects):
  if obj.type=='MESH' and any(t in obj.name.lower() for t in ['physics','collision','hitbox']):bpy.data.objects.remove(obj,do_unlink=True)
 out=OUTPUT/('Hero'+str(hero));out.mkdir(parents=True,exist_ok=True)
 material_info=[]
 for material in bpy.data.materials:
  maps={}
  if material.use_nodes:
   for node in material.node_tree.nodes:
    if node.type!='TEX_IMAGE' or not node.image:continue
    image=node.image;source=pathlib.Path(bpy.path.abspath(image.filepath))
    if source.exists():
     destination=out/source.name
     if not destination.exists():shutil.copy2(source,destination)
     image.filepath=str(destination)
     for link in node.outputs.get('Color',node.outputs[0]).links:
      maps[link.to_socket.name]=destination.name
     if not maps.get('base') and ('color' in source.name.lower() or 'diffuse' in source.name.lower()):maps['base']=destination.name
     if 'normal' in source.name.lower():maps['normal']=destination.name
  material_info.append({'name':material.name,'albedo':maps.get('base',''),'normal':maps.get('normal',''),'textures':maps})
 bpy.ops.object.select_all(action='SELECT')
 bpy.ops.export_scene.fbx(filepath=str(out/('Hero'+str(hero)+'.fbx')),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=.15,path_mode='RELATIVE',axis_forward='-Z',axis_up='Y')
 metadata={'hero':hero,'name':name,'animations':[a.name for a in base_actions],'materials':material_info,'meshes':[o.name for o in bpy.context.scene.objects if o.type=='MESH']}
 (out/'source.json').write_text(json.dumps(metadata,indent=2))
 print('DOTA_CONVERT_OK',hero,name,metadata['animations'],flush=True)
