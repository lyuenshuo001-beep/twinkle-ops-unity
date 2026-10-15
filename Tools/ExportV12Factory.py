import bpy,pathlib,json
from mathutils import Vector,Matrix
bpy.ops.wm.read_factory_settings(use_empty=True);bpy.ops.import_scene.fbx(filepath=r'D:\GameProjects\TwinkleOps\SourceAssets\V12\factory.fbx')
out=pathlib.Path(r'D:\GameProjects\TwinkleOps\Assets\ThirdParty\V12\modular_factory_facade');records=[]
groups={'PanelSolid':['wall_standard_standard_01'],'PanelDoor':['wall_door_centered_large_01'],'PanelWindow':['wall_window_centered_large_01','window_centered_large_01'],'Cornice':['cornice01_standard_standard_01'],'Crown':['crown_standard_standard_01'],'Base':['base_standard_standard_01']}
for name,names in groups.items():
 bpy.ops.object.select_all(action='DESELECT');originals=[bpy.data.objects[n] for n in names];anchor=originals[0].location.copy();copies=[]
 for o in originals:
  n=o.copy();n.data=o.data.copy();bpy.context.collection.objects.link(n);n.matrix_world=o.matrix_world.copy();n.location-=anchor;copies.append(n)
 for n in copies:n.select_set(True)
 bpy.context.view_layer.objects.active=copies[0]
 # Apply the complete source transform, then shift wall centre to X=0 and floor to Z=0.
 bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 pts=[n.matrix_world@Vector(p) for n in copies for p in n.bound_box];mid=(min(v.x for v in pts)+max(v.x for v in pts))/2;bottom=min(v.z for v in pts)
 for n in copies:n.location.x-=mid;n.location.z-=bottom
 bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
 bpy.ops.export_scene.fbx(filepath=str(out/(name+'.fbx')),use_selection=True,add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y')
 records.append({'name':name,'parts':names,'triangles':sum(len(p.vertices)-2 for n in copies for p in n.data.polygons)})
 for n in copies:bpy.data.objects.remove(n,do_unlink=True)
(out/'MODULES.json').write_text(json.dumps(records,indent=2));print(records)
