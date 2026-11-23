#!/usr/bin/env bash
set -euo pipefail

# Verificações não destrutivas na hospedagem: sem credenciais e sem POST/PUT/DELETE.
BASE_URL="${1:-https://gymflowfit.runasp.net}"
BASE_URL="${BASE_URL%/}"
failures=0

check() {
  local path="$1" expected="$2" status
  status="$(curl --silent --show-error --location --retry 2 --retry-delay 2 \
    --connect-timeout 20 --max-time 60 --header 'Accept: application/json' \
    --output /dev/null --write-out '%{http_code}' "${BASE_URL}${path}")" || status="erro"
  if [[ "$status" == "$expected" ]]; then
    printf 'OK: %s respondeu HTTP %s\n' "$path" "$status"
  else
    printf 'FALHA: %s respondeu %s (esperado %s)\n' "$path" "$status" "$expected" >&2
    failures=$((failures + 1))
  fi
}

check '/Account/Login' 200
check '/api/health' 200
check '/api/members' 401
check '/api/memberships' 401
check '/api/plans' 401
check '/api/sessions' 401
check '/api/workouts' 401
check '/api/me' 401
check '/api/admins' 401
check '/api/trainers' 401
check '/api/dashboard/overview' 401
check '/api/bookings/sessions/1/members' 401
check '/api/class-checkin/active-sessions' 401
check '/api/class-checkin/mine/1' 401

if ((failures > 0)); then
  echo "Smoke test encontrou ${failures} falha(s). Cheque versão publicada, autenticação e hospedagem." >&2
  exit 1
fi
echo 'Proteção das rotas confirmada (somente acessos sem credenciais).'
