# WeddingCouple v9 - save the editable work file and export one joined mesh per character for Unity.
import bpy, bmesh, os
from mathutils import Matrix

SRC = r"D:\UnityProjects\JSHWWedding\BlenderSource"
OUT = r"D:\UnityProjects\JSHWWedding\Assets\Models\WeddingCouple"

for n in ("Groom", "Bride"):
    ob = bpy.data.objects.get(n)
    if ob:
        bpy.data.objects.remove(ob, do_unlink=True)
for rn in ("Bride_Root", "Groom_Root"):
    for ch in bpy.data.objects[rn].children:
        ch.hide_render = False
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(SRC, "WeddingCouple_v9.blend"), relative_remap=False)

dg = bpy.context.evaluated_depsgraph_get()
report = {}
for name, rn in (("Groom", "Groom_Root"), ("Bride", "Bride_Root")):
    root = bpy.data.objects[rn]
    bm = bmesh.new()
    expect = 0
    for ch in root.children:
        ev = ch.evaluated_get(dg)
        tmp = ev.to_mesh()
        expect += len(tmp.vertices)
        tmp.transform(ch.matrix_local)          # parts are authored in root-local space (identity today)
        bm.from_mesh(tmp)                        # appends
        ev.to_mesh_clear()
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    assert len(me.vertices) == expect, (len(me.vertices), expect)
    used = {p.material_index for p in me.polygons}
    me.materials.append(bpy.data.materials["WeddingCouple_Palette"])
    if 1 in used:
        me.materials.append(bpy.data.materials["WeddingCouple_Veil"])
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    for o in bpy.context.view_layer.objects:
        o.select_set(o is ob)
    bpy.context.view_layer.objects.active = ob
    path = os.path.join(OUT, name + ".fbx")
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={"MESH"},
                             apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
                             bake_space_transform=True, mesh_smooth_type="FACE", use_mesh_modifiers=True,
                             add_leaf_bones=False, bake_anim=False, path_mode="AUTO", embed_textures=False)
    tris = sum(len(p.vertices) - 2 for p in me.polygons)
    bb = [ob.matrix_world @ v.co for v in me.vertices]
    report[name] = dict(verts=len(me.vertices), tris=tris, mats=[m.name for m in me.materials],
                        height=round(max(v.z for v in bb), 3), minz=round(min(v.z for v in bb), 3),
                        miny=round(min(v.y for v in bb), 3), maxy=round(max(v.y for v in bb), 3),
                        size_kb=os.path.getsize(path) // 1024)
    bpy.data.objects.remove(ob, do_unlink=True)
    bpy.data.meshes.remove(me)
print(report)
