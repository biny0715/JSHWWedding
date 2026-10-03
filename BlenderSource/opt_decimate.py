# WebGL 용량 최적화: Tripo 신랑/신부 FBX 를 읽어 몸(Tripo 본체) 폴리곤만 줄여 다시 내보낸다.
# 다른 재질 부위(부케·베일·몸통 보정·속눈썹 등)와 그 경계 정점은 보호(가중치 0)해서 그대로 둔다.
# 오브젝트/메시/재질 이름과 슬롯 순서는 유지(Unity 리맵·프리팹 참조 보존).
# 실행: blender -b --factory-startup -P opt_decimate.py -- <in.fbx> <out.fbx> <목표 총 삼각형 수> <줄일 재질 이름>
import bpy, bmesh, sys

src, dst, target, body_mat = sys.argv[sys.argv.index("--") + 1:][:4]
target = int(target)
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=src)
obj = next(o for o in bpy.context.scene.objects if o.type == "MESH")
me = obj.data
tris0 = sum(len(p.vertices) - 2 for p in me.polygons)
mats = [m.name for m in me.materials]
body_idx = mats.index(body_mat)

# 보호 정점: 몸이 아닌 면에 속한 정점은 전부 0
protect = set()
for p in me.polygons:
    if p.material_index != body_idx:
        protect.update(p.vertices)
# UV 경계(seam) 정점도 보호 — 경계를 가로질러 합쳐지면 눈·입 텍스처가 찌그러짐
bm = bmesh.new(); bm.from_mesh(me)
uvl = bm.loops.layers.uv.active
seam = set()
for e in bm.edges:
    if len(e.link_loops) != 2:
        seam.update(v.index for v in e.verts); continue
    l1, l2 = e.link_loops
    a1, b1 = l1[uvl].uv, l1.link_loop_next[uvl].uv      # l1: v0->v1
    a2, b2 = l2.link_loop_next[uvl].uv, l2[uvl].uv      # l2 는 반대 방향
    if (a1 - a2).length > 1e-5 or (b1 - b2).length > 1e-5:
        seam.update(v.index for v in e.verts)
bm.free()
print("seam verts", len(seam), "of", len(me.vertices))
protect |= seam

# 얼굴 앞부분(눈·코·입) 보호: 머리 높이 띠에서 텍스처가 피부색인 쪽을 '앞'으로 판단해 그 앞쪽 영역 정점을 고정
if len(sys.argv[sys.argv.index("--") + 1:]) > 4:
    import numpy as np
    img = bpy.data.images.load(sys.argv[sys.argv.index("--") + 5])
    W, H = img.size
    px = np.array(img.pixels[:], np.float32).reshape(H, W, img.channels)
    co = np.array([obj.matrix_world @ v.co for v in me.vertices])
    uv = np.zeros((len(me.vertices), 2), np.float32)
    for l in me.loops:
        uv[l.vertex_index] = me.uv_layers.active.data[l.index].uv
    zmin, zmax = co[:, 2].min(), co[:, 2].max(); h = zmax - zmin
    band = (co[:, 2] > zmin + 0.62 * h) & (co[:, 2] < zmin + 0.95 * h) & (np.abs(co[:, 0]) < 0.25)
    c = px[(np.clip(uv[:, 1], 0, 1) * (H - 1)).astype(int), (np.clip(uv[:, 0], 0, 1) * (W - 1)).astype(int), :3]
    skin = band & (c[:, 0] > 0.6) & (c[:, 0] > c[:, 2] + 0.08)
    front = np.sign(co[skin, 1].mean())                     # 피부(얼굴)가 있는 y 방향
    sk = co[skin & (np.abs(co[:, 0]) < 0.12) & (co[:, 2] > zmin + 0.72 * h)]   # 목·가슴 피부 제외
    zc = np.median(sk[:, 2])
    face = band & (co[:, 1] * front > 0) & (np.abs(co[:, 2] - zc) < 0.17) & (np.abs(co[:, 0]) < 0.22)
    protect |= set(np.where(face)[0].tolist())
    print("face front y", front, "zc", round(float(zc), 3), "face verts", int(face.sum()))
vg = obj.vertex_groups.new(name="decimate")
body_verts = {v for p in me.polygons if p.material_index == body_idx for v in p.vertices} - protect
vg.add(list(body_verts), 1.0, "REPLACE")
if protect:
    vg.add(list(protect), 0.0, "REPLACE")

orig = obj.copy(); orig.data = me.copy(); bpy.context.scene.collection.objects.link(orig)   # 법선 원본

md = obj.modifiers.new("dec", "DECIMATE")
md.decimate_type = "COLLAPSE"
md.ratio = min(1.0, target / tris0)
md.use_collapse_triangulate = True
md.vertex_group = "decimate"
md.vertex_group_factor = 1.0
bpy.context.view_layer.objects.active = obj
obj.select_set(True)
bpy.ops.object.modifier_apply(modifier="dec")
obj.vertex_groups.clear()

# 감소 후 법선이 깨져 음영이 각지므로, 원본 메시의 법선을 그대로 옮겨온다
dt = obj.modifiers.new("nrm", "DATA_TRANSFER")
dt.object = orig
dt.use_loop_data = True
dt.data_types_loops = {"CUSTOM_NORMAL"}
dt.loop_mapping = "POLYINTERP_NEAREST"
bpy.ops.object.modifier_apply(modifier="nrm")
bpy.data.objects.remove(orig, do_unlink=True)

per = {}
for p in me.polygons:
    per[mats[p.material_index]] = per.get(mats[p.material_index], 0) + len(p.vertices) - 2
print("RESULT", obj.name, "tris", tris0, "->", sum(per.values()), per, "custom_normals", me.has_custom_normals)

bpy.ops.export_scene.fbx(filepath=dst, use_selection=True, object_types={"MESH"},
                         add_leaf_bones=False, bake_anim=False, path_mode="AUTO", embed_textures=False,
                         mesh_smooth_type="OFF", apply_scale_options="FBX_SCALE_ALL")
