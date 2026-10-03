# TBride (Tripo base) step 1: place a copy of the v9 bouquet in front of the waist (v9 object is untouched).
import bpy
from mathutils import Matrix, Vector
col = bpy.data.collections["TBride_Work"]
root = bpy.data.objects["TBride_Root"]
src = bpy.data.objects["B_Bouquet"]
old = bpy.data.objects.get("TBride_Bouquet")
if old:
    bpy.data.objects.remove(old, do_unlink=True)
dg = bpy.context.evaluated_depsgraph_get()
me = bpy.data.meshes.new_from_object(src.evaluated_get(dg))
me.name = "TBride_Bouquet"
# v9 stems sit at y -0.178 in front of a slimmer dress; the Tripo dress front is ~4 cm further forward
me.transform(Matrix.Translation(Vector((0.0, -0.037, 0.015))))
ob = bpy.data.objects.new("TBride_Bouquet", me)
col.objects.link(ob)
ob.parent = root
print("bouquet tris", sum(len(p.vertices) - 2 for p in me.polygons), "mats", [m.name for m in me.materials])
