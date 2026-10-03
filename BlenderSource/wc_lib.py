# WeddingCouple v2 - shared library (geometry helpers, head model, atlas painter)
# Blender 5.2 / executed through the Blender MCP socket (execute_code).
import bpy, bmesh, math, random, os
import numpy as np
from mathutils import Vector, Matrix

ROOT = r"D:\UnityProjects\JSHWWedding"
OUT_DIR = os.path.join(ROOT, "Assets", "Models", "WeddingCouple")
SRC_DIR = os.path.join(ROOT, "BlenderSource")
ATLAS_FILE = os.path.join(OUT_DIR, "WeddingCouple_Atlas.png")
AN = 1024
TAU = 2 * math.pi


def smoothstep(e0, e1, x):
    t = min(max((x - e0) / (e1 - e0), 0.0), 1.0)
    return t * t * (3 - 2 * t)


def lerp(a, b, t):
    return a + (b - a) * t


# ------------------------------------------------------------------ palette / atlas layout
# name: (sRGB, smoothness)  -> atlas alpha channel = smoothness (URP Lit "Albedo Alpha")
PAL = {
    "skin": ((248, 211, 190), 0.33), "ear_in": ((246, 196, 180), 0.30), "nail": ((250, 214, 200), 0.4),
    "skin_g": ((249, 208, 190), 0.33), "ear_g": ((244, 188, 174), 0.30),
    "hair_b": ((70, 49, 41), 0.34), "hair_g": ((54, 46, 44), 0.30),
    "suit": ((40, 37, 38), 0.16), "vest": ((47, 44, 45), 0.22), "lapel": ((32, 30, 31), 0.28),
    "shirt": ((248, 247, 245), 0.18), "button": ((22, 21, 22), 0.75), "bow": ((26, 24, 25), 0.55),
    "shoe": ((20, 19, 20), 0.85), "sole": ((44, 36, 32), 0.25), "pocket": ((251, 250, 248), 0.2),
    "dress": ((246, 238, 229), 0.72), "dress_in": ((236, 226, 216), 0.4), "drape": ((251, 247, 241), 0.72),
    "pearl": ((240, 234, 228), 0.92), "metal": ((226, 210, 170), 0.85),
    "flower": ((252, 252, 248), 0.30), "bud": ((244, 247, 232), 0.30), "fcenter": ((226, 214, 150), 0.2),
    "leaf": ((96, 132, 72), 0.38), "leaf_d": ((70, 104, 56), 0.38), "stem": ((104, 138, 78), 0.3),
    "ribbon": ((250, 246, 238), 0.62),
}
PAL_NAMES = list(PAL)
CELL, PAL_COLS = 32, 32
REG = {"face_b": (0, 512, 512, 512), "face_g": (512, 512, 512, 512),
       "hair_b": (0, 256, 256, 256), "hair_g": (256, 256, 256, 256), "veil": (512, 256, 256, 256),
       "dress": (768, 256, 256, 256)}
FX0, FZ0, FS = -0.26, 0.84, 0.56          # face tile covers x[-0.26,0.30] z[0.84,1.40] (front projection)


def pal_uv(name):
    i = PAL_NAMES.index(name)
    cx, cy = i % PAL_COLS, i // PAL_COLS
    return ((cx * CELL + CELL / 2) / AN, (cy * CELL + CELL / 2) / AN)


def reg_uv(reg, u, v, pad=2.0):
    x, y, w, h = REG[reg]
    u = min(max(u, 0.0), 1.0)
    v = min(max(v, 0.0), 1.0)
    return ((x + pad + u * (w - 2 * pad)) / AN, (y + pad + v * (h - 2 * pad)) / AN)


def face_uv(reg, x, z):
    return reg_uv(reg, (x - FX0) / FS, (z - FZ0) / FS, pad=0.0)


# ------------------------------------------------------------------ painter (numpy, metres in face space)
class Painter:
    def __init__(self, n, x0, z0, size, bg_rgb, bg_s):
        self.n, self.px = n, size / n
        xs = x0 + (np.arange(n) + 0.5) * self.px
        zs = z0 + (np.arange(n) + 0.5) * self.px
        self.X, self.Z = np.meshgrid(xs.astype(np.float32), zs.astype(np.float32))
        self.x0, self.z0 = x0, z0
        self.img = np.zeros((n, n, 4), np.float32)
        self.img[..., :3] = np.array(bg_rgb, np.float32) / 255
        self.img[..., 3] = bg_s

    def win(self, xmin, xmax, zmin, zmax):
        c0 = max(int((xmin - self.x0) / self.px) - 2, 0)
        c1 = min(int((xmax - self.x0) / self.px) + 3, self.n)
        r0 = max(int((zmin - self.z0) / self.px) - 2, 0)
        r1 = min(int((zmax - self.z0) / self.px) + 3, self.n)
        return (slice(r0, r1), slice(c0, c1))

    def cov(self, sd, soft=1.0):
        return np.clip(0.5 - sd / (self.px * soft), 0.0, 1.0)

    def put(self, sl, c, rgb, alpha=1.0, smooth=None):
        a = (c * alpha)[..., None]
        sub = self.img[sl]
        col = rgb if isinstance(rgb, np.ndarray) else np.array(rgb, np.float32) / 255
        sub[..., :3] = sub[..., :3] * (1 - a) + col * a
        if smooth is not None:
            sub[..., 3] = sub[..., 3] * (1 - a[..., 0]) + smooth * a[..., 0]

    def stroke(self, pts, widths, rgb, alpha=1.0, smooth=None, soft=1.0):
        wmax = max(widths)
        xs = [p[0] for p in pts]
        zs = [p[1] for p in pts]
        sl = self.win(min(xs) - wmax, max(xs) + wmax, min(zs) - wmax, max(zs) + wmax)
        X, Z = self.X[sl], self.Z[sl]
        best = np.full(X.shape, 1e9, np.float32)
        for i in range(len(pts) - 1):
            x0, z0 = pts[i]
            x1, z1 = pts[i + 1]
            dx, dz = x1 - x0, z1 - z0
            L2 = dx * dx + dz * dz + 1e-12
            t = np.clip(((X - x0) * dx + (Z - z0) * dz) / L2, 0, 1)
            d = np.hypot(X - (x0 + t * dx), Z - (z0 + t * dz))
            w = widths[i] + (widths[i + 1] - widths[i]) * t
            best = np.minimum(best, d - w / 2)
        self.put(sl, self.cov(best, soft), rgb, alpha, smooth)

    def gauss(self, cx, cz, rx, rz, rgb, alpha, smooth=None):
        sl = self.win(cx - 3 * rx, cx + 3 * rx, cz - 3 * rz, cz + 3 * rz)
        g = np.exp(-(((self.X[sl] - cx) / rx) ** 2 + ((self.Z[sl] - cz) / rz) ** 2))
        self.put(sl, g, rgb, alpha, smooth)


