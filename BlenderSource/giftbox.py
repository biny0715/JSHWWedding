# 보물찾기용 선물상자(로우폴리) — 상자 + 뚜껑 + 십자 리본 + 리본 매듭. 재질 2개(상자/리본).
# 실행: blender -b --factory-startup -P giftbox.py -- <out.fbx>
# 단위 m, 바닥 중심이 원점(Unity 에서 PickupZone 위치 = 바닥).
import bpy, bmesh, math, sys
from mathutils import Matrix, Vector

out = sys.argv[sys.argv.index("--") + 1]
bpy.ops.wm.read_factory_settings(use_empty=True)

S = 0.36          # 상자 한 변
H = 0.30          # 상자 높이(뚜껑 제외)
LID_H, LID_O = 0.07, 0.025   # 뚜껑 높이 / 바깥으로 튀어나온 폭
RW, RT = 0.07, 0.006         # 리본 폭 / 두께

bm = bmesh.new()
mat_of = {}
round_faces = set()


def box(cx, cy, z0, sx, sy, sz, mat):
    r = bmesh.ops.create_cube(bm, size=1.0)
    vs = r["verts"]
    bmesh.ops.scale(bm, vec=Vector((sx, sy, sz)), verts=vs)
    bmesh.ops.translate(bm, vec=Vector((cx, cy, z0 + sz / 2)), verts=vs)
    for f in {f for v in vs for f in v.link_faces}:
        mat_of[f] = mat


# 상자 몸통 + 뚜껑
box(0, 0, 0, S, S, H, 0)
box(0, 0, H - 0.01, S + LID_O, S + LID_O, LID_H, 0)
top = H - 0.01 + LID_H
# 십자 리본(옆면을 감싸고 위로) — 몸통/뚜껑 바깥에 살짝 띄움
for sx, sy in ((RW, S + 2 * RT), (S + 2 * RT, RW)):
    box(0, 0, 0, sx, sy, H - 0.01, 1)
for sx, sy in ((RW, S + LID_O + 2 * RT), (S + LID_O + 2 * RT, RW)):
    box(0, 0, H - 0.01, sx, sy, LID_H + RT, 1)

# 리본 매듭: 납작한 고리 2개(토러스 반쪽 느낌) + 가운데 매듭
def loop(angle):
    r = bmesh.ops.create_uvsphere(bm, u_segments=10, v_segments=6, radius=1.0)
    vs = r["verts"]
    bmesh.ops.scale(bm, vec=Vector((0.085, 0.035, 0.06)), verts=vs)
    bmesh.ops.rotate(bm, cent=Vector(), matrix=Matrix.Rotation(math.radians(25), 3, "Y"), verts=vs)
    bmesh.ops.translate(bm, vec=Vector((0.075, 0, 0.045)), verts=vs)
    bmesh.ops.rotate(bm, cent=Vector(), matrix=Matrix.Rotation(angle, 3, "Z"), verts=vs)
    bmesh.ops.translate(bm, vec=Vector((0, 0, top)), verts=vs)
    for f in {f for v in vs for f in v.link_faces}:
        mat_of[f] = 1; round_faces.add(f)


for a in (math.radians(45), math.radians(225), math.radians(135), math.radians(315)):
    loop(a)
r = bmesh.ops.create_uvsphere(bm, u_segments=8, v_segments=6, radius=0.035)
bmesh.ops.translate(bm, vec=Vector((0, 0, top + 0.03)), verts=r["verts"])
for f in {f for v in r["verts"] for f in v.link_faces}:
    mat_of[f] = 1; round_faces.add(f)

me = bpy.data.meshes.new("GiftBox")
for f in bm.faces:
    f.material_index = mat_of.get(f, 0)
    f.smooth = f in round_faces          # 매듭(구)만 부드럽게, 상자/리본 띠는 각지게
bm.to_mesh(me); bm.free()
for name, col in (("GiftBox_Box", (0.85, 0.08, 0.12, 1)), ("GiftBox_Ribbon", (1.0, 0.78, 0.1, 1))):
    m = bpy.data.materials.new(name); m.diffuse_color = col; me.materials.append(m)
obj = bpy.data.objects.new("GiftBox", me)
bpy.context.scene.collection.objects.link(obj)
obj.select_set(True); bpy.context.view_layer.objects.active = obj
print("GIFTBOX tris", sum(len(p.vertices) - 2 for p in me.polygons), "top", round(top, 3))
bpy.ops.export_scene.fbx(filepath=out, use_selection=True, object_types={"MESH"}, apply_scale_options="FBX_SCALE_ALL",
                         add_leaf_bones=False, bake_anim=False, mesh_smooth_type="FACE")
