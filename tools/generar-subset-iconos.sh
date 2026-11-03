#!/usr/bin/env bash
# Regenera los subconjuntos de Material Symbols Rounded (FILL 0 y FILL 1) a partir de los
# glifos usados en src/CineList/Helpers/Iconos.cs. Ejecutar al agregar un glifo nuevo.
# Fuente: https://github.com/google/material-design-icons/tree/master/variablefont (Apache 2.0)
set -euo pipefail
RAIZ="$(cd "$(dirname "$0")/.." && pwd)"
FUENTES="$RAIZ/src/CineList/Resources/Fonts"
ICONOS="$RAIZ/src/CineList/Helpers/Iconos.cs"
TMP="$(mktemp -d)"
URL="https://github.com/google/material-design-icons/raw/master/variablefont/MaterialSymbolsRounded%5BFILL,GRAD,opsz,wght%5D"
curl -sLf -o "$TMP/ms.ttf" "$URL.ttf"
python3 -m venv "$TMP/v" && "$TMP/v/bin/pip" -q install fonttools brotli
cat > "$TMP/gen.py" <<'PY'
import re, sys
from fontTools.ttLib import TTFont
from fontTools.varLib import instancer
from fontTools import subset
tmp, iconos, salida = sys.argv[1:4]
# Codepoints: todas las constantes "\uXXXX" de Iconos.cs (+ extras que se quieran agregar aqui)
uni = sorted({int(m, 16) for m in re.findall(r'\\u([0-9a-fA-F]{4})', open(iconos, encoding='utf-8').read())})
for fill, nombre in ((0, "MaterialSymbolsRounded.ttf"), (1, "MaterialSymbolsRoundedFill.ttf")):
    f = instancer.instantiateVariableFont(TTFont(tmp + "/ms.ttf"), {"FILL": fill, "wght": 400, "GRAD": 0, "opsz": 24})
    f.save(tmp + "/inst.ttf")
    o = subset.Options(); o.layout_features = ["*"]
    s = subset.Subsetter(o); t = TTFont(tmp + "/inst.ttf")
    s.populate(unicodes=uni); s.subset(t); t.save(salida + "/" + nombre)
print("glifos:", " ".join("%04x" % u for u in uni))
PY
"$TMP/v/bin/python" "$TMP/gen.py" "$TMP" "$ICONOS" "$FUENTES"
rm -rf "$TMP"
