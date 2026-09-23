"""Makes the Persistent Work Areas site's surfaces: the site plan (warm plan paper with a faint surveyed grid, by day
and under the lamp), the vellum the sheets are drawn on, and a strip of drafting tape. Procedural (numpy and Pillow,
fixed seeds); no source images and no generative model. Tiles repeat. Run from this folder: python make_textures.py"""
import numpy as np
from PIL import Image, ImageDraw

def wrap_noise(rng, n, cell):
    g = rng.normal(0, 1, (n // cell, n // cell)); g = (g - g.min()) / (g.max() - g.min())
    big = np.tile(g, (3, 3))
    up = np.asarray(Image.fromarray((big * 255).astype(np.uint8)).resize((n * 3, n * 3), Image.BICUBIC), float) / 255
    return up[n:2 * n, n:2 * n] - .5

def save(img, name, **kw):
    img.save(name, **kw); print("made", name)

# ---- the site plan: paper with a surveyed grid, a fine line every 24 px and a firmer one every 96 px
for name, seed, paper, fine, firm in (("plan-light.webp", 5101, (236, 235, 227), (222, 220, 208), (208, 205, 190)),
                                      ("plan-dark.webp", 5102, (23, 34, 31), (30, 43, 40), (37, 53, 49))):
    rng = np.random.default_rng(seed); N = 384
    v = wrap_noise(rng, N, 48) * 5 + rng.normal(0, 1.1, (N, N))
    rgb = np.array(paper, float) + v[..., None]
    y, x = np.mgrid[0:N, 0:N]
    fine_m = ((x % 24 == 0) | (y % 24 == 0)).astype(float)
    firm_m = ((x % 96 == 0) | (y % 96 == 0)).astype(float)
    rgb = rgb * (1 - fine_m[..., None] * .55) + np.array(fine, float) * fine_m[..., None] * .55
    rgb = rgb * (1 - firm_m[..., None] * .7) + np.array(firm, float) * firm_m[..., None] * .7
    save(Image.fromarray(rgb.clip(0, 255).astype(np.uint8)), name, quality=90, method=6)

# ---- vellum: translucent, a soft cloud of fibres; laid over the plan so the grid shows faintly through
for name, seed, tone, alpha in (("vellum-light.png", 5103, (248, 247, 240), 190), ("vellum-dark.png", 5104, (32, 45, 42), 196)):
    rng = np.random.default_rng(seed); N = 256
    cloud = wrap_noise(rng, N, 32) * 10 + wrap_noise(rng, N, 8) * 4
    img = np.zeros((N, N, 4), float)
    img[..., :3] = np.array(tone, float) + cloud[..., None]
    img[..., 3] = alpha + cloud * 1.5
    save(Image.fromarray(img.clip(0, 255).astype(np.uint8), "RGBA"), name, optimize=True)

# ---- drafting tape: a short strip of cream crepe tape with a faint crinkle and torn ends, at an angle set in CSS
rng = np.random.default_rng(5105); W, H, K = 96, 30, 4
img = Image.new("RGBA", (W * K, H * K), (0, 0, 0, 0))
d = ImageDraw.Draw(img)
top = [(0, 0)]; bot = [(0, H * K)]
for i in range(1, 7):
    top.append((0 + rng.uniform(-3, 3) * K, i * H * K / 7))
left = [(rng.uniform(0, 4) * K, i * H * K / 8) for i in range(9)]
right = [(W * K - rng.uniform(0, 4) * K, (8 - i) * H * K / 8) for i in range(9)]
d.polygon(left + right, fill=(236, 226, 196, 214))
img = img.resize((W, H), Image.LANCZOS)
a = np.asarray(img, float)
crinkle = (np.sin(np.arange(W) / 1.3 + rng.uniform(0, 6)) * 3 + rng.normal(0, 2.2, (H, W)))
a[..., :3] = (a[..., :3] + crinkle[..., None]).clip(0, 255)
save(Image.fromarray(a.astype(np.uint8), "RGBA"), "tape.png", optimize=True)
