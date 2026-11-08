"""Generate the repetitive SVGs: plants, trees, parallax backgrounds and ground tiles.

    ArtSource/.venv/bin/python ArtSource/generate_procedural.py
    ArtSource/.venv/bin/python ArtSource/build_art.py

Writes plain SVG files into ArtSource/svg/{Plants,Background,Tiles}. Change a seed
or a number here and re-run to get a new variation. Background layers and tiles are
seamless: their left and right edges (and top/bottom for tiles) line up.
Units: 128 px = 1 tile = 1 Unity unit.
"""

import math
import pathlib
import random

SVG = pathlib.Path(__file__).resolve().parent / "svg"

INK = "#2C2C2A"
PAPER = "#FFFFFF"
RED = "#A32D2D"
GREEN = "#97C459"
GREEN_LIGHT = "#B5DA80"
GREEN_SHADE = "#86B34A"
GREEN_DARK = "#5F8A2E"
LEAF = "#A6CF6A"
LEAF_VEIN = "#7FAE4E"
PINE = "#5E8A33"
PINE_DARK = "#476B24"
BARK = "#8A6A4A"
BARK_DARK = "#6B4F35"

TILE = 128


def write(folder, name, w, h, body, comment):
    path = SVG / folder / f"{name}.svg"
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(
        f'<svg xmlns="http://www.w3.org/2000/svg" width="{w:.0f}" height="{h:.0f}" viewBox="0 0 {w:.0f} {h:.0f}">\n'
        f"  <!-- {comment} -->\n{body}\n</svg>\n"
    )


def poly(points):
    return " ".join(f"{x:.1f},{y:.1f}" for x, y in points)


def path_d(points, close=False):
    d = "M" + " L".join(f"{x:.1f},{y:.1f}" for x, y in points)
    return d + (" Z" if close else "")


# --------------------------------------------------------------------------- plants

def leaf(x, y, angle, length, width, fill=LEAF, stroke=GREEN_DARK):
    return (
        f'<g transform="translate({x:.1f},{y:.1f}) rotate({angle:.1f})">'
        f'<path d="M0,0 Q{length * 0.45:.1f},{-width:.1f} {length:.1f},0 Q{length * 0.45:.1f},{width:.1f} 0,0 Z" '
        f'fill="{fill}" stroke="{stroke}" stroke-width="2.5" stroke-linejoin="round"/>'
        f'<path d="M3,0 L{length * 0.8:.1f},0" stroke="{LEAF_VEIN}" stroke-width="1.6" stroke-linecap="round"/></g>'
    )


def bamboo(name, height, seed):
    rnd = random.Random(seed)
    w, cx, sw = 224, 112, 22
    top = 14
    left, right = cx - sw / 2, cx + sw / 2

    nodes = []
    y = height - rnd.uniform(70, 100)
    while y > top + 50:
        nodes.append(y)
        y -= rnd.uniform(84, 112)

    out = []
    # leaf sprigs behind the stalk
    leaf_nodes = [n for i, n in enumerate(nodes) if i >= 1 and rnd.random() < 0.55]
    sprigs = []
    for n in leaf_nodes:
        side = rnd.choice((-1, 1))
        tx, ty = cx + side * (sw / 2 + 22), n - 20
        sprigs.append(f'<path d="M{cx + side * sw / 2:.1f},{n:.1f} L{tx:.1f},{ty:.1f}" stroke="{GREEN_DARK}" '
                      f'stroke-width="3" stroke-linecap="round"/>')
        for a in (-32, 4, 36):
            ang = a + rnd.uniform(-8, 8)
            if side < 0:
                ang = 180 - ang
            sprigs.append(leaf(tx, ty, ang, rnd.uniform(50, 66), 9))
    out += sprigs

    # stalk
    out.append(
        f'<path d="M{left},{height + 4} L{left},{top + sw / 2} A{sw / 2},{sw / 2} 0 0 1 {right},{top + sw / 2} '
        f'L{right},{height + 4} Z" fill="{GREEN}" stroke="{GREEN_DARK}" stroke-width="3.5"/>'
    )
    out.append(f'<rect x="{cx + 2}" y="{top + 8}" width="{sw / 2 - 4}" height="{height - top}" fill="{GREEN_SHADE}"/>')
    out.append(f'<path d="M{left + 5},{top + 16} L{left + 5},{height + 4}" stroke="{GREEN_LIGHT}" stroke-width="3"/>')
    for n in nodes:
        out.append(f'<rect x="{left - 2.5}" y="{n - 3.5:.1f}" width="{sw + 5}" height="7" rx="3.5" '
                   f'fill="{LEAF_VEIN}" stroke="{GREEN_DARK}" stroke-width="2.5"/>')

    # crown
    for a in (-150, -118, -62, -30):
        out.append(leaf(cx, top + 10, a + rnd.uniform(-6, 6), rnd.uniform(46, 58), 8))

    write("Plants", name, w, height, "  " + "\n  ".join(out),
          f"Bamboo stalk, {height / TILE:.1f} tiles tall. Pivot: bottom centre. Bamboo forest (levels 1-4).")


