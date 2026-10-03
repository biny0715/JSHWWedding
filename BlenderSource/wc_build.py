# WeddingCouple v9 - builds groom & bride (parts kept as separate objects, joined only on export)
exec(open(r"D:\UnityProjects\JSHWWedding\BlenderSource\wc_lib.py", encoding="utf-8").read(), globals())


def ANG(n, a0=0.0):
    return [a0 + TAU * j / n for j in range(n)]


def jacket_keys():
    # z, sx, sy, half-gap angle of the front opening (rad, around the front a=pi)
    return [(0.47, 0.170, 0.128, 1.30), (0.53, 0.180, 0.134, 0.75), (0.58, 0.190, 0.142, 0.50),
            (0.64, 0.193, 0.145, 0.36), (0.70, 0.196, 0.147, 0.24), (0.76, 0.196, 0.147, 0.17),
            (0.80, 0.180, 0.137, 0.17), (0.83, 0.152, 0.117, 0.22), (0.855, 0.114, 0.094, 0.30),
            (0.877, 0.090, 0.085, 0.50)]


def key_at(keys, z):
    arr = np.array(keys, float)
    order = np.argsort(arr[:, 0])
    arr = arr[order]
    return [float(np.interp(z, arr[:, 0], arr[:, i])) for i in range(arr.shape[1])]


def front_y(sx, sy, x, n):
    s = min(abs(x) / sx, 0.999) ** (n / 2)
    c = math.sqrt(max(1 - s * s, 0))
    return -sy * c ** (2 / n)


def ribbon(part, pts, widths, thick, nfun, color, k=10, groove=0.0, sub=3, mat=0):
    P = cr_path(pts, sub)
    W = np.interp(np.linspace(0, 1, len(P)), np.linspace(0, 1, len(widths)), widths)
    Tn = np.interp(np.linspace(0, 1, len(P)), np.linspace(0, 1, len(thick)), thick)
    rows = []
    for i, p in enumerate(P):
        t = (P[min(i + 1, len(P) - 1)] - P[max(i - 1, 0)]).normalized()
        nn = nfun(p)
        b = t.cross(nn).normalized()
        nn = b.cross(t).normalized()
        row = []
        for j in range(k):
            ph = TAU * j / k
            cx, cy = math.cos(ph), math.sin(ph)
            gy = cy * (1 - groove * math.exp(-(cx / 0.35) ** 2)) if cy > 0 else cy
            row.append(p + b * (cx * W[i] / 2) + nn * (gy * Tn[i] / 2))
        rows.append(row)
    return part.grid(rows, color, wrap=True, cap0=True, cap1=True, mat=mat)


