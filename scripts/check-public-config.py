#!/usr/bin/env python3
"""Rejeita credenciais de produção no appsettings versionado."""
import json
import re
from pathlib import Path

path = Path("GymManagementPL/appsettings.json")
data = json.loads(path.read_text(encoding="utf-8-sig"))
if data.get("ConnectionStrings"):
    raise SystemExit("ERRO: não versionar ConnectionStrings no appsettings.json. Use ConnectionStrings__DefaultConnection na hospedagem.")
if data.get("Jwt", {}).get("Key"):
    raise SystemExit("ERRO: não versionar Jwt:Key. Configure Jwt__Key em ambiente protegido.")
text = path.read_text(encoding="utf-8-sig")
if re.search(r"(?i)(password|pwd|secret|private_key)\s*[=:]", text):
    raise SystemExit("ERRO: campo sensível encontrado em appsettings.json.")
print("OK: arquivo público de configuração sem senhas nem chaves.")