def pine(name, snow):
    w, h = 192, 320
    cx = 96
    tiers = [(132, 20, 276), (72, 36, 196), (16, 52, 124)]  # apex y, half-base inset, base y
    out = [f'<rect x="{cx - 9}" y="262" width="18" height="56" fill="{BARK}" stroke="{INK}" stroke-width="4.5"/>']
    for apex, inset, base in tiers:
        l, r = inset, w - inset
        out.append(f'<path d="M{cx},{apex} L{l},{base} L{r},{base} Z" fill="{PINE}" stroke="{INK}" '
                   f'stroke-width="5" stroke-linejoin="round"/>')
        out.append(f'<path d="M{cx},{apex + 4} L{cx},{base - 3} L{r - 6},{base - 3} Z" fill="{PINE_DARK}"/>')
        out.append(f'<path d="M{cx},{apex} L{l},{base} L{r},{base} Z" fill="none" stroke="{INK}" '
                   f'stroke-width="5" stroke-linejoin="round"/>')
        if snow:
            # a cap on each tier, following the tier's edges
            t = 0.42 if apex == 16 else 0.0
            if t:
                lx, rx_ = cx - (cx - l) * t, cx + (r - cx) * t
                yy = apex + (base - apex) * t
                cap = [(cx, apex), (rx_, yy), (rx_ - 12, yy - 6), (cx + 8, yy + 2), (cx - 6, yy - 8),
                       (lx + 10, yy - 2), (lx, yy)]
                out.append(f'<path d="{path_d(cap, True)}" fill="{PAPER}" stroke="{INK}" stroke-width="4" '
                           f'stroke-linejoin="round"/>')
            for sx in (l + 4, r - 4):
                dx = 14 if sx < cx else -14
                out.append(f'<path d="M{sx},{base - 1} Q{sx + dx * 0.4},{base - 12} {sx + dx * 1.6},{base - 10} '
                           f'Q{sx + dx * 2.2},{base - 4} {sx + dx * 2.4},{base - 1} Z" fill="{PAPER}" '
                           f'stroke="{INK}" stroke-width="3.5" stroke-linejoin="round"/>')
    write("Plants", name, w, h, "  " + "\n  ".join(out),
          ("Snowy pine" if snow else "Pine") + ", 1.5 x 2.5 tiles. Pivot: bottom centre. Mountain (levels 9-12).")


def willow():
    rnd = random.Random(7)
    w, h = 384, 448
    out = []
    # hanging strands first, so the canopy covers their tops
    for i in range(22):
        x0 = 56 + i * (272 / 21) + rnd.uniform(-4, 4)
        y0 = 150 + 60 * math.sin(math.pi * (i / 21)) * 0.6
        length = rnd.uniform(150, 240)
        sway = rnd.uniform(-14, 14)
        col = "#6F9A3B" if i % 2 else "#86B34A"
        out.append(f'<path d="M{x0:.1f},{y0:.1f} C{x0 + sway:.1f},{y0 + length * 0.4:.1f} {x0 - sway:.1f},'
                   f'{y0 + length * 0.7:.1f} {x0 + sway * 0.5:.1f},{y0 + length:.1f}" fill="none" stroke="{col}" '
                   f'stroke-width="4.5" stroke-linecap="round"/>')
        for k in range(2, int(length / 22)):
            ly = y0 + k * 22
            side = 1 if k % 2 else -1
            out.append(f'<ellipse cx="{x0 + side * 4:.1f}" cy="{ly:.1f}" rx="2.6" ry="6.5" fill="{col}" '
                       f'transform="rotate({-side * 28} {x0 + side * 4:.1f} {ly:.1f})"/>')
    # trunk
    out.append(f'<path d="M176,{h} C182,380 168,300 182,220 L204,220 C212,300 206,380 214,{h} Z" '
               f'fill="{BARK}" stroke="{INK}" stroke-width="5" stroke-linejoin="round"/>')
    out.append(f'<path d="M196,{h - 10} C194,380 190,320 196,240" fill="none" stroke="{BARK_DARK}" stroke-width="3"/>')
    # canopy: outline pass, then fill pass, so overlapping circles read as one shape
    blobs = [(110, 150, 70), (170, 110, 80), (240, 112, 78), (296, 156, 66), (196, 170, 84)]
    for x, y, r in blobs:
        out.append(f'<circle cx="{x}" cy="{y}" r="{r}" fill="{INK}" stroke="{INK}" stroke-width="10"/>')
    for x, y, r in blobs:
        out.append(f'<circle cx="{x}" cy="{y}" r="{r}" fill="#9CC468"/>')
    for x, y, r in blobs[:4]:
        out.append(f'<circle cx="{x - r * 0.2:.0f}" cy="{y - r * 0.25:.0f}" r="{r * 0.55:.0f}" fill="#AED47A"/>')
    write("Plants", "willow", w, h, "  " + "\n  ".join(out),
          "Willow, 3 x 3.5 tiles. Pivot: bottom centre. River (levels 5-8).")