# ------------------------------------------------------------------ groom
def build_groom(root, coll, mats):
    H = BlobHead([
        ((0.0, 0.045, 1.150), (0.228, 0.226, 0.275), 0.0),       # cranium
        ((0.085, -0.05, 0.995), (0.128, 0.142, 0.100), 0.06),    # cheek R
        ((-0.085, -0.05, 0.995), (0.128, 0.142, 0.100), 0.06),   # cheek L
        ((0.0, -0.07, 0.945), (0.118, 0.128, 0.070), 0.05),      # jaw / chin (narrow, no heavy jaw plate)
        ((0.0, -0.193, 1.008), (0.021, 0.019, 0.018), 0.02),     # nose
        ((0.114, -0.205, 1.042), (0.055, 0.03, 0.04), -0.012),   # eye area R
        ((-0.114, -0.205, 1.042), (0.055, 0.03, 0.04), -0.012),  # eye area L
    ], ztop=1.427, zbot=0.874)
    out = []

    p = Part("G_Head")
    build_head(p, H, "face_g", color="skin_g")
    out.append(p)

    p = Part("G_Ears")
    for s in (-1, 1):
        rot = Matrix.Rotation(-s * 0.30, 3, "Z")
        p.ell((s * 0.250, 0.040, 0.988), (0.045, 0.045, 0.060), "skin_g", 16, 12, rot)
        p.ell((s * 0.274, 0.032, 0.986), (0.008, 0.024, 0.034), "ear_g", 10, 8, rot)   # shallow concha, flush with the ear
    out.append(p)

    def g_off(a, t):
        s2 = math.sin(a) ** 2
        back = max(0.0, math.cos(a))
        return (0.032 + 0.060 * s2 * math.exp(-((t - 0.44) / 0.22) ** 2)
                + 0.040 * back * math.exp(-((t - 0.48) / 0.26) ** 2) + 0.006 * (1 - t) ** 2)

    def g_zb(a):
        c = math.cos(a)
        if c < 0:  # front: high only at the parting (narrow forehead triangle), down to the brows on both sides
            side = smoothstep(0.04, 0.18, abs(a - math.pi))
            return 1.10 + 0.17 * (-c) ** 1.2 - 0.05 * side * (-c)
        return 1.10 - 0.14 * c ** 1.3

    p = Part("G_HairBase")
    build_shell(p, H, g_off, g_zb, "hair_g", edge=0.78)   # hair hugs the head at its lower edge: no mushroom brim
    out.append(p)

    F = lambda x, z, o=None: (*H.front_at(x, z), o)
    A = lambda d, z, o=None: (*H.side_at(d, z), o)
    # (control points, width, thickness, tip taper). Parting at x~-0.04: the large group sweeps diagonally to the
    # character's left (+x), a smaller group to the right (-x); a narrow strip of forehead shows between them.
    locks = [
        # wide base sweeps (one per side of the parting) so the strands on top never leave deep gaps
        ([F(-0.025, 1.452), F(0.03, 1.41), F(0.085, 1.33, 0.045), F(0.115, 1.25, 0.030), F(0.125, 1.19, 0.018)], 0.17, 0.030, 0.35),
        ([F(-0.055, 1.452), F(-0.10, 1.41), F(-0.135, 1.33, 0.045), F(-0.155, 1.25, 0.030), F(-0.16, 1.20, 0.018)], 0.13, 0.030, 0.35),
        # strands - sweep to the character's left (+x)
        ([F(-0.03, 1.448), F(0.0, 1.40, 0.050), F(0.03, 1.32, 0.036), F(0.05, 1.25, 0.025), F(0.058, 1.19, 0.017), F(0.06, 1.15, 0.013)], 0.085, 0.030, 0.5),
        ([F(-0.02, 1.45), F(0.03, 1.41, 0.054), F(0.08, 1.33, 0.040), F(0.11, 1.25, 0.028), F(0.125, 1.18, 0.018), F(0.13, 1.14, 0.014)], 0.10, 0.032, 0.45),
        ([F(-0.01, 1.452), F(0.06, 1.42, 0.056), F(0.13, 1.35, 0.042), F(0.17, 1.27, 0.030), F(0.185, 1.19, 0.019), F(0.185, 1.15, 0.015)], 0.11, 0.032, 0.45),
        ([F(0.0, 1.452), F(0.07, 1.42, 0.060), F(0.12, 1.33, 0.046), F(0.145, 1.23, 0.030), F(0.15, 1.15, 0.019), F(0.145, 1.10, 0.014)], 0.075, 0.028, 0.6),
        ([F(0.02, 1.452), F(0.12, 1.43), F(0.19, 1.37), A(66, 1.28, 0.048), A(70, 1.19, 0.036), A(70, 1.12, 0.030)], 0.12, 0.034, 0.45),
        ([F(-0.03, 1.455), F(0.04, 1.435, 0.068), F(0.10, 1.39, 0.064), F(0.15, 1.33, 0.054)], 0.11, 0.032, 0.35),
        # strands - sweep to the character's right (-x)
        ([F(-0.05, 1.446), F(-0.065, 1.38, 0.050), F(-0.078, 1.30, 0.036), F(-0.086, 1.23, 0.024), F(-0.088, 1.18, 0.015)], 0.075, 0.028, 0.5),
        ([F(-0.055, 1.45), F(-0.105, 1.40, 0.054), F(-0.14, 1.32, 0.040), F(-0.16, 1.24, 0.028), F(-0.168, 1.18, 0.017)], 0.10, 0.032, 0.45),
        ([F(-0.065, 1.452), F(-0.15, 1.42, 0.056), F(-0.20, 1.34, 0.044), A(-66, 1.26, 0.038), A(-69, 1.18, 0.031), A(-68, 1.12, 0.030)], 0.11, 0.032, 0.45),
        ([F(-0.04, 1.455), F(-0.09, 1.435, 0.068), F(-0.14, 1.39, 0.064), F(-0.17, 1.33, 0.054)], 0.11, 0.032, 0.35),
        ([F(-0.09, 1.45), F(-0.19, 1.41), A(-60, 1.32, 0.052), A(-63, 1.22, 0.042), A(-65, 1.13, 0.033)], 0.09, 0.030, 0.5),
    ]
    for s in (-1, 1):
        locks += [
            # side layers: end just above the ear, flaring out into the rounded silhouette
            ([A(s * 95, 1.43), A(s * 93, 1.32), A(s * 91, 1.21), A(s * 90, 1.13, 0.06), A(s * 89, 1.085, 0.035)], 0.12, 0.034, 0.45),
            ([A(s * 122, 1.43), A(s * 121, 1.31), A(s * 119, 1.19), A(s * 117, 1.10, 0.06), A(s * 116, 1.05, 0.035)], 0.12, 0.034, 0.45),
            ([A(s * 45, 1.452), A(s * 60, 1.40), A(s * 72, 1.32), A(s * 80, 1.24)], 0.11, 0.032, 0.0),
            ([A(s * 105, 1.40), A(s * 104, 1.28), A(s * 103, 1.18, 0.06), A(s * 102, 1.12, 0.035)], 0.10, 0.03, 0.45),
            ([A(s * 82, 1.40), A(s * 80, 1.30), A(s * 79, 1.20, 0.055), A(s * 78, 1.13, 0.032)], 0.09, 0.03, 0.45),
        ]
    # back: crown whorl -> two short layers that end at the nape (reference back view)
    for d in (132, 146, 160, 174, -172, -158, -144, -130):
        whorl = 0.25 * d + 0.75 * math.copysign(170, d)   # roots gather near the crown point
        locks.append(([A(whorl, 1.455), A(d, 1.40), A(d, 1.30), A(d, 1.20), A(d, 1.15, 0.065)],
                      0.13, 0.030, 0.4))
    for i, d in enumerate((128, 142, 156, 170, -176, -162, -148, -134)):
        zt = 0.99 - 0.03 * (i % 2)   # uneven tips so the nape doesn't read as a straight bowl cut
        dn = d * 0.92                 # tips converge towards the nape centre -> tapered back
        locks.append(([A(d, 1.36), A(d, 1.24), A((d + dn) / 2, 1.12), A(dn, zt + 0.04, 0.045), A(dn, zt, 0.025)], 0.11, 0.030, 0.6))
    t_tip = H.z_to_t(1.145)

    def neat(ctrl):
        # tidy bangs: front strands end just above the brows instead of falling over them
        return [(a, min(t, t_tip) if math.cos(a) < -0.5 else t, o) for (a, t, o) in ctrl]
    p = Part("G_HairLocks")
    rng_g = random.Random(31)
    for i, (ctrl, w, h, tp) in enumerate(locks):
        ctrl = neat(ctrl)
        front = math.cos(ctrl[-1][0]) < -0.6
        # bangs: thinner, slightly different widths so they no longer read as identical thick slabs
        hh = h * (0.62 if front else 1.0)
        ww = w * (rng_g.uniform(0.85, 1.1) if front else 1.0)
        lock(p, H, ctrl, g_off, "hair_g", w=ww, h=hh, seed=100 + i, taper=tp, root=0.2)
        # finer strand laid over every lock (bangs included) with its own curve, giving soft overlapping layers
        da = (0.03 if i % 2 else -0.03) * (0.6 if front else 1.0)
        dt = 0.012 if front else 0.0
        over = [(a + da * (0.4 + 0.6 * q / max(len(ctrl) - 1, 1)), t + dt * q / max(len(ctrl) - 1, 1),
                 (g_off(a + da, t) + 0.007) if o is None else o + 0.004) for q, (a, t, o) in enumerate(ctrl)]
        lock(p, H, neat(over), g_off, "hair_g", w=ww * 0.45, h=hh * 0.55, seed=300 + i, taper=max(tp, 0.55), root=0.3, sink=0.004)
    # short sideburns in front of the ears
    for s in (-1, 1):
        lock(p, H, [A(s * 82, 1.115, 0.030), A(s * 81, 1.07, 0.014), A(s * 80, 1.03, 0.007)], g_off, "hair_g",
             w=0.032, h=0.010, seed=500 + s, taper=0.45, sink=0.0)
    out.append(p)

    p = Part("G_Neck")
    p.tube([(0, 0, 0.80), (0, 0, 0.94)], [0.070, 0.068], "skin_g", sides=20, cap0=False, cap1=False)
    out.append(p)

    p = Part("G_Shirt")
    rows = [section(z, r, r * 0.95, 0, 0, 2.0, ANG(40)) for z, r in ((0.842, 0.077), (0.866, 0.077), (0.888, 0.073), (0.894, 0.066))]
    p.grid(rows, "shirt", wrap=True)
    tk = [(0.45, 0.152, 0.104), (0.50, 0.158, 0.111), (0.58, 0.165, 0.121), (0.66, 0.172, 0.125),
          (0.74, 0.176, 0.125), (0.79, 0.166, 0.116), (0.83, 0.132, 0.097), (0.86, 0.080, 0.075)]

    def vest_tips(pv, a):
        # pointed vest front at the bottom edge (two tips either side of the centre)
        if pv.z < 0.465:
            pv = pv.copy()
            pv.z -= 0.014 * math.exp(-((abs(a - math.pi) - 0.30) / 0.12) ** 2)   # restrained vest points
        return pv
    rows = [section(z, sx, sy, 0, 0, 2.6, ANG(56), vest_tips) for (z, sx, sy) in interp_rows(tk, 3)]
    p.grid(rows, "vest", wrap=True, cap0=True)
    # shirt bib: V-shaped panel lying on the torso front (smooth boundary, no stair-stepping)
    rows = []
    for zi in range(13):
        z = lerp(0.705, 0.865, zi / 12)
        hw = min(0.003 + (z - 0.705) * 0.28, 0.045)
        _, sx, sy = key_at(tk, z)
        row = []
        for xi in range(9):
            x = lerp(-hw, hw, xi / 8)
            row.append(Vector((x, front_y(sx, sy, x, 2.6) - 0.0025, z)))
        rows.append(row)
    p.grid(rows, "shirt", wrap=False, orient=False)  # winding +x then +z -> normal faces -y (front)
    for z in (0.675, 0.625, 0.575):
        _, sx, sy = key_at(tk, z)
        p.ell((0, -sy - 0.004, z), (0.009, 0.005, 0.009), "button", 10, 6)
    for s in (-1, 1):
        p.tube([(s * 0.226, -0.012, 0.517), (s * 0.228, -0.013, 0.487)], [0.045, 0.045], "shirt", sides=16)
    out.append(p)

    p = Part("G_Jacket")
    rows = []
    # one continuous tailcoat: front cut away below the waist, tails = back panel down to the calves
    tail = [(0.12, 0.196, 0.152, 2.80, 0.24), (0.15, 0.197, 0.152, 2.52, 0.16), (0.21, 0.196, 0.150, 2.15, 0.08),
            (0.31, 0.193, 0.147, 1.85, 0.03), (0.41, 0.189, 0.142, 1.58, 0.0)]
    upper = [k + (0.0,) for k in jacket_keys()[1:]]
    dense = interp_rows(tail + upper, 3)
    split = [r for r in dense if r[0] <= 0.42]
    full = [r for r in dense if r[0] >= split[-1][0]]
    for (z, sx, sy, g, v) in full:
        angs = [math.pi + g + (TAU - 2 * g) * k / 48 for k in range(49)]
        rows.append(section(z, sx, sy, 0, 0, 2.6, angs))
    p.grid(rows, "suit", wrap=False)
    # swallow tails: two panels below the waist, split by a back vent that widens towards the hem
    for side in (-1, 1):
        rows = []
        for (z, sx, sy, g, v) in split:
            a0, a1 = (math.pi + g, TAU - max(v, 0.004)) if side < 0 else (TAU + max(v, 0.004), 3 * math.pi - g)
            angs = [lerp(a0, a1, k / 24) for k in range(25)]
            rows.append(section(z, sx, sy, 0, 0, 2.6, angs))
        p.grid(rows, "suit", wrap=False)
    p.mods.append(("SOLIDIFY", {"thickness": 0.011, "offset": -1.0, "use_rim": True, "use_even_offset": True}))
    out.append(p)

    p = Part("G_Sleeves")
    for s in (-1, 1):
        p.tube([(s * 0.135, 0.0, 0.772), (s * 0.172, 0.0, 0.71), (s * 0.200, -0.004, 0.63), (s * 0.216, -0.01, 0.56), (s * 0.226, -0.012, 0.505)],
               [0.050, 0.051, 0.052, 0.052, 0.052], "suit", sides=16, cap0=False, cap1=True, sub=3)
        p.ell((s * 0.14, 0.0, 0.765), (0.056, 0.062, 0.040), "suit", 16, 10)
    out.append(p)

    p = Part("G_Lapels")
    jk = jacket_keys()
    # flat folded lapel lying on the jacket: widest on the chest, small notch below the collar, ends at the waist point
    lz = [0.877, 0.864, 0.852, 0.842, 0.82, 0.79, 0.75, 0.70, 0.65, 0.61, 0.588]
    lw = [0.022, 0.040, 0.024, 0.050, 0.058, 0.060, 0.057, 0.048, 0.035, 0.020, 0.004]
    for s in (1, -1):
        rows = []
        for i in range(31):
            z = lerp(lz[0], lz[-1], i / 30)
            w = float(np.interp(-z, [-q for q in lz], lw))
            _, sx, sy, g = key_at(jk, z)
            ae = math.pi - s * g
            ain = ae + s * 0.02
            aout = ae - s * w / max(sx, 0.05)
            row = []
            for kk in range(4):
                f = kk / 3
                q = section(z, sx, sy, 0, 0, 2.6, [lerp(ain, aout, f)])[0]
                nrm = Vector((q.x / sx ** 2, q.y / sy ** 2, 0)).normalized()
                row.append(q + nrm * 0.0030)   # flat folded cloth on the jacket
            rows.append(row)
        p.grid(rows, "lapel", wrap=False)
    p.mods.append(("SOLIDIFY", {"thickness": 0.0028, "offset": -1.0, "use_rim": True}))
    out.append(p)

    p = Part("G_Hands")
    for s in (-1, 1):
        p.ell((s * 0.232, -0.016, 0.462), (0.027, 0.035, 0.034), "skin_g", 14, 10)
        for y, ln, r in ((-0.040, 0.95, 0.0088), (-0.023, 1.0, 0.0094), (-0.006, 0.97, 0.0092), (0.010, 0.86, 0.0085)):
            p.tube([(s * 0.236, y, 0.444), (s * 0.239, y, 0.444 - 0.024 * ln), (s * 0.229, y - 0.002, 0.444 - 0.040 * ln),
                    (s * 0.216, y - 0.003, 0.444 - 0.043 * ln)], [r, r * 0.97, r * 0.9, r * 0.8], "skin_g", sides=8, sub=3)
        p.tube([(s * 0.222, -0.044, 0.468), (s * 0.215, -0.057, 0.450), (s * 0.211, -0.058, 0.434)], [0.0105, 0.0095, 0.008], "skin_g", sides=8, sub=3)
    out.append(p)

    p = Part("G_Trousers")
    # pelvis sits inside the vest hem (the old wide hip block read as a protruding panel)
    rows = [section(z, sx, sy, 0, 0, 2.4, ANG(40)) for (z, sx, sy) in ((0.445, 0.120, 0.058), (0.48, 0.130, 0.072))]   # hidden between the legs: no crotch lump
    p.grid(rows, "suit", wrap=True, cap0=True, cap1=True)
    for s in (-1, 1):
        keys = [(0.47, s * 0.078, 0.070), (0.38, s * 0.090, 0.079), (0.28, s * 0.096, 0.079), (0.18, s * 0.098, 0.078),
                (0.11, s * 0.099, 0.079), (0.068, s * 0.099, 0.083)]
        rows = []
        for (z, cx, r) in interp_rows(keys, 3):
            def disp(pv, a, z=z, cx=cx):
                wr = 0.0035 * math.sin(z * 140.0) * smoothstep(0.17, 0.09, z) * max(0.0, -math.cos(a))
                crease = 0.003 * math.exp(-((a - math.pi) / 0.18) ** 2)
                dv = Vector((pv.x - cx, pv.y, 0)).normalized()
                return pv + dv * (wr + crease)
            rows.append(section(z, r, r * 0.95, cx, 0.0, 2.2, ANG(28), disp))
        p.grid(rows, "suit", wrap=True, cap0=False, cap1=True)
    out.append(p)

    p = Part("G_Shoes")
    # rounder, larger dress shoes (reference: each shoe ~0.2 m wide incl. sole, glossy rounded toe)
    sk = [(0.072, 0.056, 0.050), (0.047, 0.068, 0.068), (0.0, 0.075, 0.075), (-0.06, 0.077, 0.066),
          (-0.11, 0.073, 0.054), (-0.153, 0.063, 0.041), (-0.177, 0.043, 0.029), (-0.187, 0.017, 0.014)]   # 85 %
    dense = interp_rows(sk, 3)
    for s in (-1, 1):
        cx = s * 0.1
        rows = []
        for (y, hw, hh) in dense:
            ring = []
            for j in range(24):
                ph = TAU * j / 24
                c, sn = math.cos(ph), math.sin(ph)
                x = cx + hw * math.copysign(abs(c) ** (2 / 2.6), c)
                z = 0.016 + (hh * abs(sn) ** (2 / 2.6) if sn >= 0 else -0.004 * abs(sn))
                ring.append(Vector((x, y, z)))
            rows.append(ring)
        p.grid(rows, "shoe", wrap=True, cap0=True, cap1=True)
        outl = [(cx + hw + 0.004, y) for (y, hw, hh) in dense] + [(cx - hw - 0.004, y) for (y, hw, hh) in reversed(dense)]
        p.grid([[Vector((x, y, z)) for (x, y) in outl] for z in (0.0, 0.017)], "sole", wrap=True, cap0=True, cap1=True)
    out.append(p)

    p = Part("G_BowTie")
    bc = Vector((0, -0.088, 0.852))
    wk = [(0.009, 0.008, 0.011), (0.018, 0.011, 0.019), (0.031, 0.012, 0.026), (0.045, 0.011, 0.028),
          (0.053, 0.008, 0.021), (0.057, 0.003, 0.009)]
    for s in (-1, 1):
        rows = []
        for (x, ry, rz) in interp_rows(wk, 3):
            ring = []
            for j in range(16):
                ph = TAU * j / 16
                yy, zz = math.cos(ph) * ry, math.sin(ph) * rz
                if math.cos(ph) < 0:
                    yy += 0.004 * math.exp(-(zz / (0.35 * rz + 1e-6)) ** 2) * (rz / 0.025)
                ring.append(bc + Vector((s * x, yy, zz * (1 - 0.18 * math.exp(-((x - 0.035) / 0.012) ** 2)))))
            rows.append(ring)
        p.grid(rows, "bow", wrap=True, cap0=True, cap1=True)
    p.ell(bc + Vector((0, -0.004, 0)), (0.012, 0.011, 0.013), "bow", 12, 8)
    out.append(p)

    p = Part("G_PocketSquare")
    z = 0.738
    _, sx, sy, g = key_at(jk, z)
    x = 0.122
    pc = Vector((x, front_y(sx, sy, x, 2.6) - 0.004, z))
    # folded white square: two small creased peaks above the welt
    for dx, ang, hgt in ((-0.007, 0.45, 0.012), (0.007, -0.35, 0.010)):
        tip = pc + Vector((dx * 0.6, -0.0015, 0.003 + hgt))
        rows = [[pc + Vector((dx - 0.009, 0, 0.0)), pc + Vector((dx, -0.003, 0.0)), pc + Vector((dx + 0.009, 0, 0.0)), pc + Vector((dx, 0.002, 0.0))],
                [tip + Vector((-0.001, 0, 0)), tip + Vector((0, -0.0008, 0)), tip + Vector((0.001, 0, 0)), tip + Vector((0, 0.0008, 0))]]
        p.grid(rows, "pocket", wrap=True, cap0=True, cap1=True)
    p.ell(pc + Vector((0, 0.001, -0.003)), (0.028, 0.004, 0.0045), "lapel", 12, 6)
    out.append(p)

    return [q.finish(root, mats, coll) for q in out]


