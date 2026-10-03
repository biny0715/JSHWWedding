# tile TBride renders into one review sheet. usage: tb_sheet.py <TAG> <out.png> view1 view2 ...
import sys, os
from PIL import Image, ImageDraw
RD = r"D:\UnityProjects\JSHWWedding\BlenderSource\renders"
tag, out, views = sys.argv[1], sys.argv[2], sys.argv[3:]
H = 520
ims = []
for v in views:
    p = os.path.join(RD, "%s_%s.png" % (tag, v))
    s = Image.open(p).convert("RGBA"); k = Image.new("RGBA", s.size, (222, 220, 219, 255))
    im = Image.alpha_composite(k, s).convert("RGB")
    ims.append((v, im.resize((int(im.width * H / im.height), H), Image.LANCZOS)))
W = sum(i.width for _, i in ims) + 10 * len(ims)
sheet = Image.new("RGB", (W, H + 22), (255, 255, 255)); d = ImageDraw.Draw(sheet); x = 0
for v, im in ims:
    sheet.paste(im, (x, 22)); d.text((x + 4, 4), v, fill=(0, 0, 0)); x += im.width + 10
sheet.save(os.path.join(RD, out))
print("ok", out)