def bush():
    w, h = 192, 112
    blobs = [(54, 72, 38), (100, 54, 50), (148, 74, 36)]
    out = [f'<circle cx="{x}" cy="{y}" r="{r}" fill="{INK}" stroke="{INK}" stroke-width="9"/>' for x, y, r in blobs]
    out += [f'<circle cx="{x}" cy="{y}" r="{r}" fill="#8DBF5A"/>' for x, y, r in blobs]
    out += [f'<circle cx="{x - 8}" cy="{y - 10}" r="{r * 0.55:.0f}" fill="#A6CF6A"/>' for x, y, r in blobs]
    out.append(f'<rect x="0" y="{h - 4}" width="{w}" height="10" fill="none"/>')
    body = '  <clipPath id="ground"><rect width="192" height="108"/></clipPath>\n  <g clip-path="url(#ground)">' \
           + "".join(out) + "</g>\n" + f'  <path d="M14,107 L178,107" stroke="{INK}" stroke-width="4.5" stroke-linecap="round"/>'
    write("Plants", "bush", w, h, body, "Bush, 1.5 tiles wide. Pivot: bottom centre.")


def grass():
    w, h = 96, 48
    blades = [(18, 44, 10, 18), (30, 44, 22, 4), (44, 44, 46, 0), (56, 44, 62, 12), (70, 44, 82, 14), (80, 44, 90, 26)]
    out = []
    for bx, by, tx, ty in blades:
        out.append(f'<path d="M{bx - 6},{by} L{tx},{ty} L{bx + 6},{by} Z" fill="{GREEN}" stroke="{GREEN_DARK}" '
                   f'stroke-width="3" stroke-linejoin="round"/>')
    write("Plants", "grass_tuft", w, h, "  " + "\n  ".join(out), "Grass tuft. Pivot: bottom centre.")


def reeds():
    w, h = 128, 224
    out = []
    for x0, x1, top, head in ((40, 30, 40, True), (64, 70, 20, True), (88, 100, 56, True), (54, 34, 90, False),
                              (78, 104, 100, False)):
        out.append(f'<path d="M{x0},{h} Q{x0},{(h + top) / 2} {x1},{top}" fill="none" stroke="{LEAF_VEIN}" '
                   f'stroke-width="4" stroke-linecap="round"/>')
        if head:
            out.append(f'<rect x="{x1 - 7}" y="{top}" width="14" height="40" rx="7" fill="#8A6A44" '
                       f'stroke="{INK}" stroke-width="3.5" transform="rotate({(x1 - x0) * 0.4:.1f} {x1} {top + 20})"/>')
            out.append(f'<path d="M{x1},{top} L{x1},{top - 12}" stroke="{INK}" stroke-width="3" stroke-linecap="round"/>')
        else:
            out.append(f'<path d="M{x0 - 5},{h} Q{x0},{(h + top) / 2} {x1},{top} Q{x0 + 6},{(h + top) / 2} '
                       f'{x0 + 5},{h} Z" fill="{GREEN}" stroke="{GREEN_DARK}" stroke-width="2.5" stroke-linejoin="round"/>')
    write("Plants", "reeds", w, h, "  " + "\n  ".join(out), "Reeds and cattails for river banks. Pivot: bottom centre.")


# --------------------------------------------------------------------------- backgrounds

def wrap_dist(x, c, period):
    d = abs(x - c) % period
    return min(d, period - d)


def ridge(width, peaks, base, step=8, k=0.06):
    """Height profile: rounded peaks merged with a smooth max, periodic over width."""
    pts = []
    for i in range(0, width + step, step):
        x = min(i, width)
        acc = 0.0
        for c, hgt, hw in peaks:
            d = wrap_dist(x, c, width)
            v = hgt * (0.5 + 0.5 * math.cos(math.pi * d / hw)) if d < hw else 0.0
            acc += math.exp(k * v)
        pts.append((x, base - math.log(acc) / k))
    return pts


def mountains(name, height, colour, seed, count, hmin, hmax, snow=False):
    rnd = random.Random(seed)
    width = 2048
    peaks = []
    for i in range(count):
        c = (i + rnd.uniform(0.2, 0.8)) * width / count
        peaks.append((c, rnd.uniform(hmin, hmax), rnd.uniform(220, 360)))
    pts = ridge(width, peaks, height - 8)
    shape = [(0, height)] + pts + [(width, height)]
    out = [f'<path d="{path_d(shape, True)}" fill="{colour}"/>']
    if snow:
        for c, hgt, hw in peaks:
            if hgt < hmin + (hmax - hmin) * 0.45:
                continue
            for c2 in (c - width, c, c + width):
                cap_w = hw * 0.22
                top = [(x, y) for x, y in pts if abs(x - c2) <= cap_w]
                if len(top) < 3:
                    continue
                zig = []
                n = 8
                for j in range(n + 1):
                    x = top[-1][0] - (top[-1][0] - top[0][0]) * j / n
                    ys = [y for xx, y in top if abs(xx - x) <= 8] or [top[0][1]]
                    zig.append((x, max(ys) + (16 if j % 2 else 4)))
                out.append(f'<path d="{path_d(top + zig, True)}" fill="{PAPER}" stroke="{INK}" stroke-width="4" '
                           f'stroke-linejoin="round"/>')
    write("Background", name, width, height, "  " + "\n  ".join(out),
          f"Parallax mountain layer, 16 x {height / TILE:.1f} tiles, tiles horizontally. Pivot: bottom centre. "
          f"Use SpriteRenderer Draw Mode = Tiled.")


