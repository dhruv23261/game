import zlib, struct, math, os

def write_png(filename, width, height, rgba_bytes):
    def chunk(tag, data):
        return struct.pack('>I', len(data)) + tag + data + struct.pack('>I', zlib.crc32(tag + data) & 0xffffffff)
    
    raw = bytearray()
    for y in range(height):
        raw.append(0) # filter none
        raw.extend(rgba_bytes[y * width * 4 : (y + 1) * width * 4])
    
    ihdr = struct.pack('>IIBBBBB', width, height, 8, 6, 0, 0, 0)
    idat = zlib.compress(bytes(raw), 9)
    png = b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', ihdr) + chunk(b'IDAT', idat) + chunk(b'IEND', b'')
    with open(filename, 'wb') as f:
        f.write(png)
    print(f"Generated {filename} ({width}x{height}, {len(png)} bytes)")

folder = 'unity/Assets/Resources/UI/CharactersDynamic'

# 1. Generate Clean Glowing Platform (Pedestal) - ZERO BOOTS!
pw, ph = 512, 150
ped_buf = bytearray(pw * ph * 4)

pcx = pw * 0.5
pcy = ph * 0.42
prx = pw * 0.44
pry = ph * 0.32

for y in range(ph):
    inv_y = (ph - 1) - y # PNG is top to bottom
    for x in range(pw):
        dx = (x - pcx) / prx
        dy = (inv_y - pcy) / pry
        dist = math.sqrt(dx * dx + dy * dy)

        dySkirt = (inv_y - (pcy - 12.0)) / pry
        distSkirt = math.sqrt(dx * dx + dySkirt * dySkirt)

        r, g, b, a = 0, 0, 0, 0

        if dist <= 1.0:
            if dist >= 0.93:
                # Outer glowing cyan rim
                t = (1.0 - dist) / 0.07
                r = int(30 + t * 180)
                g = int(230 + t * 25)
                b = 255
                a = 255
            elif dist >= 0.88:
                # Dark separator ring
                r, g, b, a = 5, 30, 90, 245
            elif dist >= 0.72:
                # Secondary cyan ring
                r, g, b, a = 20, 190, 255, 245
            elif dist >= 0.68:
                # Dark separator ring
                r, g, b, a = 5, 30, 90, 245
            elif dist >= 0.52:
                # Inner cyan ring
                r, g, b, a = 38, 210, 255, 245
            else:
                # Center core disc with radial glow
                t = dist / 0.52
                r = int(60 * (1 - t) + 10 * t)
                g = int(200 * (1 - t) + 80 * t)
                b = int(255 * (1 - t) + 200 * t)
                a = 255
        elif inv_y < pcy and distSkirt <= 1.03:
            # 3D bevel skirt
            bevelT = min(1.0, (pcy - inv_y) / 18.0)
            r = int(8 * (1 - bevelT) + 3 * bevelT)
            g = int(55 * (1 - bevelT) + 20 * bevelT)
            b = int(140 * (1 - bevelT) + 65 * bevelT)
            a = 245
        elif dist > 1.0 and dist < 1.14:
            # Soft neon glow
            glowT = (dist - 1.0) / 0.14
            alpha = max(0, int((1.0 - glowT) * 140))
            r, g, b, a = 0, 200, 255, alpha

        idx = (y * pw + x) * 4
        ped_buf[idx] = r
        ped_buf[idx + 1] = g
        ped_buf[idx + 2] = b
        ped_buf[idx + 3] = a

ped_path = os.path.join(folder, '14_glow_platform_clean.png')
write_png(ped_path, pw, ph, ped_buf)

# 2. Generate Clean Showcase Frame - ZERO SOLDIER!
fw, fh = 850, 470
frame_buf = bytearray(fw * fh * 4)
cornerCut = 48.0

for y in range(fh):
    inv_y = (fh - 1) - y
    vGrad = inv_y / float(fh)
    # Background gradient: deep cyber blue
    bg_r = int(5 + vGrad * 10)
    bg_g = int(20 + vGrad * 55)
    bg_b = int(70 + vGrad * 120)

    for x in range(fw):
        distLeft = x
        distRight = (fw - 1) - x
        distBottom = inv_y
        distTop = (fh - 1) - inv_y

        minEdge = min(distLeft, distRight, distBottom, distTop)

        cTL = (cornerCut - x) + (cornerCut - ((fh - 1) - inv_y))
        cTR = (x - ((fw - 1) - cornerCut)) + (cornerCut - ((fh - 1) - inv_y))
        cBL = (cornerCut - x) + (cornerCut - inv_y)
        cBR = (x - ((fw - 1) - cornerCut)) + (cornerCut - inv_y)

        maxCornerCut = max(cTL, cTR, cBL, cBR)

        r, g, b, a = 0, 0, 0, 0

        if maxCornerCut <= cornerCut and minEdge >= 0:
            distToBorder = min(minEdge, cornerCut - maxCornerCut)

            if distToBorder < 5.0:
                # Outer deep blue stroke
                r, g, b, a = 8, 124, 255, 255
            elif distToBorder < 10.0:
                # Glowing neon cyan inner stroke
                r, g, b, a = 32, 234, 255, 255
            elif distToBorder < 13.0:
                # Border shadow
                r, g, b, a = 3, 15, 50, 255
            else:
                # Radiant sunburst rays in chamber background
                angle = math.atan2(inv_y - fh * 0.42, x - fw * 0.5)
                ray = math.sin(angle * 12.0)
                rayB = max(0.0, ray * 0.15)

                r = min(255, int(bg_r + rayB * 30))
                g = min(255, int(bg_g + rayB * 80))
                b = min(255, int(bg_b + rayB * 140))
                a = 245
        elif maxCornerCut < cornerCut + 4.0 and minEdge > -4.0:
            # Soft outer neon cyan border glow
            r, g, b, a = 0, 160, 255, 90

        idx = (y * fw + x) * 4
        frame_buf[idx] = r
        frame_buf[idx + 1] = g
        frame_buf[idx + 2] = b
        frame_buf[idx + 3] = a

frame_path = os.path.join(folder, '12_main_showcase_panel_clean.png')
write_png(frame_path, fw, fh, frame_buf)

# 3. Create .meta files for both new PNGs
meta_template = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 0
    wrapV: 0
    wrapW: 0
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {x: 0.5, y: 0.5}
  spritePixelsToUnits: 100
  spriteBorder: {border}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationMethod: 0
  spriteTessellationDetail: -1
  spriteGeometrySubdivision: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: 2048
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
"""

with open(ped_path + '.meta', 'w') as f:
    f.write(meta_template.replace('{guid}', 'c758143e7de9f4b7e83a7418a67a48c1').replace('{border}', '{x: 0, y: 0, z: 0, w: 0}'))

with open(frame_path + '.meta', 'w') as f:
    f.write(meta_template.replace('{guid}', '8f276182ad0454371bfbe55c3c13ff91').replace('{border}', '{x: 40, y: 40, z: 40, w: 40}'))

print("Created .meta files for clean assets.")
