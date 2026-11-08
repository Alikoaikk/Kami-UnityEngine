"""Render Kami's SVG art into Unity sprites.

    ArtSource/.venv/bin/python ArtSource/build_art.py

Every ArtSource/svg/<Folder>/<name>.svg becomes
Assets/Art/Sprites/<Folder>/<name>.png at 128 px per tile (1 tile = 1 Unity unit).
Import settings (pixels per unit, pivot, mesh type) are applied in Unity by
Assets/Editor/KamiSpriteImporter.cs, keyed on the folder name.

It also writes ArtSource/preview/<Folder>.png contact sheets for a quick look.
"""

import base64
import pathlib
import re
import sys

import resvg_py

ROOT = pathlib.Path(__file__).resolve().parent
SVG_DIR = ROOT / "svg"
OUT_DIR = ROOT.parent / "Assets" / "Art" / "Sprites"
PREVIEW_DIR = ROOT / "preview"

PARCHMENT = "#F4ECDD"


def svg_size(svg_text):
    w = re.search(r'<svg[^>]*\bwidth="([\d.]+)"', svg_text)
    h = re.search(r'<svg[^>]*\bheight="([\d.]+)"', svg_text)
    return float(w.group(1)), float(h.group(1))


def render(svg_path, png_path):
    png = resvg_py.svg_to_bytes(svg_path=str(svg_path))
    png_path.parent.mkdir(parents=True, exist_ok=True)
    png_path.write_bytes(bytes(png))


def contact_sheet(folder, items, max_width=1600, pad=28, label_h=26):
    """items: list of (name, png_path, w, h). Lays sprites out in rows on parchment."""
    scale = 1.0
    widest = max(w for _, _, w, _ in items)
    if widest + 2 * pad > max_width:
        scale = (max_width - 2 * pad) / widest

    x, y, row_h = pad, pad + 30, 0
    placed = []
    for name, png, w, h in items:
        sw, sh = w * scale, h * scale
        if x + sw + pad > max_width and x > pad:
            x, y = pad, y + row_h + label_h + pad
            row_h = 0
        placed.append((name, png, x, y, sw, sh))
        x += max(sw, 120) + pad
        row_h = max(row_h, sh)
    total_h = y + row_h + label_h + pad

    parts = [
        f'<svg xmlns="http://www.w3.org/2000/svg" width="{max_width}" height="{total_h:.0f}">',
        f'<rect width="100%" height="100%" fill="{PARCHMENT}"/>',
        f'<text x="{pad}" y="{pad + 8}" font-family="Helvetica" font-size="22" font-weight="bold" '
        f'fill="#2C2C2A">{folder}</text>',
    ]
    for name, png, px, py, sw, sh in placed:
        data = base64.b64encode(png.read_bytes()).decode()
        parts.append(f'<rect x="{px}" y="{py}" width="{sw:.1f}" height="{sh:.1f}" fill="none" '
                     f'stroke="#D8CCB4" stroke-dasharray="4 4"/>')
        parts.append(f'<image x="{px}" y="{py}" width="{sw:.1f}" height="{sh:.1f}" '
                     f'href="data:image/png;base64,{data}"/>')
        parts.append(f'<text x="{px}" y="{py + sh + 18:.1f}" font-family="Helvetica" font-size="13" '
                     f'fill="#6B6458">{name}</text>')
    parts.append("</svg>")

    PREVIEW_DIR.mkdir(exist_ok=True)
    out = PREVIEW_DIR / f"{folder}.png"
    out.write_bytes(bytes(resvg_py.svg_to_bytes(svg_string="\n".join(parts))))
    return out


def main(only=None):
    folders = sorted(p for p in SVG_DIR.iterdir() if p.is_dir())
    for folder in folders:
        if only and folder.name not in only:
            continue
        items = []
        for svg in sorted(folder.glob("*.svg")):
            png = OUT_DIR / folder.name / (svg.stem + ".png")
            render(svg, png)
            w, h = svg_size(svg.read_text())
            items.append((svg.stem, png, w, h))
        if items:
            sheet = contact_sheet(folder.name, items)
            print(f"{folder.name:12s} {len(items):3d} sprites  ->  {sheet.relative_to(ROOT.parent)}")


if __name__ == "__main__":
    main(set(sys.argv[1:]) or None)
