# TBride step 2: rig the arms (2-bone IK), pose both hands onto the bouquet stems, rebuild the hands.
# Re-runnable: the body is rebuilt from the untouched Tripo original every time.
import bpy, bmesh, math
import numpy as np
from mathutils import Matrix, Vector
exec(open(r"D:\UnityProjects\JSHWWedding\BlenderSource\wc_lib.py", encoding="utf-8").read(), globals())

P = globals().get("ARM_PARAMS", {})
col = bpy.data.collections["TBride_Work"]
root = bpy.data.objects["TBride_Root"]
src = bpy.data.objects["Tripo_Bride_A_mesh"]
for n in ("TBride_Body", "TBride_Hands", "TBride_Rig", "TB_IK_L", "TB_IK_R", "TB_Pole_L", "TB_Pole_R"):
    o = bpy.data.objects.get(n)
    if o:
        bpy.data.objects.remove(o, do_unlink=True)
for a in [a for a in bpy.data.armatures if a.users == 0]:
    bpy.data.armatures.remove(a)
for m in [m for m in bpy.data.meshes if m.users == 0]:
    bpy.data.meshes.remove(m)

me = src.data.copy(); me.name = "TBride_Body"
M = src.matrix_world.copy(); M.translation -= root.location
me.transform(M)
body = bpy.data.objects.new("TBride_Body", me); col.objects.link(body); body.parent = root

YS = P.get("ys", -0.215)                       # stem bundle (x=0, y=YS)
ZC = {1: P.get("zL", 0.590), -1: P.get("zR", 0.560)}   # +x hand (character's left) above, -x below
J = {s: dict(S=Vector((s * 0.170, -0.040, 0.775)), E=Vector((s * 0.236, -0.041, 0.665)),
             W=Vector((s * 0.312, -0.043, 0.552)), T=Vector((s * 0.385, -0.040, 0.455))) for s in (1, -1)}

# ---- remove the mitten hands (past a plane just below the wrist) and close the hole
bm = bmesh.new(); bm.from_mesh(me)
kill = []
for s in (1, -1):
    W, T = J[s]["W"], J[s]["T"]
    d = (T - W).normalized(); cut = W + d * P.get("cut", 0.016)
    L = (T - W).length + 0.03
    for v in bm.verts:
        if v.co.x * s > 0.22 and 0.40 < v.co.z < 0.6:
            q = (v.co - cut).dot(d)
            # only the hand: past the wrist plane AND close to the wrist->fingertip axis (never the dress hem)
            if 0 < q < L and ((v.co - cut) - d * q).length < 0.07:
                kill.append(v)
bmesh.ops.delete(bm, geom=list(set(kill)), context="VERTS")
edges = [e for e in bm.edges if e.is_boundary]
bmesh.ops.holes_fill(bm, edges=edges, sides=0)
bm.to_mesh(me); bm.free()

# ---- vertex weights: distance to the arm axis, fading in at the shoulder, split at the elbow
co = np.empty(len(me.vertices) * 3); me.vertices.foreach_get("co", co); co = co.reshape(-1, 3)


def seg_param(p, a, b):
    ab = b - a
    t = np.clip(((p - a) @ ab) / (ab @ ab), 0, 1)
    return t, np.linalg.norm(p - (a + t[:, None] * ab), axis=1)


def sst(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1); return t * t * (3 - 2 * t)


groups = {}
for s, nm in ((1, "L"), (-1, "R")):
    S, E, W = (np.array(J[s][k]) for k in ("S", "E", "W"))
    tu, ru = seg_param(co, S, E)
    tf, rf = seg_param(co, E, W)
    r = np.minimum(ru, rf)
    along = np.where(ru <= rf, tu * np.linalg.norm(E - S), np.linalg.norm(E - S) + tf * np.linalg.norm(W - E))
    m = sst(P.get("r1", 0.080), P.get("r0", 0.050), r) * sst(0.0, P.get("fade", 0.07), along) * (co[:, 0] * s > 0.10)
    fore = sst(np.linalg.norm(E - S) - 0.025, np.linalg.norm(E - S) + 0.025, along)
    groups["upper." + nm] = m * (1 - fore)
    groups["fore." + nm] = m * fore