def bez(p0, p1, p2, n=24):
    out = []
    for i in range(n + 1):
        t = i / n
        out.append(((1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t * t * p2[0],
                    (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t * t * p2[1]))
    return out


def paint_eye(P, cx, cz, side, e):
    """side=+1 for the character's left eye (x>0). local x grows towards the outer corner."""
    a, b = e["a"], e["b"]
    sl = P.win(cx - a * 1.6, cx + a * 1.6, cz - b * 1.6, cz + b * 2.0)
    X, Z = P.X[sl], P.Z[sl]
    xl = (X - cx) * side
    zl = Z - cz
    u = np.clip(np.abs(xl / a), 0, 1)
    tilt = e.get("tilt", 0.0)
    zup = b * (1 - u ** e.get("pu", 2.2)) ** (1 / e.get("pu", 2.2)) + tilt * (xl / a) * b
    zlo = -b * 0.92 * (1 - u ** e.get("pl", 2.6)) ** (1 / e.get("pl", 2.6)) + tilt * (xl / a) * b
    sd_w = np.maximum(np.maximum(np.abs(xl) - a, zl - zup), zlo - zl)
    cw = P.cov(sd_w)
    # sclera with soft lid shadow
    P.put(sl, cw, (250, 247, 246), 1.0, 0.4)
    sh = np.clip(1 - (zup - zl) / (0.5 * b), 0, 1)
    P.put(sl, cw * sh, (205, 196, 204), 0.55)
    # iris
    r = e["ir"]
    ix, iz = -e["idx"], e["idz"]
    dd = np.hypot(xl - ix, (zl - iz) / 1.04)
    ci = P.cov(dd - r) * cw
    tt = np.clip((zl - (iz - r)) / (2 * r), 0, 1)[..., None] ** 0.75
    top = np.array(e["iris_top"], np.float32) / 255
    bot = np.array(e["iris_bot"], np.float32) / 255
    P.put(sl, ci, bot * (1 - tt) + top * tt, 1.0, 0.35)
    ring = np.clip((dd / r - 0.76) / 0.24, 0, 1)
    P.put(sl, ci * ring, (34, 22, 18), 0.65)
    g = np.exp(-(((xl - ix) / (0.55 * r)) ** 2 + ((zl - (iz - 0.52 * r)) / (0.26 * r)) ** 2))
    P.put(sl, ci * g, e.get("glow", (160, 112, 84)), 0.55)
    P.put(sl, ci * sh, (25, 16, 14), 0.35)
    cp = P.cov(np.hypot(xl - ix, zl - (iz + 0.05 * r)) - e.get("pupil", 0.44) * r) * cw
    P.put(sl, cp, (24, 16, 14), 0.95)
    # highlights (world space: up-right like the reference)
    hx, hz = ix + side * 0.34 * r, iz + 0.40 * r
    P.put(sl, P.cov(np.hypot(xl - hx, zl - hz) - 0.20 * r) * cw, (255, 255, 255), 1.0, 0.95)
    hx, hz = ix - side * 0.36 * r, iz - 0.42 * r
    P.put(sl, P.cov(np.hypot(xl - hx, zl - hz) - 0.11 * r) * cw, (255, 255, 255), 0.85, 0.95)
    # upper lid line (+ wing)
    lw = e["lw"]
    pts, ws = [], []
    for i in range(29):
        uu = -1.02 + 2.02 * i / 28
        zz = b * (1 - min(abs(uu), 1) ** e.get("pu", 2.2)) ** (1 / e.get("pu", 2.2)) + tilt * uu * b
        pts.append((cx + side * uu * a, cz + zz + lw * 0.22))
        ws.append(lw * (0.30 + 0.70 * smoothstep(-1.0, -0.35, uu)) * (0.85 + 0.15 * uu))
    wing = e.get("wing", 0.0)
    for i in range(1, 7):
        k = i / 6
        uu = 1.0 + wing * k
        pts.append((cx + side * uu * a, cz + tilt * b + e.get("wing_rise", 0.2) * b * k + lw * 0.22 * (1 - k)))
        ws.append(lw * (1.0 - 0.85 * k))
    P.stroke(pts, ws, e["lid"], 1.0, 0.4)
    for (uu, ang, ln) in e.get("lashes", []):
        zz = b * (1 - min(abs(uu), 1) ** 2.2) ** (1 / 2.2) + tilt * uu * b
        x0, z0 = cx + side * uu * a, cz + zz + lw * 0.3
        th = math.radians(ang)
        P.stroke([(x0, z0), (x0 + side * math.cos(th) * ln * 0.55, z0 + math.sin(th) * ln * 0.6),
                  (x0 + side * math.cos(th) * ln, z0 + math.sin(th) * ln * 0.9)],
                 [lw * 0.55, lw * 0.32, lw * 0.08], e["lid"], 1.0, 0.4)
    if e.get("lower", 0) > 0:
        pts, ws = [], []
        for i in range(15):
            uu = 0.1 + 0.92 * i / 14
            zz = -b * 0.92 * (1 - min(uu, 1) ** 2.6) ** (1 / 2.6) + tilt * uu * b
            pts.append((cx + side * uu * a, cz + zz - 0.0008))
            ws.append(0.0022 * (0.6 + 0.4 * uu))
        P.stroke(pts, ws, e.get("lower_col", (150, 98, 86)), e["lower"], soft=1.4)
    if e.get("crease", 0) > 0:
        pts = []
        for i in range(15):
            uu = -0.45 + 1.3 * i / 14
            zz = b * (1 - min(abs(uu), 1) ** 2.2) ** (1 / 2.2) + tilt * uu * b
            pts.append((cx + side * uu * a, cz + zz + 0.30 * b + lw))
        P.stroke(pts, [0.0016] * len(pts), (205, 150, 136), e["crease"], soft=1.5)


def paint_face(F):
    sk = F.get("skin", "skin")
    P = Painter(512, FX0, FZ0, FS, PAL[sk][0], PAL[sk][1])
    for s in (-1, 1):
        bx, bz = F["blush"]
        P.gauss(s * bx, bz, F["blush_r"][0], F["blush_r"][1], (250, 168, 152), F["blush_a"])
    zn = F["nose_z"]
    P.gauss(0.0, zn - 0.002, 0.012, 0.009, (238, 160, 146), F.get("nose_a", 0.6))
    P.gauss(0.003, zn + 0.005, 0.005, 0.004, (255, 250, 246), 0.3)
    mx, mz, dip, up = F["mouth"]
    p0, p2 = (-mx, mz + up), (mx, mz + up)
    p1 = (0.0, 2 * (mz - dip) - mz - up)
    pts = bez(p0, p1, p2, 26)
    mw = F.get("mouth_w", 0.0035)
    ws = [0.0013 + (mw - 0.0013) * math.sin(math.pi * i / 26) for i in range(27)]
    P.stroke(pts, ws, F["mouth_col"], 1.0, 0.5, soft=1.2)
    for s in (-1, 1):
        P.stroke([(s * mx, mz + up), (s * (mx + 0.0028), mz + up + 0.0034)], [0.0013, 0.0007], F["mouth_col"], 0.9, soft=1.2)
    for s in (-1, 1):
        (x0, z0), (x1, z1), (x2, z2) = F["brow"]
        pts = bez((s * x0, z0), (s * x1, z1), (s * x2, z2), 22)
        w0, w1, w2 = F["brow_w"]
        ws = [lerp(w0, w1, min(1, 2 * i / 22)) if i <= 11 else lerp(w1, w2, (i - 11) / 11) for i in range(23)]
        P.stroke(pts, ws, F["brow_col"], 1.0, 0.3, soft=1.6)
        ex, ez = F["eye"]
        paint_eye(P, s * ex, ez, s, F["eye_p"])
    return P.img


def hair_tile(base, seed, smooth):
    n = 256
    rng = np.random.default_rng(seed)
    u = (np.arange(n) + 0.5) / n
    v = (np.arange(n) + 0.5) / n
    U, V = np.meshgrid(u, v)
    nu = np.zeros_like(U)
    wob = 0.18 * np.sin(TAU * V * 0.9)
    for k in range(16, 200, 2):
        nu += rng.random() / k ** 0.35 * np.sin(TAU * (k * U + wob * (k / 60.0) + 0.05 * np.sin(TAU * V * 2 + k)) + rng.random() * TAU)
    nu /= np.abs(nu).max()
    # fine dark separations between strands (two interleaved spacings so they don't look like a regular grating)
    sep = np.clip((np.sin(TAU * (34 * U + wob * 0.6)) - 0.86) / 0.14, 0, 1) \
        + 0.6 * np.clip((np.sin(TAU * (57 * U + wob + 0.3)) - 0.9) / 0.1, 0, 1)
    shine = np.exp(-((V - 0.64) / 0.07) ** 2) * (0.55 + 0.45 * np.clip(nu + 0.3, 0, 1))
    B = 1.0 + 0.18 * nu + 0.22 * shine - 0.12 * (1 - V) ** 3 - 0.20 * np.clip(sep, 0, 1)
    img = np.zeros((n, n, 4), np.float32)
    img[..., :3] = np.clip(np.array(base, np.float32)[None, None, :] / 255 * B[..., None], 0, 1)
    img[..., 3] = np.clip(smooth + 0.08 * nu + 0.12 * shine, 0, 1)
    return img


def dress_tile():
    # ivory satin: soft vertical sheen bands that follow the skirt folds + slight top-to-hem gradient
    n = 256
    U, V = np.meshgrid((np.arange(n) + 0.5) / n, (np.arange(n) + 0.5) / n)
    sheen = 0.55 * np.sin(TAU * (6 * U + 0.35 * np.sin(TAU * V * 0.8))) + 0.45 * np.sin(TAU * (11 * U + 0.2 * np.sin(TAU * V * 1.3) + 0.4))
    hi = np.clip(sheen, 0, 1) ** 2
    B = 0.965 + 0.035 * sheen + 0.04 * hi + 0.03 * V
    img = np.zeros((n, n, 4), np.float32)
    img[..., :3] = np.clip(np.array(PAL["dress"][0], np.float32)[None, None, :] / 255 * B[..., None], 0, 1)
    img[..., 3] = 0.74 + 0.10 * hi
    return img


def veil_tile():
    n = 256
    Y, X = np.mgrid[0:n, 0:n].astype(np.float32) + 0.5
    a = np.full((n, n), 0.36, np.float32)
    net = np.clip(1.0 - np.hypot((X % 4.0) - 2.0, (Y % 4.0) - 2.0) / 0.9, 0, 1)
    a += 0.10 * net
    cs = 9.0
    gx = (X % cs) - cs / 2
    gy = ((Y + (np.floor(X / cs) % 2) * cs / 2) % cs) - cs / 2
    rr = np.hypot(gx, gy)
    a += 0.035 * np.clip(1.0 - np.abs(rr - 2.4), 0, 1) + 0.03 * np.clip(1.2 - rr, 0, 1)
    # lace border. veil UV is ~1 cm/px across but ~0.3 cm/px down, so vertical distances are divided by ASP
    ASP = 3.2
    P_ = 20.0                                                # scallop / motif period (~20 cm)
    dx = (X % P_) - P_ / 2
    yb = 10.0 - 2.6 * np.sqrt(np.clip(1.0 - (dx / (P_ / 2)) ** 2, 0, None))   # scalloped hem
    edge = (Y >= yb) & (Y < yb + 3.0)
    band = (Y >= yb + 3.0) & (Y < 46)
    a = np.where(band, np.maximum(a, 0.30), a)

    def disk(cx, cy, r):
        return np.clip(1.4 - np.hypot(cx, cy / 1.0) / r * 1.4, 0, 1)
    lace = np.zeros_like(a)
    fx, fy = dx, (Y - 26.0) / ASP                            # flower centre ~8 cm above the hem
    for k in range(6):                                       # six-petal open flower (~6 cm)
        ang = TAU * k / 6 + 0.25
        lace = np.maximum(lace, disk(fx - 3.2 * math.cos(ang), fy - 3.2 * math.sin(ang), 2.2))
    lace = np.maximum(lace, 0.6 * disk(fx, fy, 1.4))
    for sgn in (-1, 1):                                      # leaves + vine linking the flowers
        lx, ly = dx - sgn * 7.0, (Y - 21.0) / ASP + 0.8
        lace = np.maximum(lace, np.clip(1.0 - np.hypot(lx / 2.6, ly / 1.0), 0, 1) * 0.9)
    vine = np.clip(1.0 - np.abs((Y - 18.0) / ASP - 1.2 * np.sin(TAU * X / P_)) / 0.45, 0, 1)
    lace = np.maximum(lace, 0.7 * vine * (Y < 30))
    sm = (((X % 6.0) - 3.0) ** 2 + (((Y - 38.0) / ASP) * 1.0) ** 2) < 1.3   # small sprig dots row
    lace = np.maximum(lace, 0.6 * sm)
    a = np.where(band, np.maximum(a, 0.30 + 0.55 * lace), a)
    a = np.where(edge, 0.85, a)    # thin lace trim at the top edge (crown)
    a = np.where(Y > n - 7, np.maximum(a, 0.30 + 0.2 * np.clip(1.2 - np.abs(((X % 6.0) - 3.0)), 0, 1)), a)
    a = np.where(Y < yb, 0.0, a)
    img = np.zeros((n, n, 4), np.float32)
    img[..., :3] = np.array((255, 253, 250), np.float32) / 255
    img[..., 3] = a
    return img


def build_atlas(face_b, face_g):
    A = np.zeros((AN, AN, 4), np.float32)
    for i, name in enumerate(PAL_NAMES):
        rgb, sm = PAL[name]
        cx, cy = i % PAL_COLS, i // PAL_COLS
        A[cy * CELL:(cy + 1) * CELL, cx * CELL:(cx + 1) * CELL, :3] = np.array(rgb, np.float32) / 255
        A[cy * CELL:(cy + 1) * CELL, cx * CELL:(cx + 1) * CELL, 3] = sm
    def blit(reg, tile):
        x, y, w, h = REG[reg]
        A[y:y + h, x:x + w] = tile
    blit("face_b", paint_face(face_b))
    blit("face_g", paint_face(face_g))
    blit("hair_b", hair_tile(PAL["hair_b"][0], 11, 0.32))
    blit("hair_g", hair_tile(PAL["hair_g"][0], 23, 0.26))
    blit("veil", veil_tile())
    blit("dress", dress_tile())
    img = bpy.data.images.get("WeddingCouple_Atlas")
    if img:
        bpy.data.images.remove(img)
    img = bpy.data.images.new("WeddingCouple_Atlas", AN, AN, alpha=True)
    img.alpha_mode = "STRAIGHT"
    img.pixels.foreach_set(A.ravel())
    img.filepath_raw = ATLAS_FILE
    img.file_format = "PNG"
    img.save()
    return img


# ------------------------------------------------------------------ curves / frames
def cr_path(pts, sub):
    P = [Vector(p) for p in pts]
    out = []
    for i in range(len(P) - 1):
        p0 = P[i - 1] if i > 0 else P[i] + (P[i] - P[i + 1])
        p1, p2 = P[i], P[i + 1]
        p3 = P[i + 2] if i + 2 < len(P) else P[i + 1] + (P[i + 1] - P[i])
        for s in range(sub):
            u = s / sub
            out.append(0.5 * ((2 * p1) + (-p0 + p2) * u + (2 * p0 - 5 * p1 + 4 * p2 - p3) * u * u
                              + (-p0 + 3 * p1 - 3 * p2 + p3) * u * u * u))
    out.append(P[-1])
    return out


def frames(pts, n0=None):
    T = [(pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]).normalized() for i in range(len(pts))]
    if n0 is None:
        ref = Vector((0, 0, 1)) if abs(T[0].z) < 0.9 else Vector((1, 0, 0))
        n0 = T[0].cross(ref).cross(T[0])
    n = (n0 - T[0] * n0.dot(T[0])).normalized()
    N = [n]
    for i in range(1, len(pts)):
        q = T[i - 1].rotation_difference(T[i])
        n = q @ N[-1]
        n = (n - T[i] * n.dot(T[i])).normalized()
        N.append(n)
    B = [T[i].cross(N[i]).normalized() for i in range(len(pts))]
    return T, N, B


# ------------------------------------------------------------------ vertical squash above the brows
# part-name prefix -> (start z, blend width, scale). Everything above "start" is compressed by "scale" with a C1
# blend so head, hair and veil shorten together while the face/brows below stay untouched.
SQUASH = {}
DROP = {}      # part name -> lower the whole part by this much (e.g. sit the head lower on a short neck)


def squash_z(z, b, w, s, drop=0.0):
    """compress everything above b by s (C1 blend over w), then lower it by drop (blended in over w)."""
    if z <= b:
        return z
    d = z - b
    if d < w:
        out = b + d - (1 - s) * d * d / (2 * w)
    else:
        out = b + w - (1 - s) * w / 2 + s * (d - w)
    return out - drop * smoothstep(b, b + w, z)


# ------------------------------------------------------------------ mesh part
class Part:
    def __init__(self, name):
        self.name = name
        self.bm = bmesh.new()
        self.uv = self.bm.loops.layers.uv.new("UVMap")
        self.mods = []

    def fill(self, faces, color, mat=0):
        for f in faces:
            nm = color(f.calc_center_median()) if callable(color) else color
            u = pal_uv(nm)
            f.material_index = mat
            for l in f.loops:
                l[self.uv].uv = u

    def grid(self, rows, color="skin", uvrows=None, wrap=True, cap0=False, cap1=False, mat=0, orient=True, flip=False):
        bm = self.bm
        vr = [[bm.verts.new(Vector(p)) for p in r] for r in rows]
        n = len(rows[0])
        m = n if wrap else n - 1
        faces, inward = [], 0
        cen = [sum((Vector(p) for p in r), Vector()) / len(r) for r in rows]
        for i in range(len(rows) - 1):
            c = (cen[i] + cen[i + 1]) / 2
            for j in range(m):
                j1 = (j + 1) % n
                f = bm.faces.new((vr[i][j], vr[i][j1], vr[i + 1][j1], vr[i + 1][j]))
                if uvrows is not None:
                    for l, uvv in zip(f.loops, (uvrows[i][j], uvrows[i][j1], uvrows[i + 1][j1], uvrows[i + 1][j])):
                        l[self.uv].uv = uvv
                f.normal_update()
                if f.normal.dot(f.calc_center_median() - c) < 0:
                    inward += 1
                faces.append(f)
        if (orient and inward > len(faces) / 2) or flip:
            for f in faces:
                f.normal_flip()
        caps = []
        for flag, idx, nb in ((cap0, 0, 1), (cap1, -1, -2)):
            if flag:
                f = bm.faces.new(vr[idx][:n - (0 if wrap else 1)] if not wrap else vr[idx])
                f.normal_update()
                if f.normal.dot(cen[idx] - cen[nb]) < 0:
                    f.normal_flip()
                caps.append(f)
        if uvrows is None:
            self.fill(faces + caps, color, mat)
        else:
            for f in faces:
                f.material_index = mat
            if caps:
                self.fill(caps, color, mat)
        return faces

    def ell(self, c, r, color, u=16, v=10, rot=None, mat=0):
        M = Matrix.Translation(Vector(c)) @ (rot.to_4x4() if rot is not None else Matrix.Identity(4)) \
            @ Matrix.Diagonal((r[0], r[1], r[2], 1.0))
        res = bmesh.ops.create_uvsphere(self.bm, u_segments=u, v_segments=v, radius=1.0, matrix=M)
        faces = list({f for vv in res["verts"] for f in vv.link_faces})
        self.fill(faces, color, mat)
        return faces

    def tube(self, path, radii, color="skin", sides=12, cap0=True, cap1=True, ex=1.0, n0=None, uvreg=None, mat=0, sub=1):
        pts = cr_path(path, sub) if sub > 1 else [Vector(p) for p in path]
        if len(radii) != len(pts):
            rr = np.interp(np.linspace(0, 1, len(pts)), np.linspace(0, 1, len(radii)), radii)
        else:
            rr = radii
        T, N, B = frames(pts, n0)
        rows, uvr = [], []
        cnt = sides + (1 if uvreg else 0)
        for i, p in enumerate(pts):
            row, ur = [], []
            for j in range(cnt):
                ph = TAU * j / sides
                row.append(p + N[i] * (math.cos(ph) * rr[i] * ex) + B[i] * (math.sin(ph) * rr[i]))
                if uvreg:
                    ur.append(reg_uv(uvreg, j / sides, 1 - i / (len(pts) - 1)))
            rows.append(row)
            uvr.append(ur)
        if uvreg:
            return self.grid(rows, color, uvrows=uvr, wrap=False, cap0=cap0, cap1=cap1, mat=mat)
        return self.grid(rows, color, wrap=True, cap0=cap0, cap1=cap1, mat=mat)

    def torus(self, c, axis, R, r, color, seg=24, mseg=8, sx=1.0, sy=1.0, mat=0):
        c, axis = Vector(c), Vector(axis).normalized()
        e1 = axis.orthogonal().normalized()
        e2 = axis.cross(e1)
        rows = []
        for i in range(seg):
            a = TAU * i / seg
            d = e1 * math.cos(a) * sx + e2 * math.sin(a) * sy
            ctr = c + d * R
            out = d.normalized()
            rows.append([ctr + out * (math.cos(b) * r) + axis * (math.sin(b) * r) for b in [TAU * k / mseg for k in range(mseg)]])
        rows.append(rows[0])
        return self.grid(rows, color, wrap=True, mat=mat)

    def finish(self, parent, mats, coll, merge=0.00012):
        bmesh.ops.remove_doubles(self.bm, verts=self.bm.verts, dist=merge)
        sq = SQUASH.get(self.name[:2])
        if sq:
            for v in self.bm.verts:
                v.co.z = squash_z(v.co.z, *sq)
        dz = DROP.get(self.name, 0.0)
        if dz:
            for v in self.bm.verts:
                v.co.z -= dz
        for f in self.bm.faces:
            f.smooth = True
        me = bpy.data.meshes.new(self.name)
        self.bm.to_mesh(me)
        self.bm.free()
        for m in mats:
            me.materials.append(m)
        ob = bpy.data.objects.new(self.name, me)
        coll.objects.link(ob)
        ob.parent = parent
        for typ, kw in self.mods:
            md = ob.modifiers.new(typ.lower(), typ)
            for k, v in kw.items():
                setattr(md, k, v)
        return ob


def section(z, sx, sy, cx=0.0, cy=0.0, n=2.4, angles=None, disp=None):
    pts = []
    for a in angles:
        s, c = math.sin(a), math.cos(a)
        x = cx + sx * math.copysign(abs(s) ** (2 / n), s)
        y = cy + sy * math.copysign(abs(c) ** (2 / n), c)
        p = Vector((x, y, z))
        if disp:
            p = disp(p, a)
        pts.append(p)
    return pts



def interp_rows(keys, sub):
    arr = np.array(keys, float)
    out = []
    m = len(arr)
    for i in range(m - 1):
        p0 = arr[i - 1] if i > 0 else 2 * arr[i] - arr[i + 1]
        p1, p2 = arr[i], arr[i + 1]
        p3 = arr[i + 2] if i + 2 < m else 2 * arr[i + 1] - arr[i]
        for s in range(sub):
            u = s / sub
            out.append(0.5 * ((2 * p1) + (-p0 + p2) * u + (2 * p0 - 5 * p1 + 4 * p2 - p3) * u * u
                              + (-p0 + 3 * p1 - 3 * p2 + p3) * u ** 3))
    out.append(arr[-1])
    return [tuple(r) for r in out]


# ------------------------------------------------------------------ head model
# keys: (z, half-width, front depth, back depth, front superellipse exp, back exp), crown -> chin.
# Front depth is the side-view profile: forehead bulge -> slight eye recess -> cheeks/mouth -> short chin.
# front depth (3rd column) follows the modeling-sheet side profile, normalised by eye->chin distance:
# forehead slopes back above the brows (0.048 m behind the nose tip at the brow, 0.08 at the hairline), the eye
# area sits ~2 cm behind the cheeks, cheeks/mouth are the most forward plane, short round chin.
BRIDE_HEAD = [
    (1.465, 0.000, 0.000, 0.000, 2.0, 2.0), (1.458, 0.058, 0.050, 0.062, 2.0, 2.0),
    (1.440, 0.110, 0.095, 0.120, 2.0, 2.0), (1.405, 0.154, 0.130, 0.170, 2.0, 2.0),
    (1.355, 0.188, 0.150, 0.204, 2.0, 2.0), (1.300, 0.212, 0.160, 0.223, 2.0, 2.0),
    (1.240, 0.226, 0.168, 0.232, 2.05, 2.0), (1.175, 0.232, 0.176, 0.232, 2.1, 2.0),
    (1.100, 0.235, 0.186, 0.222, 2.15, 2.0), (1.040, 0.237, 0.200, 0.205, 2.15, 2.0),
    (1.000, 0.228, 0.204, 0.188, 2.1, 2.0), (0.975, 0.212, 0.197, 0.172, 2.05, 2.0),
    (0.952, 0.188, 0.186, 0.152, 2.0, 2.0), (0.935, 0.160, 0.170, 0.132, 2.0, 2.0),
    (0.920, 0.122, 0.148, 0.108, 2.0, 2.0), (0.910, 0.085, 0.120, 0.080, 2.0, 2.0),
    (0.904, 0.045, 0.082, 0.048, 2.0, 2.0), (0.902, 0.000, 0.000, 0.000, 2.0, 2.0)]
GROOM_HEAD = [
    (1.440, 0.000, 0.000, 0.000, 2.0, 2.0), (1.433, 0.058, 0.055, 0.062, 2.0, 2.0),
    (1.417, 0.110, 0.105, 0.119, 2.0, 2.0), (1.385, 0.152, 0.150, 0.168, 2.0, 2.0),
    (1.338, 0.184, 0.188, 0.201, 2.0, 2.0), (1.287, 0.207, 0.209, 0.220, 2.0, 2.0),
    (1.225, 0.220, 0.222, 0.230, 2.05, 2.0), (1.155, 0.226, 0.220, 0.230, 2.1, 2.0),
    (1.080, 0.228, 0.208, 0.220, 2.15, 2.0), (1.025, 0.230, 0.208, 0.203, 2.15, 2.0),
    (0.985, 0.228, 0.203, 0.186, 2.1, 2.0), (0.955, 0.218, 0.193, 0.170, 2.05, 2.0),
    (0.930, 0.200, 0.180, 0.150, 2.0, 2.0), (0.910, 0.172, 0.164, 0.128, 2.0, 2.0),
    (0.895, 0.135, 0.145, 0.102, 2.0, 2.0), (0.885, 0.088, 0.115, 0.068, 2.0, 2.0),
    (0.879, 0.040, 0.075, 0.035, 2.0, 2.0), (0.877, 0.000, 0.000, 0.000, 2.0, 2.0)]

class Head:
    def __init__(self, keys, nose_z, eye, cheek_z=1.0, cheek_x=0.135, cheek_amp=0.016, cheek_r=(0.075, 0.06),
                 nose_amp=0.008, socket_amp=0.005, mouth_z=1.0, mouth_amp=0.004):
        d = np.array(interp_rows(keys, 12))
        d[:, 1:4] = np.maximum(d[:, 1:4], 0.0)
        seg = np.hypot(np.diff(d[:, 1]), np.diff(d[:, 0]))
        L = np.concatenate([[0], np.cumsum(seg)])
        self.L = L / L[-1]
        self.d = d
        self.zr = np.maximum.accumulate(d[::-1, 0])
        self.nose_z, self.eye, self.cheek_z, self.cheek_x, self.cheek_r = nose_z, eye, cheek_z, cheek_x, cheek_r
        self.cheek_amp, self.nose_amp, self.socket_amp = cheek_amp, nose_amp, socket_amp
        self.mouth_z, self.mouth_amp = mouth_z, mouth_amp

    def prof6(self, t):
        return [float(np.interp(t, self.L, self.d[:, i])) for i in range(6)]

    def prof(self, t):
        z, xw, yf = self.prof6(t)[:3]
        return z, xw, yf

    def z_to_t(self, z):
        return float(np.interp(z, self.zr, self.L[::-1]))

    def raw(self, a, t):
        z, xw, yf, yb, nf, nb = self.prof6(t)
        s, c = math.sin(a), math.cos(a)
        n = nf if c < 0 else nb
        x = math.copysign(xw * abs(s) ** (2 / n), s)
        y = -yf * abs(c) ** (2 / n) if c < 0 else yb * abs(c) ** (2 / n)
        return Vector((x, y, z))

    def _n(self, f, a, t):
        t = min(max(t, 0.004), 0.996)
        da, dt = 1e-3, 1e-3
        pa = f(a + da, t) - f(a - da, t)
        pt = f(a, min(t + dt, 1)) - f(a, max(t - dt, 0))
        n = pa.cross(pt)
        if n.length < 1e-12:
            return Vector((0, 0, 1 if t < 0.5 else -1))
        n.normalize()
        p = f(a, t)
        if n.dot(p - Vector((0, 0, 1.17))) < 0:
            n = -n
        return n

    def disp(self, p):
        front = min(max(-p.y / 0.12, 0.0), 1.0)
        g = lambda cx, cz, rx, rz: math.exp(-(((p.x - cx) / rx) ** 2 + ((p.z - cz) / rz) ** 2))
        d = 0.0
        ex, ez = self.eye
        for s in (-1, 1):
            d += self.cheek_amp * g(s * self.cheek_x, self.cheek_z, *self.cheek_r)   # plump cheeks under the eyes
            d -= self.socket_amp * g(s * ex, ez, 0.05, 0.04)                         # eyes sit slightly into the face
        d += self.nose_amp * g(0.0, self.nose_z, 0.017, 0.015)
        d += self.mouth_amp * g(0.0, self.mouth_z, 0.05, 0.028)
        return d * front

    def pt(self, a, t):
        p = self.raw(a, t)
        return p + self._n(self.raw, a, t) * self.disp(p)

    def nrm(self, a, t):
        return self._n(self.pt, a, t)

    def front_at(self, x, z):
        t = self.z_to_t(z)
        _, rx, _ = self.prof(t)
        return math.pi - math.asin(max(-0.97, min(0.97, x / max(rx, 1e-4)))), t

    def side_at(self, adeg, z):
        return math.pi - math.radians(adeg), self.z_to_t(z)

# ------------------------------------------------------------------ blended-volume head (v8)
# The head is one continuous surface: a smooth union of ellipsoids (cranium, two cheeks, jaw/chin, nose ...).
# It is sampled into a (angle, height) radius table so the hair / veil / face-UV code can keep using the same
# pt / nrm / z_to_t / side_at / front_at interface as the old stacked-section Head.
def _ell_sdf(P, c, r):
    q = (P - np.asarray(c)) / np.asarray(r)
    k = np.linalg.norm(q, axis=-1)
    return (k - 1.0) * min(r)


def _smin(a, b, k):
    h = np.clip(0.5 + 0.5 * (b - a) / k, 0.0, 1.0)
    return b * (1 - h) + a * h - k * h * (1 - h)


class BlobHead:
    def __init__(self, blobs, ztop, zbot, nz=220, na=180):
        """blobs: list of (centre, radii, blend k) - first entry is the base; k blends it into the union so far."""
        self.blobs, self.ztop, self.zbot = blobs, ztop, zbot
        self.zmid, self.zhalf = (ztop + zbot) / 2, (ztop - zbot) / 2
        zs = self.zmid + self.zhalf * np.cos(np.pi * np.linspace(0.004, 0.996, nz))
        self.tz = np.linspace(0.004, 0.996, nz)
        # slice centroids (ray origins) so the narrow chin / crown slices are always star-shaped around them
        gx, gy = np.meshgrid(np.linspace(-0.33, 0.33, 67), np.linspace(-0.36, 0.36, 73))
        cx, cy = np.zeros(nz), np.zeros(nz)
        for i, z in enumerate(zs):
            P = np.stack([gx, gy, np.full_like(gx, z)], -1)
            ins = self.sdf(P) < 0
            if ins.any():
                cy[i] = gy[ins].mean()
        cy = np.convolve(np.pad(cy, 6, mode="edge"), np.ones(13) / 13, mode="valid")
        self.cy = cy
        A = np.linspace(0, TAU, na, endpoint=False)
        self.A = A
        dirs = np.stack([np.sin(A), np.cos(A)], -1)                      # a measured like the old Head: x=sin, y=cos
        lo = np.zeros((nz, na)); hi = np.full((nz, na), 0.45)
        for _ in range(32):
            mid = (lo + hi) / 2
            P = np.stack([dirs[None, :, 0] * mid, cy[:, None] + dirs[None, :, 1] * mid, np.repeat(zs[:, None], na, 1)], -1)
            inside = self.sdf(P) < 0
            lo = np.where(inside, mid, lo); hi = np.where(inside, hi, mid)
        self.R = (lo + hi) / 2
        self.zs = zs

    def sdf(self, P):
        d = None
        for (c, r, k) in self.blobs:
            e = _ell_sdf(P, c, r)
            if d is None:
                d = e
            elif k < 0:                      # negative k: smooth subtraction (shallow dents, e.g. eye areas)
                kk = -k
                h = np.clip(0.5 - 0.5 * (d + e) / kk, 0.0, 1.0)
                d = d * (1 - h) + (-e) * h + kk * h * (1 - h)
            else:
                d = _smin(d, e, k)
        return d

    def _rad(self, a, t):
        t = min(max(t, self.tz[0]), self.tz[-1])
        fi = np.interp(t, self.tz, np.arange(len(self.tz)))
        i0 = int(fi); i1 = min(i0 + 1, len(self.tz) - 1); ft = fi - i0
        fa = (a % TAU) / TAU * len(self.A)
        j0 = int(fa) % len(self.A); j1 = (j0 + 1) % len(self.A); fj = fa - int(fa)
        R = self.R
        r0 = R[i0, j0] * (1 - fj) + R[i0, j1] * fj
        r1 = R[i1, j0] * (1 - fj) + R[i1, j1] * fj
        return r0 * (1 - ft) + r1 * ft, self.cy[i0] * (1 - ft) + self.cy[i1] * ft

    def z_of(self, t):
        return self.zmid + self.zhalf * math.cos(math.pi * t)

    def z_to_t(self, z):
        return math.acos(max(-1.0, min(1.0, (z - self.zmid) / self.zhalf))) / math.pi

    def pt(self, a, t):
        if t <= 0.0:
            return Vector((0, float(self.cy[0]), self.ztop))
        if t >= 1.0:
            return Vector((0, float(self.cy[-1]), self.zbot))
        r, cy = self._rad(a, t)
        return Vector((math.sin(a) * r, cy + math.cos(a) * r, self.z_of(t)))

    def nrm(self, a, t):
        t = min(max(t, 0.006), 0.994)
        da, dt = 2e-3, 2e-3
        pa = self.pt(a + da, t) - self.pt(a - da, t)
        pt_ = self.pt(a, t + dt) - self.pt(a, t - dt)
        n = pa.cross(pt_)
        if n.length < 1e-12:
            return Vector((0, 0, 1 if t < 0.5 else -1))
        n.normalize()
        if n.dot(self.pt(a, t) - Vector((0, 0.03, 1.15))) < 0:
            n = -n
        return n

    def prof(self, t):
        return self.z_of(t), self._rad(math.pi / 2, t)[0], self._rad(math.pi, t)[0]

    def front_at(self, x, z):
        t = self.z_to_t(z)
        _, rx, _ = self.prof(t)
        return math.pi - math.asin(max(-0.97, min(0.97, x / max(rx, 1e-4)))), t

    def side_at(self, adeg, z):
        return math.pi - math.radians(adeg), self.z_to_t(z)

def build_head(part, H, reg, na=56, nt=36, color="skin"):
    rows = []
    for i in range(nt + 1):
        t = i / nt
        rows.append([H.pt(TAU * j / na, t) for j in range(na)])
    faces = part.grid(rows, color, wrap=True)
    for f in faces:
        f.normal_update()
        c = f.calc_center_median()
        if f.normal.y < -0.12 and c.z > FZ0 + 0.01:
            for l in f.loops:
                l[part.uv].uv = face_uv(reg, l.vert.co.x, l.vert.co.z)
    return faces


def build_shell(part, H, off, zb, reg, na=56, nr=16, edge=0.38):
    rows, uvr = [], []
    tbs = [H.z_to_t(zb(TAU * j / na)) for j in range(na + 1)]
    fr = [i / nr for i in range(nr + 1)]
    specs = [(f, None) for f in fr] + [(1.0, 0.012), (1.0, 0.02)]
    for k, (f, extra) in enumerate(specs):
        row, ur = [], []
        for j in range(na + 1):
            a = TAU * j / na
            tb = tbs[j]
            if extra is None:
                t = tb * f ** 0.9
                o = off(a, t) * (1 - edge * smoothstep(0.62 if edge > 0.5 else 0.72, 1.0, f))
            else:
                t = tb + extra
                o = off(a, tb) * 0.25 if extra < 0.015 else -0.004
            row.append(H.pt(a, t) + H.nrm(a, t) * o)
            ur.append(reg_uv(reg, j / na, 1 - 0.92 * min(k / nr, 1.0)))
        rows.append(row)
        uvr.append(ur)
    return part.grid(rows, uvrows=uvr, wrap=False)


def lock(part, H, ctrl, off, reg, w=0.07, h=0.026, n=13, k=8, groove=0.25, seed=0, sink=0.012, taper=0.75, root=0.0):
    """ctrl: list of (a, t, o); o=None -> lies on the shell (off+0.005)."""
    rng = random.Random(seed)
    cp = []
    for (a, t, o) in ctrl:
        cp.append(Vector((a, t, off(a, t) + 0.005 if o is None else o)))
    dense = cr_path(cp, 6)
    # resample by count
    idx = np.linspace(0, len(dense) - 1, n)
    samp = []
    for x in idx:
        i0 = int(math.floor(x))
        i1 = min(i0 + 1, len(dense) - 1)
        samp.append(dense[i0].lerp(dense[i1], x - i0))
    pts, nrms = [], []
    for i, s in enumerate(samp):
        a, t, o = s.x, min(max(s.y, 0.0), 0.995), s.z
        if i == 0:
            o -= sink
        nn = H.nrm(a, t)
        pts.append(H.pt(a, t) + nn * o)
        nrms.append(nn)
    T = [(pts[min(i + 1, n - 1)] - pts[max(i - 1, 0)]).normalized() for i in range(n)]
    u0 = rng.random() * 0.6
    rows, uvr = [], []
    for i in range(n):
        s = i / (n - 1)
        Bw = T[i].cross(nrms[i]).normalized()
        Nn = Bw.cross(T[i]).normalized()
        prof = (1 - s) ** taper if taper > 0 else (1 - s ** 3) ** 0.5
        rt = min(1.0, 0.3 + 0.7 * s / root) if root > 0 else 1.0
        wi = w * (0.8 + 0.2 * math.sin(math.pi / 2 * min(s / 0.3, 1))) * prof * rt + 0.0015
        hi = h * (1 - s) ** 0.5 * rt + 0.0012
        row, ur = [], []
        for j in range(k + 1):
            ph = TAU * j / k
            cx, cy = math.cos(ph), math.sin(ph)
            gy = cy * (1 - groove * math.exp(-(cx / 0.4) ** 2)) if cy > 0 else cy
            row.append(pts[i] + Bw * (cx * wi / 2) + Nn * (gy * hi / 2))
            ur.append(reg_uv(reg, u0 + 0.4 * j / k, 1 - s))
        rows.append(row)
        uvr.append(ur)
    return part.grid(rows, "hair_b" if reg == "hair_b" else "hair_g", uvrows=uvr, wrap=False, cap0=True, cap1=True)