# WebGL budget: non-destructive Decimate (collapse) per part, applied only on export. Head/face, veil and
# hair shell are left untouched (face-projection / lace UVs).
DECIMATE = {"G_Jacket": 0.5, "G_Shirt": 0.5, "G_Shoes": 0.45, "G_Trousers": 0.6, "G_Sleeves": 0.6, "G_BowTie": 0.5,
            "G_Hands": 0.85, "G_Ears": 0.6, "G_HairLocks": 0.75, "G_PocketSquare": 0.6,
            "B_Bouquet": 0.6, "B_Dress": 0.6, "B_Body": 0.6, "B_Drape": 0.6, "B_HairLocks": 0.75, "B_Earrings": 0.5}


SQUASH["G_"] = (1.13, 0.12, 0.5, 0.017)   # groom: brow-top -> crown 50%, lowered with the brows (forehead shrinks)
SQUASH["B_"] = (1.21, 0.10, 0.7)    # bride: brow-top -> crown 70%
for _n in ("B_Head", "B_HairBase", "B_HairLocks", "B_Earrings", "B_Veil"):
    DROP[_n] = 0.014                 # shorter chin raised the jaw line; sit the head lower so the neck stays short


# ------------------------------------------------------------------ bride
BUN_C = Vector((-0.15, 0.225, 0.978))