for gname, w in groups.items():
    vg = body.vertex_groups.new(name=gname)
    idx = np.where(w > 0.002)[0]
    for i in idx:
        vg.add([int(i)], float(w[i]), "REPLACE")

# ---- armature
arm_d = bpy.data.armatures.new("TBride_Rig"); rig = bpy.data.objects.new("TBride_Rig", arm_d)
col.objects.link(rig); rig.parent = root
vl = bpy.context.view_layer
for o in vl.objects:
    o.select_set(False)
rig.select_set(True); vl.objects.active = rig
bpy.ops.object.mode_set(mode="EDIT")
for s, nm in ((1, "L"), (-1, "R")):
    u = arm_d.edit_bones.new("upper." + nm); u.head = J[s]["S"]; u.tail = J[s]["E"]
    f = arm_d.edit_bones.new("fore." + nm); f.head = J[s]["E"]; f.tail = J[s]["W"]; f.parent = u; f.use_connect = True
bpy.ops.object.mode_set(mode="OBJECT")
for s, nm in ((1, "L"), (-1, "R")):
    tgt = bpy.data.objects.new("TB_IK_" + nm, None); col.objects.link(tgt); tgt.parent = root
    tgt.location = Vector((s * P.get("wx", 0.062), YS + P.get("wy", 0.020), ZC[s]))
    pole = bpy.data.objects.new("TB_Pole_" + nm, None); col.objects.link(pole); pole.parent = root
    pole.location = Vector((s * 0.42, 0.18, 0.66))
    pb = rig.pose.bones["fore." + nm]
    c = pb.constraints.new("IK"); c.target = tgt; c.pole_target = pole; c.chain_count = 2
    c.pole_angle = math.radians(P.get("pole_deg", -90))
mod = body.modifiers.new("Armature", "ARMATURE"); mod.object = rig
bpy.context.view_layer.update()

# actual wrist after IK (world -> root local)
dg = bpy.context.evaluated_depsgraph_get()
wr = {}
for s, nm in ((1, "L"), (-1, "R")):
    pb = rig.pose.bones["fore." + nm]
    wr[s] = root.matrix_world.inverted() @ rig.matrix_world @ pb.tail
    print("wrist", nm, tuple(round(v, 3) for v in wr[s]), "target", tuple(round(v, 3) for v in bpy.data.objects["TB_IK_" + nm].location))

# ---- new hands closed around the stems (Tripo skin texel, Tripo material)
p = Part("TBride_Hands")
K = P.get("hand_scale", 1.30)
for s in (1, -1):
    zc = ZC[s]
    c0 = Vector((0, YS, zc))
    def h(x, y, z):          # hand-local (relative to the stem bundle, x mirrored per side) -> root local
        return c0 + Vector((s * x * K, y * K, z * K))
    w = wr[s]
    # wrist bridge from the forearm end into the palm
    p.tube([w + (w - h(0.034, 0.010, 0.0)).normalized() * 0.012, h(0.046, 0.012, 0.0), h(0.034, 0.010, 0.0)],
           [0.020, 0.019, 0.020], "skin", sides=14, sub=3)
    p.ell(h(0.034, 0.010, 0.0), (0.020 * K, 0.025 * K, 0.029 * K), "skin", 12, 10)          # palm (behind the stems)
    for dz in (0.013, 0.004, -0.005, -0.014):                                                     # four curled fingers
        p.tube([h(0.032, -0.008, dz), h(0.013, -0.021, dz), h(-0.004, -0.019, dz - 0.002), h(-0.013, -0.009, dz - 0.002)],
               [0.0068 * K, 0.0066 * K, 0.0060 * K, 0.0048 * K], "skin", sides=8, sub=3)
    p.tube([h(0.030, -0.006, 0.020), h(0.020, -0.016, 0.026), h(0.010, -0.020, 0.028)], [0.008 * K, 0.0075 * K, 0.006 * K], "skin", sides=8, sub=3)  # thumb
tm = body.data.materials[0]
hands = p.finish(root, [tm], col)
uvd = hands.data.uv_layers[0].data
uvd.foreach_set("uv", np.tile(np.array(P.get("skin_uv", (0.1736, 0.0668)), np.float32), len(uvd)))
for poly in hands.data.polygons:
    poly.material_index = 0
print("hands tris", sum(len(q.vertices) - 2 for q in hands.data.polygons), "body verts", len(me.vertices))