def mountain_peak():
    w, h = 1024, 512
    body = [
        f'<path d="M0,{h} C180,420 330,240 452,96 C480,64 544,64 572,96 C694,240 844,420 1024,{h} Z" fill="#C9C0B0"/>',
        f'<path d="M452,96 C480,64 544,64 572,96 L626,160 L596,150 L572,176 L540,150 L512,184 L484,150 L452,172 '
        f'L428,152 L398,162 Z" fill="{PAPER}" stroke="{INK}" stroke-width="5" stroke-linejoin="round"/>',
    ]
    write("Background", "mountain_peak", w, h, "  " + "\n  ".join(body),
          "Single snowy peak, a backdrop landmark (8 x 4 tiles). Pivot: bottom centre.")


def bamboo_far():
    rnd = random.Random(11)
    width, height = 2048, 768
    out = []
    for i in range(26):
        x = (i + rnd.uniform(0.1, 0.9)) * width / 26
        sw = rnd.uniform(9, 15)
        top = rnd.uniform(-40, 160)
        col, node = ("#CFE3AE", "#B4CF8C") if rnd.random() < 0.5 else ("#C3DBA0", "#A9C67F")
        for xx in (x - width, x, x + width):
            out.append(f'<rect x="{xx - sw / 2:.1f}" y="{top:.1f}" width="{sw:.1f}" height="{height - top + 4:.1f}" '
                       f'rx="{sw / 2:.1f}" fill="{col}"/>')
            y = top + rnd.uniform(60, 120)
            while y < height:
                out.append(f'<rect x="{xx - sw / 2 - 2:.1f}" y="{y:.1f}" width="{sw + 4:.1f}" height="5" rx="2.5" '
                           f'fill="{node}"/>')
                y += rnd.uniform(80, 120)
            for _ in range(rnd.randint(1, 3)):
                ly = rnd.uniform(top + 40, height * 0.6)
                side = rnd.choice((-1, 1))
                for a in (-30, 10, 40):
                    ang = a if side > 0 else 180 - a
                    out.append(leaf(xx + side * sw / 2, ly, ang, rnd.uniform(36, 50), 6, fill="#C9E0A6",
                                    stroke="#B4CF8C"))
    write("Background", "bamboo_far", width, height, "  " + "\n  ".join(out),
          "Pale far bamboo, 16 x 6 tiles, tiles horizontally. Pivot: bottom centre. Draw Mode = Tiled.")


def sun():
    write("Background", "sun", 384, 384,
          f'  <circle cx="192" cy="192" r="184" fill="{RED}" fill-opacity="0.86"/>',
          "Seal-red sun, 3 tiles across. Pivot: centre. Sort it between mountain layers.")


def clouds():
    a = (f'  <g fill="none" stroke="#B3AA9A" stroke-width="5" stroke-linecap="round">'
         f'<path d="M6,46 C14,26 38,22 50,36 C58,16 88,14 98,34 C108,26 124,28 130,40"/>'
         f'<path d="M40,62 C52,52 70,52 80,60 C88,54 100,54 106,62"/></g>')
    b = (f'  <g fill="none" stroke="#B3AA9A" stroke-width="5" stroke-linecap="round">'
         f'<path d="M6,40 C16,22 40,20 52,32 C62,22 80,24 86,36"/>'
         f'<path d="M30,56 C40,48 56,48 64,56"/></g>')
    write("Background", "cloud_a", 136, 72, a, "Line cloud. Pivot: centre. Drift slowly in code.")
    write("Background", "cloud_b", 92, 64, b, "Small line cloud. Pivot: centre.")


# --------------------------------------------------------------------------- tiles

def jag(seed, n=16, amp=3.2):
    rnd = random.Random(seed)
    offs = [rnd.uniform(-amp, amp) for _ in range(n)]
    return offs + [offs[0]]  # periodic: last == first