def build_bride(root, coll, mats):
    # one continuous blended surface (modeling-sheet clay head): round cranium set back from the face, chubby cheeks
    # low on the face, small round chin, tiny nose, shallow eye areas
    H = BlobHead([
        ((0.0, 0.045, 1.175), (0.235, 0.228, 0.270), 0.0),     # cranium
        ((0.085, -0.055, 1.035), (0.130, 0.146, 0.095), 0.06),   # cheek R
        ((-0.085, -0.055, 1.035), (0.130, 0.146, 0.095), 0.06),  # cheek L
        ((0.0, -0.075, 0.990), (0.112, 0.128, 0.064), 0.05),     # jaw / chin (short: eye->chin ~0.33 of face width)
        ((0.0, -0.199, 1.034), (0.023, 0.020, 0.019), 0.02),     # nose
        ((0.12, -0.212, 1.08), (0.062, 0.03, 0.048), -0.012),  # eye area R (shallow)
        ((-0.12, -0.212, 1.08), (0.062, 0.03, 0.048), -0.012), # eye area L
    ], ztop=1.447, zbot=0.922)
    out = []

    p = Part("B_Head")
    build_head(p, H, "face_b")
    for s in (-1, 1):
        # ear set back on the head (reference side view: ~70% of the head depth behind the face)
        p.ell((s * 0.252, 0.048, 1.048), (0.034, 0.045, 0.054), "skin", 12, 10, Matrix.Rotation(-s * 0.62, 3, "Z"))
        p.ell((s * 0.270, 0.036, 1.046), (0.008, 0.024, 0.032), "ear_in", 8, 6, Matrix.Rotation(-s * 0.62, 3, "Z"))
    out.append(p)

    def b_off(a, t):
        s2 = math.sin(a) ** 2
        part = 1 - 0.5 * math.exp(-((a - math.pi) / 0.022) ** 2) * (1 - smoothstep(0.25, 0.4, t))  # thin parting groove
        return (0.026 + 0.020 * s2 * math.exp(-((t - 0.45) / 0.20) ** 2) + 0.008 * (1 - t)) * part   # calm sides, no puff

    def b_zb(a):
        c = math.cos(a)
        # side hairline sweeps back above the ear, leaving the temple/cheek open in front of it
        if c < 0:
            u = math.degrees(abs(math.pi - (a % TAU)))
            # arch from the parting, over the outer brow ends, down the face sides (slimming), then back above the ear
            return float(np.interp(u, [0, 10, 25, 40, 50, 60, 72, 85, 95], [1.335, 1.331, 1.30, 1.258, 1.21, 1.15, 1.09, 1.06, 1.10]))
        return 1.10 - 0.18 * smoothstep(0.25, 1.0, c)   # clear the ear, then down to the nape

    p = Part("B_HairBase")
    build_shell(p, H, b_off, b_zb, "hair_b")
    out.append(p)

    A = lambda d, z, o=None: (*H.side_at(d, z), o)
    # strands start right at the parting line and follow the framing hairline down to the temple, then back to the bun
    flows = [
        [(1.5, 1.335), (10, 1.331), (25, 1.30), (40, 1.258), (50, 1.21), (60, 1.15), (72, 1.09), (85, 1.06), (98, 1.095), (118, 1.07), (140, 1.02), (160, 0.99)],
        [(1.5, 1.375), (14, 1.365), (30, 1.335), (46, 1.29), (58, 1.23), (70, 1.16), (84, 1.11), (100, 1.12), (124, 1.075), (150, 1.02), (165, 0.99)],
        [(1.5, 1.41), (20, 1.397), (38, 1.362), (56, 1.30), (72, 1.23), (92, 1.16), (118, 1.10), (145, 1.03), (166, 1.0)],
        [(1.5, 1.445), (28, 1.42), (52, 1.37), (76, 1.30), (102, 1.21), (128, 1.12), (152, 1.05), (170, 1.02)],
        [(2, 1.458), (50, 1.45), (95, 1.40), (130, 1.30), (155, 1.18), (170, 1.08), (177, 1.04)],
    ]
    p = Part("B_HairLocks")
    k = 0
    def resample(fl, n=9):
        # fixed number of points along a flow so neighbouring flows can be blended
        d = np.array([q[0] for q in fl], float); z = np.array([q[1] for q in fl], float)
        L = np.concatenate([[0], np.cumsum(np.hypot(np.diff(d) / 60.0, np.diff(z)))]); L /= L[-1]
        u = np.linspace(0, 1, n)
        return np.stack([np.interp(u, L, d), np.interp(u, L, z)], 1)

    RF = [resample(fl) for fl in flows]
    rng_h = random.Random(77)
    for s in (-1, 1):
        # 11 strands per side, each a blend between two neighbouring guide flows -> continuous sweep, no repeated bands
        for i in range(11):
            f = i / 10 * (len(RF) - 1)
            i0 = min(int(f), len(RF) - 2); ft = f - i0
            fl = (1 - ft) * RF[i0] + ft * RF[i0 + 1]
            jit = rng_h.uniform(-1.0, 1.0)
            pts = [(4.0 + 1.4 * i + (0.9 if s > 0 else 0.0) + jit, fl[0][1] - 0.004 * rng_h.random())]
            pts += [(d + jit * 1.5 * (1 - q / 8), z + 0.004 * jit * math.sin(q)) for q, (d, z) in enumerate(fl[1:], 1)]
            ctrl = [A(s * d, z, None) for (d, z) in pts]
            w = rng_h.uniform(0.050, 0.072)
            lock(p, H, ctrl, b_off, "hair_b", w=w, h=rng_h.uniform(0.008, 0.012), n=18, seed=200 + k, groove=0.0,
                 root=0.35, sink=0.014, taper=rng_h.uniform(0.45, 0.7))
            k += 1
    # thin scalp-coloured parting line between the two sides
    ppts = [H.pt(*H.side_at(0, z)) + H.nrm(*H.side_at(0, z)) * 0.012 for z in (1.335, 1.375, 1.41, 1.445, 1.458)]
    p.tube(ppts, [0.0026] * 5, "ear_in", sides=6, sub=3)
    # low bun: core + coiled strand
    p.ell(BUN_C, (0.100, 0.075, 0.092), "hair_b", 16, 12)
    e1, e2, ax = Vector((1, 0, 0)), Vector((0, 0, 1)), Vector((0, 1, 0))
    coil = []
    for i in range(48):
        th = 4.2 * math.pi * i / 47
        R = 0.083 - 0.052 * i / 47
        coil.append(BUN_C + e1 * (math.cos(th) * R) + e2 * (math.sin(th) * R * 0.9) + ax * (0.024 + 0.04 * i / 47))
    p.tube(coil, list(np.linspace(0.034, 0.018, len(coil))), "hair_b", sides=10, uvreg="hair_b")
    # twisted rope braid along each side: temple -> above the ear -> into the bun
    for s in (-1, 1):
        cps = [A(s * 62, 1.30), A(s * 85, 1.22), A(s * 108, 1.14), A(s * 130, 1.07), A(s * 150, 1.02)]
        path = [H.pt(a, t) + H.nrm(a, t) * (b_off(a, t) + 0.010) for (a, t, o) in cps]
        path.append(BUN_C + Vector((s * 0.02 if s > 0 else 0.03, -0.02, 0.03)))
        P = cr_path(path, 10)
        T, N, Bn = frames(P)
        for ph0 in (0.0, math.pi):
            strand = []
            for i, q in enumerate(P):
                ang = ph0 + i * 0.55
                strand.append(q + (N[i] * math.cos(ang) + Bn[i] * math.sin(ang)) * 0.009)
            rr = [0.0115 * (1 - 0.35 * i / len(P)) for i in range(len(P))]
            p.tube(strand, rr, "hair_b", sides=8, uvreg="hair_b")
    # wisps in front of the ears
    for s in (-1, 1):
        wp = [A(s * 84, 1.13, 0.008), A(s * 86, 1.09, 0.016), A(s * 83, 1.05, 0.021), A(s * 87, 1.015, 0.024), A(s * 84, 0.99, 0.027), A(s * 87, 0.972, 0.03)]
        pts = [H.pt(a, t) + H.nrm(a, t) * o for (a, t, o) in wp]
        p.tube(pts, [0.0045, 0.0042, 0.0038, 0.0032, 0.0025, 0.0012], "hair_b", sides=6, sub=4)
    out.append(p)

    p = Part("B_Earrings")
    for s in (-1, 1):
        p.ell((s * 0.256, 0.038, 0.998), (0.0075, 0.005, 0.0075), "metal", 6, 4)       # small stud on the lobe
        p.tube([(s * 0.256, 0.038, 0.992), (s * 0.257, 0.037, 0.981)], [0.0018, 0.0018], "metal", sides=6)
        p.ell((s * 0.257, 0.036, 0.960), (0.021, 0.021, 0.021), "pearl", 16, 12)
    out.append(p)

    p = Part("B_Body")
    p.tube([(0, 0, 0.80), (0, 0.004, 0.95)], [0.049, 0.046], "skin", sides=20, cap0=False, cap1=False)
    SK = [(0.70, 0.100, 0.074), (0.75, 0.114, 0.081), (0.785, 0.140, 0.088), (0.805, 0.140, 0.086),
          (0.822, 0.130, 0.080), (0.836, 0.112, 0.071), (0.848, 0.088, 0.060), (0.858, 0.062, 0.050), (0.868, 0.045, 0.043)]
    p.grid([section(z, sx, sy, 0, 0, 2.3, ANG(40)) for (z, sx, sy) in interp_rows(SK, 3)], "skin", wrap=True, cap0=True)
    for s in (-1, 1):
        p.ell((s * 0.146, -0.002, 0.787), (0.033, 0.035, 0.03), "skin", 16, 12)   # deltoid cap joining shoulder and arm
    for s in (-1, 1):
        # upper arm -> rounded elbow -> slightly fuller forearm -> slim wrist, bent forward to the bouquet
        path = [(s * 0.142, 0.0, 0.80), (s * 0.165, -0.004, 0.75), (s * 0.176, -0.015, 0.69), (s * 0.174, -0.035, 0.65),
                (s * 0.148, -0.085, 0.605), (s * 0.104, -0.13, 0.575), (s * 0.060, -0.158, 0.562)]
        p.tube(path, [0.031, 0.029, 0.024, 0.020, 0.0235, 0.020, 0.0145], "skin", sides=14, cap0=False, cap1=True, sub=4)
    # hands closed around the stem bundle (stems at x~0, y~-0.178): left hand above, right hand below
    for s, zc in ((1, 0.575), (-1, 0.545)):
        p.ell((s * 0.034, -0.168, zc), (0.020, 0.025, 0.029), "skin", 12, 10)
        for dz in (0.013, 0.004, -0.005, -0.014):
            z = zc + dz
            p.tube([(s * 0.032, -0.186, z), (s * 0.013, -0.199, z), (-s * 0.004, -0.197, z), (-s * 0.013, -0.187, z)],
                   [0.0068, 0.0066, 0.006, 0.0048], "skin", sides=8, sub=3)
        p.ell((s * 0.016, -0.192, zc + 0.022), (0.008, 0.007, 0.013), "skin", 8, 6, Matrix.Rotation(s * 0.5, 3, "Y"))
    out.append(p)

    p = Part("B_Dress")
    dk = [(0.000, 0.345, 0.300, 0, 0.050), (0.020, 0.330, 0.288, 0, 0.045), (0.045, 0.300, 0.262, 0, 0.034),
          (0.090, 0.250, 0.218, 0, 0.020), (0.140, 0.210, 0.184, 0, 0.010), (0.190, 0.180, 0.158, 0, 0.004),
          (0.260, 0.150, 0.132, 0, 0.0), (0.330, 0.138, 0.120, 0, 0.0), (0.420, 0.138, 0.117, 0, 0.0),
          (0.500, 0.132, 0.110, 0, 0.0), (0.580, 0.112, 0.090, 0, 0.0), (0.640, 0.111, 0.088, 0, 0.0),
          (0.700, 0.123, 0.093, 0, 0.0), (0.745, 0.128, 0.094, 0, 0.0), (0.775, 0.128, 0.092, 0, 0.0)]
    rows = []
    for (z, sx, sy, cx, cy) in interp_rows(dk, 3):
        def disp(pv, a, z=z, cy=cy):
            hem = smoothstep(0.22, 0.0, z)
            ripple = 0.6 * math.sin(14 * a + 0.5 * math.sin(5 * a)) + 0.4 * math.sin(23 * a + 1.3)   # many small waves
            amp = 0.006 * hem * ripple + 0.004 * hem * math.sin(4 * a + 0.6) \
                + 0.0018 * smoothstep(0.5, 0.25, z) * math.sin(9 * a + 2.0)
            dv = Vector((pv.x, pv.y - cy, 0))
            return pv + dv.normalized() * amp if dv.length > 1e-6 else pv
        rows.append(section(z, sx, sy, cx, cy, 2.2, [TAU * j / 72 for j in range(73)], disp))
    uvr = [[reg_uv("dress", j / 72, min(r[0].z / 0.78, 1.0)) for j in range(73)] for r in rows]
    p.grid(rows, "dress", uvrows=uvr, wrap=False, cap0=True, cap1=True)
    out.append(p)

    # off-shoulder: thin satin bands that lie on the bodice / skin and wrap the upper arms
    p = Part("B_Drape")
    radial = lambda q: Vector((q.x, q.y, 0)).normalized()
    DK3 = [k[:3] for k in dk]

    def torso(x, z, off, back=False):
        keys, n = (SK, 2.3) if z >= 0.78 else (DK3, 2.2)
        _, sx, sy = key_at(keys, z)
        y = front_y(sx, sy, x, n) * (-1 if back else 1)
        return Vector((x, y, z)) + Vector((x / sx ** 2, y / sy ** 2, 0)).normalized() * off

    def arm(s, ph, z, off):
        c = Vector((s * (0.142 + 0.46 * (0.80 - z)), -0.004 - 0.1 * (0.80 - z), z))
        r = 0.030 - 0.035 * (0.80 - z)
        return c + Vector((s * math.sin(ph), -math.cos(ph), 0)) * (r + off)

    def band(s, z0, z1, off, extra=0.0):
        xs = (0.12, 0.06, 0.0, -0.06, -0.12)
        pts = [arm(s, 2.6, z0 + 0.002, off), arm(s, 1.8, z0 + 0.001, off), arm(s, 1.0, z0 - 0.001, off), arm(s, 0.35, z0 - 0.004, off)]
        for i, x in enumerate(xs):
            z = lerp(z0 - 0.008, z1 + 0.008, i / 4)
            pts.append(torso(s * x, z, off + extra * math.exp(-(x / 0.07) ** 2)))
        pts += [arm(-s, 0.35, z1 + 0.004, off), arm(-s, 1.0, z1 + 0.002, off), arm(-s, 1.8, z1 + 0.001, off), arm(-s, 2.6, z1, off)]
        return pts

    ribbon(p, band(1, 0.782, 0.726, 0.0055, 0.004), [0.050] * 13, [0.0055] * 13, radial, "drape", k=12, groove=0.6, sub=2)
    ribbon(p, band(-1, 0.782, 0.726, 0.0055), [0.050] * 13, [0.0055] * 13, radial, "drape", k=12, groove=0.6, sub=2)
    top = [arm(1, 2.4, 0.792, 0.008), arm(1, 1.4, 0.792, 0.008), arm(1, 0.5, 0.79, 0.008), torso(0.12, 0.788, 0.008), torso(0.0, 0.784, 0.008),
           torso(-0.12, 0.788, 0.008), arm(-1, 0.5, 0.79, 0.008), arm(-1, 1.4, 0.792, 0.008), arm(-1, 2.4, 0.792, 0.008)]
    ribbon(p, top, [0.022] * 9, [0.0065] * 9, radial, "drape", k=12, groove=0.5, sub=2)   # folded top edge
    back = [arm(1, 2.3, 0.778, 0.007), torso(0.12, 0.778, 0.007, True), torso(0.0, 0.778, 0.007, True),
            torso(-0.12, 0.778, 0.007, True), arm(-1, 2.3, 0.778, 0.007)]
    ribbon(p, back, [0.055] * 5, [0.0055] * 5, radial, "drape", k=12, groove=0.5, sub=3)
    for s in (-1, 1):
        ax = Vector((s * 0.028, -0.004, -0.05))
        p.torus((s * 0.160, -0.006, 0.754), ax, 0.034, 0.0055, "drape", seg=24, mseg=6, sx=1.0, sy=1.0)
        p.torus((s * 0.166, -0.008, 0.737), ax, 0.032, 0.005, "drape", seg=24, mseg=6)
    out.append(p)

    p = Part("B_Bouquet")
    Dc = Vector((0, -0.202, 0.620))
    dr = Vector((0.092, 0.052, 0.066))
    p.ell(Dc + Vector((0, 0.016, -0.006)), (0.058, 0.03, 0.04), "leaf_d", 12, 8)   # dark greenery between the blooms
    rng = random.Random(5)
    golden = math.pi * (3 - math.sqrt(5))
    cand = []
    for i in range(160):
        zz = 1 - 2 * (i + 0.5) / 160
        rr = math.sqrt(1 - zz * zz)
        th = golden * i
        d = Vector((math.cos(th) * rr, math.sin(th) * rr, zz))
        if d.y < 0.1 and d.z > -0.6:
            cand.append(d)

    def frame_of(n):
        e1 = n.orthogonal().normalized()
        return e1, n.cross(e1)

    def rose_bud(c, n, r):
        # closed fresh bud: 4 cupped petals wrapped around a core, slightly opening at the top
        e1, e2 = frame_of(n)
        base = rng.random() * TAU
        for kk in range(4):
            th = base + TAU * kk / 4
            rad = e1 * math.cos(th) + e2 * math.sin(th)
            tan = n.cross(rad)
            tilt = Matrix.Rotation(-0.28, 3, tan)          # petal tips lean in towards the bud centre
            rot = tilt @ Matrix((rad, tan, n)).transposed()
            p.ell(c + rad * (r * 0.42) + n * (r * 0.05), (r * 0.42, r * 0.78, r * 0.95), "flower", 8, 6, rot)
        p.ell(c + n * (r * 0.30), (r * 0.55, r * 0.55, r * 0.6), "bud", 8, 6)

    def open_flower(c, n, r):
        # small five-petal bloom with cupped petals and a yellow-green heart
        e1, e2 = frame_of(n)
        base = rng.random() * TAU
        for kk in range(5):
            th = base + TAU * kk / 5
            rad = e1 * math.cos(th) + e2 * math.sin(th)
            tan = n.cross(rad)
            rot = Matrix.Rotation(-0.45, 3, tan) @ Matrix((rad, tan, n)).transposed()
            p.ell(c + rad * (r * 0.85) + n * (r * 0.25), (r * 0.8, r * 0.55, r * 0.22), "flower", 6, 4, rot)
        p.ell(c + n * (r * 0.32), (r * 0.32, r * 0.32, r * 0.25), "fcenter", 6, 4, Matrix((e1, e2, n)).transposed())

    picks = [cand[int(i * len(cand) / 24)] for i in range(24)]
    for i, d in enumerate(picks):
        c = Dc + Vector((d.x * dr.x, d.y * dr.y, d.z * dr.z))
        n = Vector((d.x / dr.x, d.y / dr.y, d.z / dr.z)).normalized()
        if i % 3 == 1:
            open_flower(c, n, rng.uniform(0.0105, 0.0125))
        else:
            rose_bud(c + n * 0.004, n, rng.uniform(0.0125, 0.0155))
    # baby's breath: tiny white florets filling the gaps on the outer surface
    for i in range(46):
        d = cand[(i * 37 + 11) % len(cand)]
        c = Dc + Vector((d.x * dr.x * 1.08, d.y * dr.y * 1.12, d.z * dr.z * 1.08))
        for jj in range(2):
            o = Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), rng.uniform(-1, 1))) * 0.006
            p.ell(c + o, (0.0032, 0.0032, 0.0032), "flower", 6, 4)
    # pointed leaves radiating out between and behind the blooms (varied length / angle, slight curl)
    for i, thd in enumerate((-50, -30, -12, 6, 24, 42, 140, 158, 176, 194, 212, 232, 255, 285, 305, 330)):
        th = math.radians(thd + rng.uniform(-6, 6))
        dirv = Vector((math.cos(th), 0.0, math.sin(th)))
        L = rng.uniform(0.055, 0.075)
        base = Dc + Vector((0, 0.02, -0.006)) + dirv * 0.05
        tip = base + dirv * L + Vector((0, 0.012 + rng.uniform(0, 0.01), 0))
        mid = base.lerp(tip, 0.45) + Vector((0, -0.007, 0))
        ribbon(p, [base, mid, tip], [0.005, 0.02, 0.0015], [0.004, 0.0045, 0.0015], lambda q: Vector((0, -1, 0)),
               "leaf" if i % 2 else "leaf_d", k=6, groove=0.55)
    for xi in (-0.015, -0.009, -0.003, 0.003, 0.009, 0.015):
        p.tube([(xi, -0.182, 0.60), (xi * 0.35, -0.178, 0.53), (xi * 0.6, -0.175, 0.40)], [0.0045, 0.0045, 0.0042], "stem", sides=6, sub=3)
    bw = Vector((0, -0.190, 0.520))
    p.torus(bw + Vector((0, 0.004, 0)), (0, 0, 1), 0.012, 0.006, "ribbon", seg=16, mseg=6)
    for s in (-1, 1):
        p.ell(bw + Vector((s * 0.038, -0.003, 0.006)), (0.036, 0.012, 0.022), "ribbon", 14, 8, Matrix.Rotation(s * 0.25, 3, "Y"))
        p.ell(bw + Vector((s * 0.040, -0.012, 0.006)), (0.018, 0.004, 0.010), "dress_in", 10, 6, Matrix.Rotation(s * 0.25, 3, "Y"))
        ribbon(p, [bw + Vector((s * 0.005, -0.009, -0.004)), bw + Vector((s * 0.016, -0.012, -0.04)), bw + Vector((s * 0.026, -0.011, -0.08)),
                   bw + Vector((s * 0.034, -0.006, -0.12))], [0.02, 0.022, 0.023, 0.024], [0.0035] * 4, lambda q: Vector((0, -1, 0)), "ribbon", k=6)
    p.ell(bw + Vector((0, -0.012, 0)), (0.012, 0.010, 0.013), "ribbon", 10, 8)
    out.append(p)
    p = Part("B_Veil")
    build_veil(p)
    out.append(p)
    return [q.finish(root, mats, coll) for q in out]


