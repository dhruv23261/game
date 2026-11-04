import os
import math
from PIL import Image, ImageDraw, ImageFont, ImageFilter

out_dir = "unity/Assets/Resources/UI/CartoonDefeat"
os.makedirs(out_dir, exist_ok=True)

def rounded_rectangle(draw, bounds, radius, fill=None, outline=None, width=1):
    draw.rounded_rectangle(bounds, radius=radius, fill=fill, outline=outline, width=width)

# 1. CARTOON MODAL CARD (640 x 720, supersampled 1280 x 1440)
W, H = 1280, 1440
card = Image.new("RGBA", (W, H), (0, 0, 0, 0))
draw = ImageDraw.Draw(card)

# Outer comic drop shadow
for i in range(12, 0, -2):
    alpha = int(35 * (1.0 - i / 12.0))
    draw.rounded_rectangle([60 + i*2, 60 + i*3, W - 60 + i*2, H - 60 + i*3], radius=70, fill=(10, 15, 30, alpha))

# Thick cartoon outer stroke (Dark midnight navy / purple comic stroke)
draw.rounded_rectangle([60, 60, W - 60, H - 60], radius=70, fill=(30, 24, 48, 255))

# Outer bright accent ring (Playful warm gold / sunset orange outline)
draw.rounded_rectangle([72, 72, W - 72, H - 72], radius=62, fill=(255, 180, 40, 255))
draw.rounded_rectangle([84, 84, W - 84, H - 84], radius=54, fill=(40, 32, 64, 255))

# Inner Card Gradient (Deep playful dark slate purple, soft and friendly)
for y in range(96, H - 96):
    t = (y - 96) / float(H - 192)
    r = int(36 + (48 - 36) * t)
    g = int(32 + (44 - 32) * t)
    b = int(60 + (80 - 60) * t)
    draw.line([96, y, W - 96, y], fill=(r, g, b, 255))

# Re-mask corners with rounded rectangle
mask = Image.new("L", (W, H), 0)
mask_draw = ImageDraw.Draw(mask)
mask_draw.rounded_rectangle([96, 96, W - 96, H - 96], radius=46, fill=255)
inner_content = Image.new("RGBA", (W, H), (0, 0, 0, 0))
inner_draw = ImageDraw.Draw(inner_content)
for y in range(96, H - 96):
    t = (y - 96) / float(H - 192)
    # subtle playful gradient: top slightly lighter violet, bottom dark rich purple
    r = int(45 * (1 - t*0.35))
    g = int(40 * (1 - t*0.3))
    b = int(75 * (1 - t*0.25))
    inner_draw.line([96, y, W - 96, y], fill=(r, g, b, 255))

# Subtle comic polka dots / grid pattern on top 30%
for px in range(120, W - 120, 48):
    for py in range(120, 500, 48):
        inner_draw.ellipse([px - 4, py - 4, px + 4, py + 4], fill=(255, 255, 255, 8))

# Combine with outer border
card.paste(inner_content, (0, 0), mask)
# Top glossy cartoon sheen arc
sheen = Image.new("RGBA", (W, H), (0, 0, 0, 0))
sheen_draw = ImageDraw.Draw(sheen)
sheen_draw.ellipse([100, -200, W - 100, 420], fill=(255, 255, 255, 18))
card.paste(sheen, (0, 0), mask)

card_final = card.resize((640, 720), Image.Resampling.LANCZOS)
card_final.save(os.path.join(out_dir, "UI_CartoonCard_Bg.png"))
print("Saved UI_CartoonCard_Bg.png")


# 2. CARTOON DIZZY STAR / OOPS BADGE (240 x 240, supersampled 480 x 480)
BW, BH = 480, 480
badge = Image.new("RGBA", (BW, BH), (0, 0, 0, 0))
bdraw = ImageDraw.Draw(badge)

