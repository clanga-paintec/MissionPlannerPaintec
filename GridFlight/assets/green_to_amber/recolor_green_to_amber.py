"""
recolor_green_to_amber.py
─────────────────────────
Reemplaza píxeles verdes por ámbar (#FFC107) en todos los assets PNG/JPG.

Estrategia:
  1. Convierte cada imagen a HSV.
  2. Crea una máscara para los píxeles cuyo matiz cae en el rango "verde"
     (rango primario ~36°–168° en escala HSV de 0-180 de OpenCV).
  3. Para cada píxel enmascarado, desplaza el matiz al tono ámbar (#FFC107 → H≈25 en OpenCV)
     preservando la saturación y el brillo originales (solo cambia HUE).
  4. Guarda el resultado en la subcarpeta `output/` (nunca sobreescribe los originales).

Uso:
  python recolor_green_to_amber.py [--dry-run] [--preview]

  --dry-run   Solo imprime qué archivos se procesarían sin escribir nada.
  --preview   Abre una ventana de comparación lado a lado para el primer archivo encontrado.
"""

import argparse
import sys
from pathlib import Path

import cv2
import numpy as np


# ─── Configuración de color ──────────────────────────────────────────────────

# Verde en OpenCV HSV (H: 0-180, S: 0-255, V: 0-255)
# Rango 1: verde puro (~60°-150° en 0-360°  → 30-75 en OpenCV)
GREEN_H_MIN1, GREEN_H_MAX1 = 36, 85      # verde puro
# Rango 2: verde-amarillento claro (para íconos que usan verde brillante estilo "lime")
GREEN_H_MIN2, GREEN_H_MAX2 = 30, 36      # verde-lima limítrofe

# Saturación mínima para considerar un píxel como "verde" (filtra grises)
GREEN_S_MIN = 50   # 0-255
# Brillo mínimo (filtra negros casi puros)
GREEN_V_MIN = 30   # 0-255

# Ámbar destino: #FFC107
# H: ~43° / 2 = ~21.5 en OpenCV → usamos 22
# S: ~255 (saturación máxima)
AMBER_H = 22          # Hue objetivo en escala OpenCV (0-180)

# ─────────────────────────────────────────────────────────────────────────────


def build_green_mask(hsv: np.ndarray) -> np.ndarray:
    """Devuelve una máscara binaria de los píxeles verdes en la imagen HSV."""
    h, s, v = hsv[:, :, 0], hsv[:, :, 1], hsv[:, :, 2]

    green1 = (h >= GREEN_H_MIN1) & (h <= GREEN_H_MAX1)
    green2 = (h >= GREEN_H_MIN2) & (h <= GREEN_H_MAX2)
    saturated = s >= GREEN_S_MIN
    bright = v >= GREEN_V_MIN

    return (green1 | green2) & saturated & bright


def recolor_image(img_bgra: np.ndarray) -> tuple[np.ndarray, int]:
    """
    Recibe una imagen BGRA (o BGR). Devuelve (imagen_recoloreada_BGRA, n_pixeles_cambiados).
    """
    has_alpha = img_bgra.shape[2] == 4

    if has_alpha:
        bgr = img_bgra[:, :, :3]
        alpha = img_bgra[:, :, 3]
    else:
        bgr = img_bgra
        alpha = None

    hsv = cv2.cvtColor(bgr, cv2.COLOR_BGR2HSV)
    mask = build_green_mask(hsv)

    n_changed = int(mask.sum())
    if n_changed == 0:
        return img_bgra, 0

    # Desplaza solo el canal HUE en los píxeles enmascarados
    # Preserva saturación y brillo para mantener sombras/iluminaciones naturales
    hsv_out = hsv.copy()
    hsv_out[mask, 0] = AMBER_H  # reemplaza HUE

    # Boost suave de saturación para que el ámbar sea vibrante
    # (muchos íconos tienen verde con S relativa alta; lo mantenemos, solo clampeamos)
    s_boosted = np.clip(hsv_out[:, :, 1].astype(np.int16) + 20, 0, 255).astype(np.uint8)
    hsv_out[mask, 1] = s_boosted[mask]

    bgr_out = cv2.cvtColor(hsv_out, cv2.COLOR_HSV2BGR)

    if has_alpha:
        result = np.dstack([bgr_out, alpha])
    else:
        result = bgr_out

    return result, n_changed