def build_veil(part, NU=34, NV=26):
    obst = [(Vector((0, 0.02, 1.19)), Vector((0.345, 0.325, 0.335))),
            (BUN_C + Vector((0, 0.02, 0)), Vector((0.11, 0.09, 0.10))),
            (Vector((0, 0.0, 0.79)), Vector((0.265, 0.15, 0.085)))]

    def push(q):
        for c, r in obst:
            d = Vector(((q.x - c.x) / r.x, (q.y - c.y) / r.y, (q.z - c.z) / r.z))
            L = d.length
            if L < 1.0:
                d = d / max(L, 1e-6)
                q = Vector((c.x + d.x * r.x, c.y + d.y * r.y, c.z + d.z * r.z))
        return q

    cols = []
    for iu in range(NU + 1):
        u = iu / NU
        th = math.pi * (u - 0.5)
        # comb line across the crown, set back from the hairline (reference side/back views: the veil starts at the
        # top-back of the head and hangs only behind/beside the body, never in front of the face or arms)
        C = Vector((-0.315 * math.sin(th), -0.03 + 0.07 * (1 - math.cos(th)), 1.30 + 0.225 * math.cos(th)))
        be = 1.45 * (2 * u - 1)
        R = 0.40 + 0.10 * (abs(be) / 1.45) ** 2
        Bp = Vector((-R * math.sin(be) * 1.12, R * math.cos(be) + 0.06, 0.52 + 0.20 * (abs(be) / 1.45) ** 2.2))
        col = []
        for iv in range(NV + 1):
            v = iv / NV
            e = 1 - (1 - v) ** 2
            col.append(Vector((lerp(C.x, Bp.x, e), lerp(C.y, Bp.y, e), lerp(C.z, Bp.z, v))))
        cols.append(col)
    for it in range(10):
        for iu in range(NU + 1):
            col = [push(q) for q in cols[iu]]
            if it < 9:
                col = [col[0]] + [(col[i - 1] + col[i] * 2 + col[i + 1]) / 4 for i in range(1, NV)] + [col[-1]]
            cols[iu] = col
    rows, uvr = [], []
    for iv in range(NV + 1):
        v = iv / NV
        row, ur = [], []
        for iu in range(NU + 1):
            u = iu / NU
            q = cols[iu][iv].copy()
            rad = Vector((q.x, q.y, 0))
            if rad.length > 1e-6:
                # soft gravity folds: broad + finer waves that grow towards the hem, slightly irregular
                fold = 0.65 * math.sin(TAU * 5.5 * u + 0.4) + 0.35 * math.sin(TAU * 11.0 * u + 1.7 + 0.8 * math.sin(TAU * 2 * u))
                q = q + rad.normalized() * (0.024 * v ** 1.4 * fold)
            row.append(q)
            ur.append(reg_uv("veil", u, 1 - v))
        rows.append(row)
        uvr.append(ur)
    part.grid(rows, "dress", uvrows=uvr, wrap=False, mat=1)