# Outer comic explosion starburst (12 points)
center = (BW // 2, BH // 2)
outer_r = 210
inner_r = 160
points = 14
poly = []
for i in range(points * 2):
    r = outer_r if (i % 2 == 0) else inner_r
    angle = i * math.pi / points
    x = center[0] + r * math.cos(angle)
    y = center[1] + r * math.sin(angle)
    poly.append((x, y))

# Dark outline
bdraw.polygon(poly, fill=(35, 20, 10, 255))

# Shrunk starburst for vibrant yellow / orange fill
poly_fill = []
for i in range(points * 2):
    r = (outer_r - 12) if (i % 2 == 0) else (inner_r - 8)
    angle = i * math.pi / points
    x = center[0] + r * math.cos(angle)
    y = center[1] + r * math.sin(angle)
    poly_fill.append((x, y))
bdraw.polygon(poly_fill, fill=(255, 195, 25, 255))

# Inner bright comic circle (cute dizzy character face)
face_r = 135
bdraw.ellipse([center[0] - face_r - 8, center[1] - face_r - 8, center[0] + face_r + 8, center[1] + face_r + 8], fill=(40, 25, 15, 255))
bdraw.ellipse([center[0] - face_r, center[1] - face_r, center[0] + face_r, center[1] + face_r], fill=(255, 110, 60, 255))

# Inner gradient on face
for y in range(center[1] - face_r, center[1] + face_r):
    dist = math.sqrt(face_r**2 - (y - center[1])**2)
    t = (y - (center[1] - face_r)) / (2.0 * face_r)
    r = int(255)
    g = int(120 + 70 * t)
    b = int(50)
    bdraw.line([center[0] - dist, y, center[0] + dist, y], fill=(r, g, b, 255))

# Cartoon dizzy eyes: Left eye = 'X', Right eye = 'X' (comic KO face)
def draw_comic_x(x, y, sz, thick=14):
    for off in range(-thick//2, thick//2 + 1):
        bdraw.line([x - sz + off, y - sz, x + sz + off, y + sz], fill=(45, 15, 20, 255), width=3)
        bdraw.line([x - sz + off, y + sz, x + sz + off, y - sz], fill=(45, 15, 20, 255), width=3)
    bdraw.line([x - sz, y - sz, x + sz, y + sz], fill=(255, 255, 255, 255), width=thick//2)
    bdraw.line([x - sz, y + sz, x + sz, y - sz], fill=(255, 255, 255, 255), width=thick//2)

draw_comic_x(center[0] - 55, center[1] - 25, 34, thick=12)
draw_comic_x(center[0] + 55, center[1] - 25, 34, thick=12)

# Cute open wobbly mouth (comic wavy / dizzy expression :o)
mouth_box = [center[0] - 42, center[1] + 25, center[0] + 42, center[1] + 75]
bdraw.ellipse(mouth_box, fill=(50, 15, 20, 255))
bdraw.ellipse([mouth_box[0] + 6, mouth_box[1] + 6, mouth_box[2] - 6, mouth_box[3] - 6], fill=(210, 45, 75, 255))
# Cute tongue
bdraw.chord([mouth_box[0] + 12, mouth_box[1] + 24, mouth_box[2] - 12, mouth_box[3] + 4], start=0, end=180, fill=(255, 140, 160, 255))

# Swirling dizzy stars around head (top-left & top-right)
def draw_mini_star(cx, cy, s):
    st_pts = []
    for k in range(10):
        sr = s if (k % 2 == 0) else s * 0.45
        sang = k * math.pi / 5 - math.pi / 2
        st_pts.append((cx + sr * math.cos(sang), cy + sr * math.sin(sang)))
    bdraw.polygon(st_pts, fill=(255, 240, 80, 255), outline=(60, 30, 0, 255))

draw_mini_star(center[0] - 120, center[1] - 110, 28)
draw_mini_star(center[0] + 125, center[1] - 105, 24)
draw_mini_star(center[0] + 15, center[1] - 145, 18)

badge_final = badge.resize((240, 240), Image.Resampling.LANCZOS)
badge_final.save(os.path.join(out_dir, "UI_Cartoon_DizzyBadge.png"))
print("Saved UI_Cartoon_DizzyBadge.png")


# 3. REASON BUBBLE (540 x 110, supersampled 1080 x 220)
RW, RH = 1080, 220
bubble = Image.new("RGBA", (RW, RH), (0, 0, 0, 0))
bdraw = ImageDraw.Draw(bubble)

# Dark comic border
bdraw.rounded_rectangle([10, 10, RW - 10, RH - 10], radius=50, fill=(22, 20, 38, 255))
# Bright cyan/blue playful comic accent rim
bdraw.rounded_rectangle([18, 18, RW - 18, RH - 18], radius=44, fill=(65, 145, 245, 255))
# Inner dark sleek bubble body
bdraw.rounded_rectangle([28, 28, RW - 28, RH - 28], radius=36, fill=(26, 28, 52, 255))

# Subtle top shine
bdraw.rounded_rectangle([36, 32, RW - 36, 75], radius=20, fill=(255, 255, 255, 14))

bubble_final = bubble.resize((540, 110), Image.Resampling.LANCZOS)
bubble_final.save(os.path.join(out_dir, "UI_Cartoon_ReasonBubble.png"))
print("Saved UI_Cartoon_ReasonBubble.png")


# 4. JUICY CANDY GREEN "PLAY AGAIN" BUTTON (520 x 86, supersampled 1040 x 172)
PW, PH = 1040, 172
btn_play = Image.new("RGBA", (PW, PH), (0, 0, 0, 0))
pdraw = ImageDraw.Draw(btn_play)

# Dark comic bottom shadow / outline
pdraw.rounded_rectangle([10, 12, PW - 10, PH - 4], radius=50, fill=(15, 55, 25, 255))
# 3D dark green bottom bevel (gives that punchy pop!)
pdraw.rounded_rectangle([10, 10, PW - 10, PH - 10], radius=50, fill=(25, 120, 45, 255))

# Top face of button (Vibrant candy lime green)
pdraw.rounded_rectangle([10, 10, PW - 10, PH - 24], radius=48, fill=(70, 205, 50, 255))

# Top glossy candy pill shine
pdraw.rounded_rectangle([28, 18, PW - 28, 70], radius=28, fill=(255, 255, 255, 75))

btn_play_final = btn_play.resize((520, 86), Image.Resampling.LANCZOS)
btn_play_final.save(os.path.join(out_dir, "UI_Btn_PlayAgain_Juicy.png"))
print("Saved UI_Btn_PlayAgain_Juicy.png")


# 5. JUICY CANDY BLUE "MAIN MENU" BUTTON (520 x 74, supersampled 1040 x 148)
MW, MH = 1040, 148
btn_menu = Image.new("RGBA", (MW, MH), (0, 0, 0, 0))
mdraw = ImageDraw.Draw(btn_menu)

# Dark comic bottom outline
mdraw.rounded_rectangle([10, 10, MW - 10, MH - 4], radius=44, fill=(20, 25, 60, 255))
# 3D dark blue bottom bevel
mdraw.rounded_rectangle([10, 8, MW - 10, MH - 10], radius=44, fill=(40, 75, 160, 255))

# Top face (Friendly vibrant sky blue / purple)
mdraw.rounded_rectangle([10, 8, MW - 10, MH - 22], radius=42, fill=(65, 135, 235, 255))

# Top glossy candy shine
mdraw.rounded_rectangle([26, 16, MW - 26, 58], radius=24, fill=(255, 255, 255, 65))

btn_menu_final = btn_menu.resize((520, 74), Image.Resampling.LANCZOS)
btn_menu_final.save(os.path.join(out_dir, "UI_Btn_Menu_Juicy.png"))
print("Saved UI_Btn_Menu_Juicy.png")

print("All cartoon defeat UI textures generated successfully!")