def edge_y(offs, x, inset):
    """Jagged edge position at x (0..128), periodic with the tile."""
    seg = TILE / (len(offs) - 1)
    i = int((x % TILE) // seg)
    t = ((x % TILE) - i * seg) / seg
    return inset + offs[i] + (offs[i + 1] - offs[i]) * t


def ground_tile(set_name, flags, palette, seed):
    """flags: subset of 'TBLR' = which sides have the torn ink edge."""
    fill, band, mark = palette["fill"], palette["band"], palette["mark"]
    inset, pad, seg = 10, 12, 8
    offs_h, offs_v = jag(seed), jag(seed + 1)

    def h_edge(y_inset, sign, x0, x1):
        xs = sorted({x0, x1, *[x for x in range(-TILE, 2 * TILE + 1, seg) if x0 < x < x1]})
        return [(x, edge_y(offs_h, x, 0) * sign + y_inset) for x in xs]

    def v_edge(x_inset, sign, y0, y1):
        ys = sorted({y0, y1, *[y for y in range(-TILE, 2 * TILE + 1, seg) if y0 < y < y1]})
        return [(edge_y(offs_v, y, 0) * sign + x_inset, y) for y in ys]

    x0 = inset if "L" in flags else -pad
    x1 = TILE - inset if "R" in flags else TILE + pad
    y0 = inset if "T" in flags else -pad
    y1 = TILE - inset if "B" in flags else TILE + pad

    top = h_edge(y0, 1, x0, x1) if "T" in flags else [(x0, y0), (x1, y0)]
    right = v_edge(x1, -1, y0, y1) if "R" in flags else [(x1, y0), (x1, y1)]
    bottom = list(reversed(h_edge(y1, -1, x0, x1))) if "B" in flags else [(x1, y1), (x0, y1)]
    left = list(reversed(v_edge(x0, 1, y0, y1))) if "L" in flags else [(x0, y1), (x0, y0)]
    region = top + right[1:] + bottom[1:] + left[1:-1]

    out = [f'<clipPath id="r"><path d="{path_d(region, True)}"/></clipPath>',
           f'<path d="{path_d(region, True)}" fill="{fill}"/>',
           '<g clip-path="url(#r)">']
    if "T" in flags:
        out.append(f'<path d="{path_d([(x, y + 10) for x, y in top])}" fill="none" stroke="{band}" stroke-width="10"/>')
    # paper fibres (kept off the edges so the pattern stays seamless)
    rnd = random.Random(1234)
    for _ in range(7):
        fx, fy = rnd.uniform(18, 96), rnd.uniform(30, 112)
        out.append(f'<path d="M{fx:.1f},{fy:.1f} l{rnd.uniform(8, 16):.1f},{rnd.uniform(-1.5, 1.5):.1f}" '
                   f'stroke="{mark}" stroke-width="2.5" stroke-linecap="round"/>')
    if palette.get("snow") and "T" in flags:
        wave = [(x, y + 20 + 4 * math.sin(x / TILE * 2 * math.pi * 2)) for x, y in top]
        cap = top + list(reversed(wave))
        out.append(f'<path d="{path_d(cap, True)}" fill="{PAPER}"/>')
        out.append(f'<path d="{path_d(wave)}" fill="none" stroke="#D9D4CB" stroke-width="3"/>')
    out.append("</g>")
    for side, pts in (("T", top), ("R", right), ("B", bottom), ("L", left)):
        if side in flags:
            out.append(f'<path d="{path_d(pts)}" fill="none" stroke="{INK}" stroke-width="4.5" '
                       f'stroke-linejoin="round" stroke-linecap="round"/>')
    return "  " + "\n  ".join(out)


TILE_NAMES = {
    "top_left": "TL", "top": "T", "top_right": "TR",
    "left": "L", "fill": "", "right": "R",
    "bottom_left": "BL", "bottom": "B", "bottom_right": "BR",
    "single": "TBLR",
}

PALETTES = {
    "sand": {"fill": "#E2D2AE", "band": "#D3BF96", "mark": "#D4C29C", "seed": 3,
             "note": "Paper-sand ground (bamboo forest, river banks)"},
    "stone": {"fill": "#ABA497", "band": "#9A9386", "mark": "#9C9589", "seed": 5,
              "note": "Stone ground (stone gates, mountain)"},
    "snow": {"fill": "#ABA497", "band": "#9A9386", "mark": "#9C9589", "seed": 5, "snow": True,
             "note": "Snow over stone (mountain)"},
}


def tiles():
    for set_name, pal in PALETTES.items():
        for name, flags in TILE_NAMES.items():
            if pal.get("snow") and "T" not in flags:
                continue  # snow only changes the top row; use stone for the rest
            write("Tiles", f"{set_name}_{name}", TILE, TILE, ground_tile(set_name, flags, pal, pal["seed"]),
                  f"{pal['note']}: {name.replace('_', ' ')}. 1 tile. Seamless with its set.")
    bridge()


def bridge():
    """One-way bamboo bridge: left / mid / right, pole along the top of the cell."""
    y0, y1 = 8, 34
    for name, x0, x1 in (("bridge_left", 14, TILE + 12), ("bridge_mid", -12, TILE + 12), ("bridge_right", -12, TILE - 14)):
        out = [f'<rect x="{x0}" y="{y0}" width="{x1 - x0}" height="{y1 - y0}" fill="{GREEN}"/>',
               f'<rect x="{x0}" y="{y0 + 15}" width="{x1 - x0}" height="{y1 - y0 - 15}" fill="{GREEN_SHADE}"/>',
               f'<path d="M{x0},{y0 + 6} L{x1},{y0 + 6}" stroke="{GREEN_LIGHT}" stroke-width="3"/>',
               f'<path d="M{x0},{y0} L{x1},{y0} M{x0},{y1} L{x1},{y1}" stroke="{INK}" stroke-width="4.5" '
               f'stroke-linecap="round"/>']
        if name == "bridge_mid":
            out.append(f'<rect x="61" y="{y0 - 2}" width="7" height="{y1 - y0 + 4}" rx="3" fill="{LEAF_VEIN}" '
                       f'stroke="{INK}" stroke-width="3"/>')
        for xe in ([x0] if name == "bridge_left" else []) + ([x1] if name == "bridge_right" else []):
            out.append(f'<ellipse cx="{xe}" cy="{(y0 + y1) / 2}" rx="7" ry="{(y1 - y0) / 2}" fill="#E4EFC9" '
                       f'stroke="{INK}" stroke-width="4"/>')
            out.append(f'<ellipse cx="{xe}" cy="{(y0 + y1) / 2}" rx="2.5" ry="6" fill="none" stroke="{GREEN_DARK}" '
                       f'stroke-width="2"/>')
            lash = xe + (14 if name == "bridge_left" else -14)
            out.append(f'<path d="M{lash - 4},{y0 - 2} L{lash + 4},{y1 + 2} M{lash + 4},{y0 - 2} L{lash - 4},{y1 + 2}" '
                       f'stroke="#8A6A44" stroke-width="3.5" stroke-linecap="round"/>')
        write("Tiles", name, TILE, TILE, "  " + "\n  ".join(out),
              "One-way bamboo bridge (use a PlatformEffector2D). Pole sits in the top quarter of the cell.")



# --------------------------------------------------------------------------- ninja animation frames

NINJA_TAILS = ('<path d="M46,43 C32,36 18,30 4,30 C10,36 12,40 12,45 C22,45 32,47 46,52 Z" fill="#A32D2D" stroke-width="4"/>'
               '<path d="M46,50 C36,55 26,63 16,76 C22,76 26,78 30,82 C34,70 40,62 46,57 Z" fill="#A32D2D" stroke-width="4"/>')


def ninja_frame(name, comment, lean=0, head_dy=0, tail_rot=0, arm_l=0, arm_r=0, feet=((56, 212), (90, 212)),
                hurt=False):
    eyes = ('<path d="M66,74 L84,80 L66,86 M120,74 L102,80 L120,86" fill="none" stroke="#FFFFFF" stroke-width="4"/>'
            if hurt else
            '<path d="M63,73 L85,80 L65,87 Z" fill="#FFFFFF" stroke="none"/>'
            '<path d="M99,80 L121,73 L119,87 Z" fill="#FFFFFF" stroke="none"/>')
    head = ('<rect x="44" y="26" width="80" height="86" rx="16" fill="#FFFFFF" stroke="none"/>'
            '<g clip-path="url(#head)">'
            '<path d="M44,26 L76,26 L44,58 Z" fill="#EDE5D6" stroke="none"/>'
            '<path d="M76,26 L44,58" fill="none" stroke-width="2.5"/>'
            '<rect x="40" y="40" width="88" height="15" fill="#A32D2D" stroke-width="3.5"/>'
            '<rect x="40" y="64" width="88" height="28" fill="#2C2C2A" stroke="none"/></g>'
            + eyes +
            '<rect x="44" y="26" width="80" height="86" rx="16" fill="none" stroke-width="5"/>'
            '<circle cx="45" cy="48" r="6.5" fill="#A32D2D" stroke-width="4"/>')
    (fl, fr) = feet
    body = (
        f'<g transform="translate(0,{head_dy}) rotate({tail_rot} 45 48)">{NINJA_TAILS}</g>'
        f'<rect x="{fl[0]}" y="{fl[1]}" width="22" height="18" rx="6" fill="#2C2C2A" stroke="none"/>'
        f'<rect x="{fr[0]}" y="{fr[1]}" width="22" height="18" rx="6" fill="#2C2C2A" stroke="none"/>'
        f'<path d="M58,120 L34,168 L54,160 Z" transform="rotate({arm_l} 58 120)" fill="#EDE5D6" stroke-width="4.5"/>'
        '<path d="M56,110 L112,110 L124,222 L44,222 Z" fill="#FFFFFF" stroke="none"/>'
        '<path d="M56,110 L84,110 L84,222 L44,222 Z" fill="#EDE5D6" stroke="none"/>'
        '<path d="M66,110 L84,132 L102,110 Z" fill="#2C2C2A" stroke="none"/>'
        '<path d="M84,132 L84,222" fill="none" stroke-width="2.5"/>'
        '<path d="M64,110 L84,134 L104,110" fill="none" stroke-width="3.5"/>'
        '<path d="M56,110 L112,110 L124,222 L44,222 Z" fill="none" stroke-width="5"/>'
        f'<path d="M110,120 L134,168 L114,160 Z" transform="rotate({arm_r} 110 120)" fill="#FFFFFF" stroke-width="4.5"/>'
        f'<g transform="translate(0,{head_dy})">{head}</g>'
    )
    svg = ('  <defs><clipPath id="head"><rect x="44" y="26" width="80" height="86" rx="16"/></clipPath></defs>\n'
           f'  <g stroke="{INK}" stroke-linejoin="round" stroke-linecap="round" transform="rotate({lean} 84 230)">'
           f'{body}</g>')
    write("Characters", name, 168, 232, svg, comment + " Same canvas and pivot as kami_ninja.")


def ninja_frames():
    ninja_frame("kami_ninja_run_1", "Ninja run, frame 1 of 2.", lean=5, tail_rot=12, arm_l=35, arm_r=-35,
                feet=((42, 206), (102, 213)))
    ninja_frame("kami_ninja_run_2", "Ninja run, frame 2 of 2.", lean=5, head_dy=3, tail_rot=2, arm_l=-20, arm_r=20,
                feet=((62, 213), (84, 205)))
    ninja_frame("kami_ninja_jump", "Ninja jump (rising).", head_dy=-2, tail_rot=-28, arm_l=60, arm_r=-60,
                feet=((58, 206), (88, 206)))
    ninja_frame("kami_ninja_fall", "Ninja fall.", tail_rot=40, arm_l=118, arm_r=-118, feet=((50, 213), (96, 213)))
    ninja_frame("kami_ninja_throw", "Ninja throwing a paper star (spawn the star at the right hand).", lean=3,
                tail_rot=8, arm_l=30, arm_r=-72, feet=((52, 212), (94, 212)))
    ninja_frame("kami_ninja_hurt", "Ninja hurt / ink-stained (no folding for 3 s).", lean=-6, head_dy=2,
                tail_rot=24, arm_l=70, arm_r=-70, feet=((54, 212), (92, 212)), hurt=True)


# --------------------------------------------------------------------------- more scenery

def maple():
    rnd = random.Random(31)
    w, h = 352, 400
    out = [f'<path d="M160,{h} C166,340 150,300 150,250 C130,226 104,212 84,206 L92,196 C112,200 134,210 152,226 '
           f'C154,200 160,180 172,164 L184,168 C176,186 172,206 174,230 C196,206 222,192 252,186 L256,198 '
           f'C228,206 204,222 186,246 C182,300 194,340 198,{h} Z" fill="{BARK}" stroke="{INK}" stroke-width="5" '
           f'stroke-linejoin="round"/>']
    blobs = [(86, 176, 58), (146, 118, 72), (222, 104, 74), (282, 160, 60), (184, 172, 70), (120, 196, 46),
             (252, 206, 44)]
    for x, y, r in blobs:
        out.append(f'<circle cx="{x}" cy="{y}" r="{r}" fill="{INK}" stroke="{INK}" stroke-width="10"/>')
    for x, y, r in blobs:
        out.append(f'<circle cx="{x}" cy="{y}" r="{r}" fill="{RED}"/>')
    for x, y, r in blobs[:5]:
        out.append(f'<circle cx="{x - r * 0.18:.0f}" cy="{y - r * 0.22:.0f}" r="{r * 0.6:.0f}" fill="#C0473A"/>')
    leaf_d = ("M0,-9 L2.5,-3 L8,-5 L6,0 L9,2 L4,3 L5,8 L0,5 L-5,8 L-4,3 L-9,2 L-6,0 L-8,-5 L-2.5,-3 Z")
    for _ in range(34):
        x, y, r = rnd.choice(blobs[:5])
        a = rnd.uniform(0, 2 * math.pi)
        d = rnd.uniform(0, r * 0.75)
        out.append(f'<path d="{leaf_d}" transform="translate({x + math.cos(a) * d:.1f},{y + math.sin(a) * d:.1f}) '
                   f'rotate({rnd.uniform(-40, 40):.0f}) scale({rnd.uniform(0.9, 1.4):.2f})" '
                   f'fill="{rnd.choice(("#D9663F", "#E08A4F", "#B83A2E"))}"/>')
    write("Plants", "maple", w, h, "  " + "\n  ".join(out),
          "Red maple (momiji), 2.75 x 3 tiles. Pivot: bottom centre. River and mountain. Pair with FX/maple_leaf.")


def arched_bridge():
    w, h = 512, 208

    def quad(p0, p1, p2, t):
        return ((1 - t) ** 2 * p0[0] + 2 * t * (1 - t) * p1[0] + t * t * p2[0],
                (1 - t) ** 2 * p0[1] + 2 * t * (1 - t) * p1[1] + t * t * p2[1])

    deck_top = [quad((0, 176), (256, 44), (512, 176), i / 32) for i in range(33)]
    deck_bot = [quad((0, 200), (256, 72), (512, 200), i / 32) for i in range(33)]
    rail = [quad((14, 126), (256, -4), (498, 126), i / 32) for i in range(33)]
    out = [f'<path d="{path_d(deck_top + list(reversed(deck_bot)), True)}" fill="{BARK}" stroke="{INK}" '
           f'stroke-width="5" stroke-linejoin="round"/>',
           f'<path d="{path_d(deck_top)}" fill="none" stroke="#B08A5C" stroke-width="5" transform="translate(0,6)"/>']
    for t in (0.03, 0.2, 0.36, 0.5, 0.64, 0.8, 0.97):
        x, y = quad((0, 176), (256, 44), (512, 176), t)
        rx, ry = quad((14, 126), (256, -4), (498, 126), t)
        out.append(f'<path d="M{x:.1f},{y + 2:.1f} L{x:.1f},{ry:.1f}" stroke="{INK}" stroke-width="15" stroke-linecap="round"/>')
        out.append(f'<path d="M{x:.1f},{y + 2:.1f} L{x:.1f},{ry:.1f}" stroke="{RED}" stroke-width="8"/>')
    out.append(f'<path d="{path_d(rail)}" fill="none" stroke="{INK}" stroke-width="16" stroke-linecap="round"/>')
    out.append(f'<path d="{path_d(rail)}" fill="none" stroke="{RED}" stroke-width="9" stroke-linecap="round"/>')
    for t in (0.03, 0.97):
        x, _ = quad((0, 176), (256, 44), (512, 176), t)
        _, ry = quad((14, 126), (256, -4), (498, 126), t)
        out.append(f'<path d="M{x - 9:.1f},{ry - 6:.1f} Q{x:.1f},{ry - 26:.1f} {x + 9:.1f},{ry - 6:.1f} Z" fill="{INK}"/>')
    write("Props", "arched_bridge", w, h, "  " + "\n  ".join(out),
          "Red arched (taiko) bridge, 4 tiles wide. Walkable: put an EdgeCollider2D along the deck. Pivot: bottom centre.")


def cave_layers():
    rnd = random.Random(41)
    width = 2048
    # ceiling with stalactites, hangs from the top of the screen
    h = 384
    pts = [(0, 0)]
    x = 0
    while x < width:
        step = rnd.uniform(40, 90)
        base = 120 + 40 * math.sin(x / width * 2 * math.pi * 3)
        if rnd.random() < 0.45:
            tip = base + rnd.uniform(90, 220)
            pts += [(x, base), (x + step * 0.5, tip), (x + step, base)]
        else:
            pts += [(x, base), (x + step, base + rnd.uniform(-20, 30))]
        x += step
    pts = [(min(px, width), py) for px, py in pts]
    pts[-1] = (width, 120 + 40 * math.sin(0))
    pts.insert(1, (0, 120))
    pts.append((width, 0))
    out = [f'<path d="{path_d(pts, True)}" fill="#3E3B37"/>']
    write("Background", "cave_ceiling", width, h, "  " + "\n  ".join(out),
          "Cave ceiling with stalactites, 16 x 3 tiles, tiles horizontally. Pivot: TOP centre. Ink rain cave (level 10).")
    # back wall
    h = 768
    out = [f'<rect width="{width}" height="{h}" fill="#5B5752"/>']
    for _ in range(40):
        cx, cy = rnd.uniform(0, width), rnd.uniform(0, h)
        rx_, ry_ = rnd.uniform(60, 180), rnd.uniform(30, 80)
        col = rnd.choice(("#625E58", "#55514C", "#67625C"))
        for xx in (cx - width, cx, cx + width):
            out.append(f'<ellipse cx="{xx:.0f}" cy="{cy:.0f}" rx="{rx_:.0f}" ry="{ry_:.0f}" fill="{col}"/>')
    for _ in range(30):
        cx, cy = rnd.uniform(0, width), rnd.uniform(0, h)
        for xx in (cx - width, cx, cx + width):
            out.append(f'<path d="M{xx:.0f},{cy:.0f} l{rnd.uniform(20, 50):.0f},{rnd.uniform(-8, 8):.0f}" '
                       f'stroke="#4A4743" stroke-width="4" stroke-linecap="round"/>')
    write("Background", "cave_wall", width, h, "  " + "\n  ".join(out),
          "Dark cave back wall, 16 x 6 tiles, tiles horizontally. Pivot: bottom centre. Draw Mode = Tiled.")


def ink_sky():
    w, h = 1024, 512
    body = ('  <defs><linearGradient id="g" x1="0" y1="0" x2="0" y2="1">'
            '<stop offset="0" stop-color="#2C2C2A"/><stop offset="0.6" stop-color="#4A4743"/>'
            '<stop offset="1" stop-color="#7A7268"/></linearGradient></defs>\n'
            f'  <rect width="{w}" height="{h}" fill="url(#g)"/>\n'
            '  <circle cx="760" cy="170" r="110" fill="#7E2323" fill-opacity="0.75"/>\n'
            '  <g fill="none" stroke="#5B5A55" stroke-width="10" stroke-linecap="round">'
            '<path d="M60,120 C140,60 240,80 260,140 C276,190 214,206 196,170"/>'
            '<path d="M420,70 C520,20 640,40 660,100"/>'
            '<path d="M120,300 C260,250 380,290 420,330 C450,360 410,390 384,368"/>'
            '<path d="M620,330 C720,280 860,300 960,260"/></g>')
    write("Background", "ink_sky", w, h, body,
          "Finale sky, stained by the spilled ink (level 13). Stretch it behind everything. Pivot: centre.")


def main():
    bamboo("bamboo_tall", 768, 1)
    bamboo("bamboo_mid", 576, 2)
    bamboo("bamboo_short", 384, 3)
    pine("pine", snow=False)
    pine("pine_snow", snow=True)
    willow()
    bush()
    grass()
    reeds()
    mountains("mountains_far", 512, "#DDD5C7", seed=21, count=5, hmin=250, hmax=400)
    mountains("mountains_far_snow", 512, "#DDD5C7", seed=21, count=5, hmin=250, hmax=400, snow=True)
    mountains("mountains_mid", 448, "#C9C0B0", seed=22, count=6, hmin=150, hmax=300)
    mountains("mountains_near", 384, "#B3AA9A", seed=23, count=7, hmin=90, hmax=200)
    mountain_peak()
    bamboo_far()
    sun()
    clouds()
    tiles()
    ninja_frames()
    maple()
    arched_bridge()
    cave_layers()
    ink_sky()
    print("procedural SVGs written to", SVG)


if __name__ == "__main__":
    main()