# ------------------------------------------------------------------ materials / scene
def make_materials(img):
    m = bpy.data.materials.get("WeddingCouple_Palette") or bpy.data.materials.new("WeddingCouple_Palette")
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = img
    tex.interpolation = "Linear"
    inv = nt.nodes.new("ShaderNodeMath")
    inv.operation = "SUBTRACT"
    inv.inputs[0].default_value = 1.0
    nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    nt.links.new(tex.outputs["Alpha"], inv.inputs[1])
    nt.links.new(inv.outputs[0], bsdf.inputs["Roughness"])
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    v = bpy.data.materials.get("WeddingCouple_Veil") or bpy.data.materials.new("WeddingCouple_Veil")
    v.use_nodes = True
    nt = v.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = img
    nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    nt.links.new(tex.outputs["Alpha"], bsdf.inputs["Alpha"])
    bsdf.inputs["Roughness"].default_value = 0.75
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    v.use_backface_culling = False
    v.surface_render_method = "BLENDED"
    return m, v


def reset_scene():
    for name in ("WeddingCouple_v2",):
        c = bpy.data.collections.get(name)
        if c:
            for ob in list(c.all_objects):
                bpy.data.objects.remove(ob, do_unlink=True)
            for ch in list(c.children_recursive):
                bpy.data.collections.remove(ch)
            bpy.data.collections.remove(c)
    for n in ("Groom", "Bride"):
        ob = bpy.data.objects.get(n)
        if ob:
            bpy.data.objects.remove(ob, do_unlink=True)
    for me in list(bpy.data.meshes):
        if me.users == 0:
            bpy.data.meshes.remove(me)


