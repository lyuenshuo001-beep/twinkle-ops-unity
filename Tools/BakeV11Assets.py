import bpy,pathlib,math
root=pathlib.Path(r'D:\GameProjects\TwinkleOps\SourceAssets\V11')
for name,filename in [('Safe','Safe.blend'),('M870','Shotgun.blend')]:
 folder=root/name;bpy.ops.wm.open_mainfile(filepath=str(folder/filename),load_ui=False,use_scripts=False)
 meshes=[o for o in bpy.context.scene.objects if o.type=='MESH' and len(o.data.polygons)>0]
 bpy.ops.object.select_all(action='DESELECT')
 for o in meshes:o.select_set(True)
 bpy.context.view_layer.objects.active=meshes[0];bpy.ops.object.convert(target='MESH');bpy.ops.object.join();obj=bpy.context.object
 bpy.ops.object.transform_apply(location=False,rotation=False,scale=True)
 bpy.ops.object.mode_set(mode='EDIT');bpy.ops.mesh.select_all(action='SELECT');bpy.ops.uv.smart_project(angle_limit=1.15,island_margin=.025);bpy.ops.object.mode_set(mode='OBJECT')
 scene=bpy.context.scene;scene.render.engine='CYCLES';scene.cycles.samples=8;scene.render.bake.margin=8;scene.render.bake.use_clear=True
 material_info=[]
 for mat in obj.data.materials:
  color=tuple(mat.diffuse_color);wood='Wood' in mat.name
  metal=.0 if wood or mat.name in ['Material.002','Material.004'] else .8
  mat.use_nodes=True;nodes=mat.node_tree.nodes;nodes.clear();links=mat.node_tree.links
  output=nodes.new('ShaderNodeOutputMaterial');emit=nodes.new('ShaderNodeEmission');links.new(emit.outputs[0],output.inputs[0])
  coord=nodes.new('ShaderNodeTexCoord');mapping=nodes.new('ShaderNodeVectorMath');mapping.operation='MULTIPLY';mapping.inputs[1].default_value=(38,1.5,38) if wood else (12,12,12);links.new(coord.outputs['Generated'],mapping.inputs[0])
  noise=nodes.new('ShaderNodeTexNoise');noise.inputs['Scale'].default_value=6;noise.inputs['Detail'].default_value=3;noise.inputs['Roughness'].default_value=.75;links.new(mapping.outputs[0],noise.inputs[0])
  ramp=nodes.new('ShaderNodeValToRGB');ramp.color_ramp.elements[0].position=.22;ramp.color_ramp.elements[1].position=.8
  ramp.color_ramp.elements[0].color=tuple(c*.38 for c in color[:3])+(1,);ramp.color_ramp.elements[1].color=tuple(c*.9 for c in color[:3])+(1,);links.new(noise.outputs['Fac'],ramp.inputs[0])
  tex=nodes.new('ShaderNodeTexImage');nodes.active=tex
  material_info.append((mat,emit,ramp,noise,tex,metal))
 for channel in ['diff','rough','metal']:
  im=bpy.data.images.new(name+'_'+channel,width=1024,height=1024,alpha=False)
  if channel!='diff':im.colorspace_settings.name='Non-Color'
  for mat,emit,ramp,noise,tex,metal in material_info:
   for link in list(emit.inputs[0].links):mat.node_tree.links.remove(link)
   if channel=='diff':mat.node_tree.links.new(ramp.outputs[0],emit.inputs[0])
   elif channel=='rough':mat.node_tree.links.new(noise.outputs['Fac'],emit.inputs[0])
   else:emit.inputs[0].default_value=(metal,metal,metal,1)
   tex.image=im;mat.node_tree.nodes.active=tex
  bpy.ops.object.bake(type='EMIT');im.filepath_raw=str(folder/(channel+'.png'));im.file_format='PNG';im.save()
 bpy.ops.export_scene.fbx(filepath=str(folder/(name+'.fbx')),use_selection=True,object_types={'MESH'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y')
 print('BAKED',name,'triangles',sum(len(p.vertices)-2 for p in obj.data.polygons),flush=True)
