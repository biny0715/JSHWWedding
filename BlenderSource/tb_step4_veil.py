# TBride step 4: new veil mesh (comb at the top-back of the head, falls behind/beside the body),
# vertical folds, wavy hem, lace texture, translucent white material. Re-runnable.
import bpy, bmesh, math, os
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree

TAU = 2 * math.pi
P = globals().get("VEIL_PARAMS", {})
OUT_DIR = r"D:\UnityProjects\JSHWWedding\Assets\Models\WeddingBrideTripo"
os.makedirs(OUT_DIR, exist_ok=True)
col = bpy.data.collections["TBride_Work"]
root = bpy.data.objects["TBride_Root"]
old = bpy.data.objects.get("TBride_Veil")
if old:
    bpy.data.objects.remove(old, do_unlink=True)

# ---------------- lace texture (white RGB, alpha = tulle + lace), 1024 x 512 ~ 1.6 mm/px both ways
W, H = 1024, 512
Y, X = np.mgrid[0:H, 0:W].astype(np.float32) + 0.5          # Y=0 is the hem row (image bottom in Blender)
a = np.full((H, W), P.get("tulle", 0.20), np.float32)
a += 0.06 * np.clip(1 - np.hypot((X % 5) - 2.5, (Y % 5) - 2.5) / 1.1, 0, 1)        # fine net
per = 48.0
dx = (X % per) - per / 2
yb = 10 - 7 * np.sqrt(np.clip(1 - (dx / (per / 2)) ** 2, 0, None))                 # scalloped hem edge
band_top = 70
inband = (Y >= yb) & (Y < band_top)
lace = np.zeros_like(a)
fx, fy = dx, Y - 34
for k in range(6):
    ang = TAU * k / 6 + 0.3
    lace = np.maximum(lace, np.clip(1 - np.hypot(fx - 8 * math.cos(ang), fy - 8 * math.sin(ang)) / 5.5, 0, 1))
lace = np.maximum(lace, 0.8 * np.clip(1 - np.hypot(fx, fy) / 3.5, 0, 1))
for sgn in (-1, 1):                                                                   # leaves
    lx, ly = dx - sgn * 18, Y - 22
    lace = np.maximum(lace, 0.9 * np.clip(1 - np.hypot(lx / 7, ly / 3), 0, 1))
vine = np.clip(1 - np.abs(Y - 14 - 4 * np.sin(TAU * X / per)) / 1.4, 0, 1)
lace = np.maximum(lace, 0.75 * vine * (Y < 26))
lace = np.maximum(lace, 0.6 * (((X % 12) - 6) ** 2 + (Y - 58) ** 2 < 4))            # sprig dots row
a = np.where(inband, np.maximum(a, 0.28 + 0.6 * lace), a)
a = np.where((Y >= yb) & (Y < yb + 3), 0.9, a)                                       # corded edge
a = np.where(((Y >= band_top) & (Y < band_top + 2)), np.maximum(a, 0.55), a)        # band top line
a = np.where(Y < yb, 0.0, a)
side = np.minimum(X, W - 1 - X)                                                      # thin lace along the front edges
a = np.where(side < 6, np.maximum(a, 0.5), a)
img_np = np.ones((H, W, 4), np.float32); img_np[..., 3] = np.clip(a, 0, 1)
img = bpy.data.images.get("TBride_VeilLace")
if img:
    bpy.data.images.remove(img)
img = bpy.data.images.new("TBride_VeilLace", W, H, alpha=True)
img.alpha_mode = "STRAIGHT"
img.pixels.foreach_set(img_np.ravel())
img.filepath_raw = os.path.join(OUT_DIR, "Bride_VeilLace.png"); img.file_format = "PNG"; img.save()

# ---------------- collision proxy: posed body + hands (root-local)
dg = bpy.context.evaluated_depsgraph_get()
bm = bmesh.new()
for n in ("TBride_Body", "TBride_Hands"):
    o = bpy.data.objects[n]
    tmp = o.evaluated_get(dg).to_mesh()
    tmp.transform(o.matrix_local)
    bm.from_mesh(tmp)
    o.evaluated_get(dg).to_mesh_clear()
bvh = BVHTree.FromBMesh(bm)
bm.free()
GAP = P.get("gap", 0.014)