def process_file(src: Path, dst: Path, dry_run: bool = False) -> int:
    """Procesa un archivo. Devuelve número de píxeles cambiados (0 = sin verde)."""
    # Lee conservando canal alpha si existe
    img = cv2.imread(str(src), cv2.IMREAD_UNCHANGED)
    if img is None:
        print(f"  [SKIP] No se pudo leer: {src.name}")
        return -1

    # Asegura 4 canales si PNG con alpha, o 3 para el resto
    if img.ndim == 2:
        img = cv2.cvtColor(img, cv2.COLOR_GRAY2BGRA)
    elif img.shape[2] == 3:
        img = cv2.cvtColor(img, cv2.COLOR_BGR2BGRA)

    result, n = recolor_image(img)

    if n == 0:
        print(f"  [---]  Sin verde: {src.name}")
        return 0

    if dry_run:
        print(f"  [DRY]  {src.name}  →  {n:,} píxeles verdes detectados")
        return n

    dst.parent.mkdir(parents=True, exist_ok=True)

    # Mantiene formato original
    ext = src.suffix.lower()
    if ext in (".jpg", ".jpeg"):
        # JPG no soporta alpha → convierte a BGR
        out_bgr = cv2.cvtColor(result, cv2.COLOR_BGRA2BGR)
        cv2.imwrite(str(dst), out_bgr, [cv2.IMWRITE_JPEG_QUALITY, 95])
    else:
        cv2.imwrite(str(dst), result)

    print(f"  [OK]   {src.name}  →  {n:,} píxeles recoloreados  →  {dst.relative_to(dst.parent.parent.parent)}")
    return n


def preview_first(assets_dir: Path):
    """Abre una ventana de comparación para el primer asset con verde."""
    exts = {".png", ".jpg", ".jpeg"}
    for src in sorted(assets_dir.iterdir()):
        if src.suffix.lower() not in exts or src.is_dir():
            continue
        img = cv2.imread(str(src), cv2.IMREAD_UNCHANGED)
        if img is None:
            continue
        if img.ndim == 2:
            img = cv2.cvtColor(img, cv2.COLOR_GRAY2BGRA)
        elif img.shape[2] == 3:
            img = cv2.cvtColor(img, cv2.COLOR_BGR2BGRA)

        result, n = recolor_image(img)
        if n == 0:
            continue

        # Lado a lado (fondo gris para transparencias)
        def flatten(im):
            bg = np.full((*im.shape[:2], 3), 80, dtype=np.uint8)
            if im.shape[2] == 4:
                a = im[:, :, 3:4].astype(np.float32) / 255.0
                fg = im[:, :, :3].astype(np.float32)
                blended = (fg * a + bg * (1 - a)).astype(np.uint8)
            else:
                blended = im[:, :, :3]
            return blended

        left = flatten(img)
        right = flatten(result)
        # Resize al mismo alto si difieren
        h = max(left.shape[0], right.shape[0])
        sep = np.full((h, 4, 3), 200, dtype=np.uint8)

        def pad_h(im, target):
            if im.shape[0] < target:
                pad = np.full((target - im.shape[0], im.shape[1], 3), 80, dtype=np.uint8)
                return np.vstack([im, pad])
            return im

        canvas = np.hstack([pad_h(left, h), sep, pad_h(right, h)])
        cv2.imshow(f"ORIGINAL  |  ÁMBAR  —  {src.name}  ({n} px)", canvas)
        print(f"Previsualización de '{src.name}' ({n} px verdes). Presiona cualquier tecla para cerrar.")
        cv2.waitKey(0)
        cv2.destroyAllWindows()
        return

    print("No se encontró ningún archivo con píxeles verdes para previsualizar.")


def main():
    parser = argparse.ArgumentParser(description="Reemplaza verde → ámbar en assets PNG/JPG.")
    parser.add_argument("--dry-run", action="store_true", help="Solo reporta, no escribe archivos.")
    parser.add_argument("--preview", action="store_true", help="Muestra comparación del primer archivo con verde.")
    args = parser.parse_args()

    # El script está en assets/green_to_amber/ → assets/ es el padre
    assets_dir = Path(__file__).resolve().parent.parent
    output_dir = Path(__file__).resolve().parent / "output"

    print(f"Carpeta fuente : {assets_dir}")
    print(f"Carpeta salida : {output_dir}")
    print(f"Rango verde HUE: [{GREEN_H_MIN2}–{GREEN_H_MAX1}] (OpenCV 0-180)  S≥{GREEN_S_MIN}  V≥{GREEN_V_MIN}")
    print(f"HUE ámbar destino: {AMBER_H}  (#FFC107)\n")

    if args.preview:
        preview_first(assets_dir)
        return

    exts = {".png", ".jpg", ".jpeg"}
    files = sorted(f for f in assets_dir.iterdir() if f.suffix.lower() in exts and f.is_file())

    if not files:
        print("No se encontraron imágenes en la carpeta.")
        sys.exit(0)

    total_files = 0
    total_px = 0
    skipped = 0

    for src in files:
        dst = output_dir / src.name
        result = process_file(src, dst, dry_run=args.dry_run)
        if result > 0:
            total_files += 1
            total_px += result
        elif result == -1:
            skipped += 1

    print(f"\n{'─'*55}")
    if args.dry_run:
        print(f"DRY RUN: {total_files} archivos con verde  |  {total_px:,} píxeles en total  |  {skipped} errores")
    else:
        print(f"Listo: {total_files} archivos recoloreados  |  {total_px:,} píxeles  |  {skipped} errores")
        print(f"Resultados en: {output_dir}")


if __name__ == "__main__":
    main()
