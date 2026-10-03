# TBride review renders: front / 3-4 L / 3-4 R / side / back (+ optional closeups). Set TAG / VIEWS before exec.
import bpy, math, os
from mathutils import Vector
RD = r"D:\UnityProjects\JSHWWedding\BlenderSource\renders"
TAG = globals().get("TAG", "tb")
VIEWS = globals().get("VIEWS", ["front", "q34L", "q34R", "side", "back"])
sc = bpy.context.scene
root = bpy.data.objects["TBride_Root"]
pc = bpy.data.collections.get("WC_Preview")
for ob in list(pc.objects):
    bpy.data.objects.remove(ob, do_unlink=True)
cam_d = bpy.data.cameras.new("TB_Cam"); cam = bpy.data.objects.new("TB_Cam", cam_d); pc.objects.link(cam); sc.camera = cam
sc.render.film_transparent = True
sc.view_settings.view_transform = "Standard"
keep = {o.name for o in root.children_recursive} | {root.name}
saved = {o.name: o.hide_render for o in bpy.data.objects}


def area(name, loc, size, energy, tgt):
    ld = bpy.data.lights.new(name, "AREA"); ld.size = size; ld.energy = energy
    o = bpy.data.objects.new(name, ld); pc.objects.link(o); o.location = loc
    o.rotation_euler = (Vector(tgt) - Vector(loc)).to_track_quat("-Z", "Y").to_euler()


# yaw (camera direction around the character), target z, ortho scale or None for perspective, distance
V = {"front": (0, 0.72, 1.58, 6), "side": (90, 0.72, 1.58, 6), "sideL": (-90, 0.72, 1.58, 6), "back": (180, 0.72, 1.58, 6),
     "q34L": (-35, 0.72, None, 4.6), "q34R": (35, 0.72, None, 4.6),
     "hands": (0, 0.58, 0.42, 6), "handsL": (-40, 0.58, None, 1.6), "handsR": (40, 0.58, None, 1.6), "handsUnder": (0, 0.58, None, 1.4),
     "shoulderL": (-60, 0.72, None, 1.8), "shoulderR": (60, 0.72, None, 1.8), "head_back": (150, 1.15, None, 2.2), "head_side": (90, 1.1, 0.75, 6)}
try:
    for o in bpy.data.objects:
        if o.type in ("MESH", "CURVE", "EMPTY"):
            o.hide_render = o.name not in keep
    for view in VIEWS:
        yaw, zc, ortho, dist = V[view]
        for o in list(pc.objects):
            if o.type == "LIGHT":
                bpy.data.objects.remove(o, do_unlink=True)
        th = math.radians(yaw)
        fwd = Vector((math.sin(th), -math.cos(th), 0)); side = Vector((math.cos(th), math.sin(th), 0))
        tgt = root.location + Vector((0, -0.12 if view.startswith("hands") else 0, zc))
        area("key", tgt + fwd * 3 - side * 1.8 + Vector((0, 0, 2.2)), 3.0, 190, tgt)
        area("fill", tgt + fwd * 2.6 + side * 2.4 + Vector((0, 0, 0.6)), 3.0, 75, tgt)
        area("rim", tgt - fwd * 2.5 + Vector((0, 0, 2.0)), 2.5, 120, tgt)
        if ortho:
            cam_d.type = "ORTHO"; cam_d.ortho_scale = ortho; cam.location = tgt + fwd * dist
        else:
            cam_d.type = "PERSP"; cam_d.lens = 85
            cam.location = tgt + fwd * dist + Vector((0, 0, -0.35 if view == "handsUnder" else 0.12))
        small = ortho is not None and ortho < 1.0 or dist < 2.5
        sc.render.resolution_x, sc.render.resolution_y = (700, 700) if small else (540, 948)
        cam.rotation_euler = (tgt - cam.location).to_track_quat("-Z", "Y").to_euler()
        sc.render.filepath = os.path.join(RD, "%s_%s.png" % (TAG, view))
        bpy.ops.render.render(write_still=True)
finally:
    for n, h in saved.items():
        if n in bpy.data.objects:
            bpy.data.objects[n].hide_render = h
print("rendered", TAG, VIEWS)