reset_scene()
top = bpy.data.collections.new("WeddingCouple_v2")
bpy.context.scene.collection.children.link(top)
img = build_atlas(
    dict(blush=(0.166, 1.004), blush_r=(0.062, 0.042), blush_a=0.58, nose_z=1.033, nose_a=0.65, mouth=(0.040, 0.994, 0.009, 0.0025),
         mouth_w=0.0044, mouth_col=(214, 96, 84), brow=((0.072, 1.180), (0.122, 1.198), (0.174, 1.186)), brow_w=(0.020, 0.018, 0.007),
         brow_col=(58, 40, 34), eye=(0.121, 1.078),
         eye_p=dict(a=0.060, b=0.046, pu=1.8, pl=2.1, tilt=0.03, ir=0.045, idx=0.006, idz=0.006, iris_top=(22, 15, 13), iris_bot=(86, 54, 40),
                    glow=(118, 78, 56), pupil=0.46, lw=0.0082, lid=(32, 20, 17), wing=0.22, wing_rise=0.26,
                    lashes=[(0.52, 64, 0.010), (0.75, 46, 0.012), (0.95, 30, 0.014)], lower=0.6, crease=0.45)),
    dict(skin="skin_g", blush=(0.160, 0.985), blush_r=(0.055, 0.036), blush_a=0.52, nose_z=1.012, nose_a=0.55, mouth=(0.036, 0.976, 0.007, 0.002),
         mouth_w=0.0040, mouth_col=(206, 98, 86), brow=((0.072, 1.108), (0.114, 1.119), (0.158, 1.108)), brow_w=(0.017, 0.015, 0.007),
         brow_col=(38, 30, 28), eye=(0.114, 1.042),
         eye_p=dict(a=0.047, b=0.025, tilt=-0.06, ir=0.029, idx=0.009, idz=-0.002, iris_top=(24, 17, 15), iris_bot=(80, 56, 44),
                    glow=(106, 74, 58), lw=0.0060, lid=(32, 24, 21), wing=0.12, wing_rise=0.05, lashes=[], lower=0.3, crease=0.0)))
mats = make_materials(img)
mats = [mats[0], mats[1]]
results = {}
for name, x, fn in (("Groom", 0.45, build_groom), ("Bride", -0.45, build_bride)):
    cc = bpy.data.collections.new(name + "_v2")
    top.children.link(cc)
    root = bpy.data.objects.new(name + "_Root", None)
    root.empty_display_type = "PLAIN_AXES"
    root.location = (x, 0, 0)
    cc.objects.link(root)
    obs = fn(root, cc, mats)
    for ob in obs:
        if ob.name in DECIMATE:
            md = ob.modifiers.new("decimate", "DECIMATE")
            md.ratio = DECIMATE[ob.name]
            md.use_collapse_triangulate = True
    dg = bpy.context.evaluated_depsgraph_get()
    tris = 0
    for ob in obs:
        ev = ob.evaluated_get(dg)
        me = ev.to_mesh()
        tris += sum(len(pp.vertices) - 2 for pp in me.polygons)
        ev.to_mesh_clear()
    results[name] = (len(obs), tris)
print("built", results)
