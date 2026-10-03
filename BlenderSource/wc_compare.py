# Side-by-side + overlay comparison of front renders against the reference (same metres-per-pixel framing).
# usage: python wc_compare.py <reference.png> <TAG>
import sys, os
from PIL import Image, ImageDraw

REF = sys.argv[1]
RDIR = r"D:\UnityProjects\JSHWWedding\BlenderSource\renders"
TAG = sys.argv[2]
im = Image.open(REF).convert("RGB")


def onblack(path):
    src = Image.open(path).convert("RGBA")
    bg = Image.new("RGBA", src.size, (0, 0, 0, 255))
    return Image.alpha_composite(bg, src).convert("RGB")
# per character: (centre x px, feet/hem y px, metres per px) measured on the black-background reference (1536x1024)
CH = {"bride": (520, 975, 1.5 / 867.0), "groom": (1037, 980, 1.5 / 885.0)}
for name, (cx, feet, K) in CH.items():
    fp = os.path.join(RDIR, "%s_%s_front.png" % (TAG, name))
    if os.path.exists(fp):
        box = (int(cx - 0.4747 / K), int(feet - 1.55 / K), int(cx + 0.4747 / K), int(feet + 0.03 / K))
        ref = im.crop(box).resize((540, 948), Image.LANCZOS)
        ren = onblack(fp).resize((540, 948))
        ov = Image.blend(ref, ren, 0.5)
        out = Image.new("RGB", (540 * 3 + 20, 948), (255, 255, 255))
        out.paste(ref, (0, 0)); out.paste(ren, (550, 0)); out.paste(ov, (1100, 0))
        d = ImageDraw.Draw(out)
        for z in (0.0, 0.5, 0.9, 1.0, 1.1, 1.2, 1.5):
            y = int(948 - (z + 0.03) / 1.58 * 948)
            d.line([(0, y), (out.width, y)], fill=(255, 0, 0) if z in (0.9, 1.5) else (0, 150, 255), width=1)
            d.text((2, y - 12), "z%.1f" % z, fill=(255, 0, 0))
        out.save(os.path.join(RDIR, "%s_%s_compare.png" % (TAG, name)))
    # face close-up (render face view: ortho 0.62 m centred z 1.12)
    fz0, fz1 = 1.12 - 0.31, 1.12 + 0.31
    fbox = (int(cx - 0.31 / K), int(feet - fz1 / K), int(cx + 0.31 / K), int(feet - fz0 / K))
    fref = im.crop(fbox).resize((700, 700), Image.LANCZOS)
    fp = os.path.join(RDIR, "%s_%s_face.png" % (TAG, name))
    if os.path.exists(fp):
        fren = onblack(fp).resize((700, 700))
        o2 = Image.new("RGB", (2120, 700), (255, 255, 255))
        o2.paste(fref, (0, 0)); o2.paste(fren, (710, 0)); o2.paste(Image.blend(fref, fren, 0.5), (1420, 0))
        o2.save(os.path.join(RDIR, "%s_%s_facecmp.png" % (TAG, name)))
print("ok")
