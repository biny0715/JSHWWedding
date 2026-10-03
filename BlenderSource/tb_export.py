# TBride export: bake the posed rig, decimate the Tripo body for WebGL, write FBX + textures for Unity.
import bpy, os
OUT = r"D:\UnityProjects\JSHWWedding\Assets\Models\WeddingBrideTripo"
os.makedirs(OUT, exist_ok=True)
RATIO = globals().get("BODY_RATIO", 0.14)
root = bpy.data.objects["TBride_Root"]
bpy.ops.wm.save_as_mainfile(filepath=r"D:\UnityProjects\JSHWWedding\BlenderSource\WeddingCouple_v10.blend")
dg = bpy.context.evaluated_depsgraph_get()
body_mat = bpy.data.objects["TBride_Body"].data.materials[0]
# textures -> PNG next to the FBX (names stable for Unity)
names = {"Color": "Bride_Tripo_BaseColor.png", "ORM": "Bride_Tripo_ORM.png", "NormalGL": "Bride_Tripo_Normal.png"}
for n in body_mat.node_tree.nodes:
    if n.type == "TEX_IMAGE" and n.image:
        for k, fn in names.items():
            if n.image.name.startswith(k + "_"):
                im = n.image.copy(); im.filepath_raw = os.path.join(OUT, fn); im.file_format = "PNG"; im.save(); bpy.data.images.remove(im)
body_mat.name = "Bride_Tripo_Body"
tmp = []
for n in ("TBride_Body", "TBride_Hands", "TBride_Bouquet", "TBride_Veil"):
    o = bpy.data.objects[n]
    me = bpy.data.meshes.new_from_object(o.evaluated_get(dg))      # posed (armature applied)
    me.transform(o.matrix_local)
    t = bpy.data.objects.new("EXP_" + n, me); bpy.context.scene.collection.objects.link(t); tmp.append(t)
    if n == "TBride_Body":
        md = t.modifiers.new("dec", "DECIMATE"); md.ratio = RATIO; md.use_collapse_triangulate = True
vl = bpy.context.view_layer
for o in vl.objects:
    o.select_set(False)
for t in tmp:
    t.select_set(True)
vl.objects.active = tmp[0]
bpy.ops.object.modifier_apply(modifier="dec")
bpy.ops.object.join()
j = vl.objects.active; j.name = "Bride_Tripo"
tris = sum(len(p.vertices) - 2 for p in j.data.polygons)
path = os.path.join(OUT, "Bride_Tripo.fbx")
bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={"MESH"}, apply_scale_options="FBX_SCALE_ALL",
                         axis_forward="-Z", axis_up="Y", bake_space_transform=True, mesh_smooth_type="FACE",
                         add_leaf_bones=False, bake_anim=False, path_mode="AUTO", embed_textures=False)
print("tris", tris, "mats", [m.name for m in j.data.materials], "size_kb", os.path.getsize(path) // 1024)
bpy.data.objects.remove(j, do_unlink=True)
print(sorted(os.listdir(OUT)))