def push(q):
    loc, nrm, _, dist = bvh.find_nearest(q)
    if loc is None:
        return q
    d = q - loc
    if d.dot(nrm) < 0 or dist < GAP:
        return loc + nrm * GAP
    return q


# ---------------- veil surface
NU, NV = P.get("nu", 64), P.get("nv", 44)
cols = []
for iu in range(NU + 1):
    u = 2 * iu / NU - 1
    th = u * math.pi / 2 * P.get("comb_span", 0.92)
    # comb line over the top-back of the head (behind the hairline, in front of the bun)
    C = Vector((0.305 * math.sin(th), P.get("comb_y", 0.07) + 0.07 * (1 - math.cos(th)), 1.20 + 0.255 * math.cos(th)))
    be = u * math.radians(P.get("hem_span", 112))
    R = P.get("hem_r", 0.40)
    Bp = Vector((R * 1.08 * math.sin(be), P.get("hem_y", 0.10) + R * math.cos(be), P.get("hem_z", 0.50) + P.get("hem_rise", 0.20) * abs(u) ** 2.0))
    col_ = []
    for iv in range(NV + 1):
        v = iv / NV
        e = 1 - (1 - v) ** 2.3                     # falls outward first, then hangs
        p = Vector((C.x + (Bp.x - C.x) * e, C.y + (Bp.y - C.y) * e, C.z + (Bp.z - C.z) * v))
        col_.append(p)
    cols.append(col_)
for it in range(12):                              # collide + relax so the cloth drapes over head / shoulders
    for iu in range(NU + 1):
        c = [push(q) for q in cols[iu]]
        if it < 11:
            c = [c[0]] + [(c[i - 1] + 2 * c[i] + c[i + 1]) / 4 for i in range(1, NV)] + [c[-1]]
        cols[iu] = c
rows = []
for iv in range(NV + 1):
    v = iv / NV
    row = []
    for iu in range(NU + 1):
        u = iu / NU
        q = cols[iu][iv].copy()
        rad = Vector((q.x, q.y - 0.05, 0))
        if rad.length > 1e-6:
            fold = 0.6 * math.sin(TAU * 7 * u + 0.5) + 0.4 * math.sin(TAU * 13 * u + 1.9 + 0.7 * math.sin(TAU * 2 * u))
            q += rad.normalized() * (P.get("fold", 0.026) * v ** 1.3 * fold)
        q.z += P.get("wave", 0.012) * max(0.0, (v - 0.85) / 0.15) * math.sin(TAU * 16 * u)
        row.append(push(q))
    rows.append(row)
bm = bmesh.new()
uvl = bm.loops.layers.uv.new("UVMap")
vr = [[bm.verts.new(p) for p in r] for r in rows]
for iv in range(NV):
    for iu in range(NU):
        f = bm.faces.new((vr[iv][iu], vr[iv][iu + 1], vr[iv + 1][iu + 1], vr[iv + 1][iu]))
        for l, (a_, b_) in zip(f.loops, ((iu, iv), (iu + 1, iv), (iu + 1, iv + 1), (iu, iv + 1))):
            l[uvl].uv = (a_ / NU, 1 - b_ / NV)
for f in bm.faces:
    f.smooth = True
bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
me = bpy.data.meshes.new("TBride_Veil"); bm.to_mesh(me); bm.free()
veil = bpy.data.objects.new("TBride_Veil", me); col.objects.link(veil); veil.parent = root

mat = bpy.data.materials.get("TBride_Veil_Mat") or bpy.data.materials.new("TBride_Veil_Mat")
mat.use_nodes = True
nt = mat.node_tree; nt.nodes.clear()
out = nt.nodes.new("ShaderNodeOutputMaterial"); bs = nt.nodes.new("ShaderNodeBsdfPrincipled"); tx = nt.nodes.new("ShaderNodeTexImage")
tx.image = img
bs.inputs["Base Color"].default_value = (1, 1, 1, 1)
bs.inputs["Roughness"].default_value = 0.8
nt.links.new(tx.outputs["Alpha"], bs.inputs["Alpha"])
nt.links.new(bs.outputs["BSDF"], out.inputs["Surface"])
mat.use_backface_culling = False
try:
    mat.surface_render_method = "BLENDED"
except Exception:
    mat.blend_method = "BLEND"
me.materials.append(mat)
co = np.array([v.co[:] for v in me.vertices])
print("veil verts", len(me.vertices), "bounds", co.min(0).round(3), co.max(0).round(3))
