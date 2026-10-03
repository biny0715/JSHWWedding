# WeddingCouple v2 - review renders (neutral background, soft light). Set TAG / VIEWS before exec if needed.
import bpy, math, os
from mathutils import Vector

RDIR = r"D:\UnityProjects\JSHWWedding\BlenderSource\renders"
os.makedirs(RDIR, exist_ok=True)
TAG = globals().get("TAG", "it")
VIEWS = globals().get("VIEWS", ["front", "q34", "side", "back", "face", "face34"])
HIDE = globals().get("HIDE", [])          # part-name substrings hidden in this render pass (e.g. ["Hair", "Veil"])
WHO = globals().get("WHO", ["bride", "groom"])
sc = bpy.context.scene

pc = bpy.data.collections.get("WC_Preview")
if pc is None:
    pc = bpy.data.collections.new("WC_Preview")
    sc.collection.children.link(pc)
for ob in list(pc.objects):
    bpy.data.objects.remove(ob, do_unlink=True)


def area(name, loc, size, energy, target=(0, 0, 0.8)):
    ld = bpy.data.lights.new(name, "AREA")
    ld.size = size
    ld.energy = energy
    ob = bpy.data.objects.new(name, ld)
    pc.objects.link(ob)
    ob.location = loc
    d = Vector(target) - Vector(loc)
    ob.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
    return ob


cam_d = bpy.data.cameras.new("WC_Cam")
cam = bpy.data.objects.new("WC_Cam", cam_d)
pc.objects.link(cam)
sc.camera = cam

w = sc.world or bpy.data.worlds.new("World")
sc.world = w
w.use_nodes = True
bg = w.node_tree.nodes.get("Background")
bg.inputs[0].default_value = (0.36, 0.36, 0.37, 1)
bg.inputs[1].default_value = 1.0
sc.render.engine = "BLENDER_EEVEE"
sc.view_settings.view_transform = "Standard"
sc.view_settings.look = "None"
sc.render.film_transparent = globals().get("TRANSPARENT", True)   # composited on black like the reference


def shoot(name, root, mode):
    rx = bpy.data.objects[root].location.x
    for rn in ("Bride_Root", "Groom_Root"):  # render one character at a time
        for ch in bpy.data.objects[rn].children:
            ch.hide_render = rn != root or any(h in ch.name for h in HIDE)
    for ob in list(pc.objects):
        if ob.type == "LIGHT":
            bpy.data.objects.remove(ob, do_unlink=True)
    yaw = {"front": 0, "q34": 35, "side": 90, "back": 180, "face": 0, "face34": 35, "face34L": -35, "faceside": 90, "sideL": -90, "hand": 25, "shoulder": 20, "waist": 15, "hands": 0, "hem": 20}[mode]
    th = math.radians(yaw)
    fwd = Vector((math.sin(th), -math.cos(th), 0))  # camera position direction from the character
    tgt = Vector((rx, 0, 1.12 if mode.startswith("face") else 0.76))
    if mode == "hand":
        tgt = Vector((rx + 0.23, -0.02, 0.45))
    if mode == "shoulder":
        tgt = Vector((rx, -0.02, 0.80))
    if mode == "waist":
        tgt = Vector((rx, -0.02, 0.45))
    if mode == "hands":
        tgt = Vector((rx, -0.15, 0.58))
    if mode == "hem":
        tgt = Vector((rx, 0.0, 0.62))
    # soft key (upper front-left), fill (right), rim (back) - relative to the camera direction
    side = Vector((math.cos(th), math.sin(th), 0))
    clay = bool(globals().get("CLAY"))
    area("key", tgt + fwd * 3.0 - side * 1.8 + Vector((0, 0, 2.2)), 1.5 if clay else 3.0, 260 if clay else 190, tgt)
    area("fill", tgt + fwd * 2.6 + side * 2.4 + Vector((0, 0, 0.6)), 3.0, 25 if clay else 75, tgt)
    area("rim", tgt - fwd * 2.5 + Vector((0, 0, 2.0)), 2.5, 120, tgt)
    if mode in ("front", "side", "sideL", "back", "face", "faceside"):
        cam_d.type = "ORTHO"
        cam_d.ortho_scale = 0.62 if mode.startswith("face") else 1.58
        cam.location = tgt + fwd * 6.0
        if not mode.startswith("face"):
            cam.location.z = 0.76
        sc.render.resolution_x, sc.render.resolution_y = (700, 700) if mode.startswith("face") else (540, 948)
    else:
        cam_d.type = "PERSP"
        cam_d.lens = 85 if mode == "q34" else 120
        dist = {"q34": 4.6, "hand": 1.2, "shoulder": 1.9, "waist": 2.0, "hands": 1.3, "hem": 3.2}.get(mode, 2.4)
        cam.location = tgt + fwd * dist + Vector((0, 0, 0.25 if mode == "q34" else 0.05))
        sc.render.resolution_x, sc.render.resolution_y = (700, 700) if mode.startswith("face") else (600, 948)
    d = tgt - cam.location
    if mode in ("front", "side", "back"):
        d = Vector((d.x, d.y, 0))
    cam.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()
    sc.render.filepath = os.path.join(RDIR, "%s_%s_%s%s%s.png" % (TAG, name, mode, "_nohair" if HIDE else "", "_clay" if globals().get("CLAY") else ""))
    bpy.ops.render.render(write_still=True)


# CLAY: neutral grey material on everything (face paint / hair colour hidden) to judge the mesh form only
vl = bpy.context.view_layer
if globals().get("CLAY"):
    cm = bpy.data.materials.get("WC_Clay") or bpy.data.materials.new("WC_Clay")
    cm.use_nodes = True
    bs = next(nd for nd in cm.node_tree.nodes if nd.type == "BSDF_PRINCIPLED")
    bs.inputs["Base Color"].default_value = (0.30, 0.30, 0.31, 1)
    bs.inputs["Roughness"].default_value = 0.6
    vl.material_override = cm
else:
    vl.material_override = None
for name, root in (("bride", "Bride_Root"), ("groom", "Groom_Root")):
    if name not in WHO:
        continue
    for mode in VIEWS:
        shoot(name, root, mode)
vl.material_override = None
print("rendered", TAG, VIEWS)
