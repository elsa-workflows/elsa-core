#!/usr/bin/env bash
set -euo pipefail

if [ $# -lt 2 ]; then
  echo "Usage: $0 <image> <tls_app_dir> [docker-run-arg ...]" >&2
  exit 1
fi

IMAGE="$1"
TLS_APP_DIR="$2"
shift 2 || true
DOCKER_ARGS=("$@")

SMOKE_DLL=/tls/TlsSmoke.dll
PUBLIC_URL="https://example.com"
LOCAL_PORT=9443

run_smoke() {
  local url="$1"
  shift
  docker run --rm \
    -v "${TLS_APP_DIR}:/tls:ro" \
    "${DOCKER_ARGS[@]}" \
    "$@" \
    "$IMAGE" \
    dotnet "$SMOKE_DLL" "$url"
}

run_public_test() {
  echo "[CA] Validating public trust store against ${PUBLIC_URL}" >&2
  run_smoke "$PUBLIC_URL"
}

start_local_ca_server() {
  CERT_WORKDIR=$(mktemp -d)

  openssl req -x509 -newkey rsa:2048 -days 2 -nodes -keyout "$CERT_WORKDIR/ca.key" -out "$CERT_WORKDIR/ca.crt" -subj "/CN=ElsaTestCA" >/dev/null 2>&1
  openssl req -newkey rsa:2048 -nodes -keyout "$CERT_WORKDIR/server.key" -out "$CERT_WORKDIR/server.csr" -subj "/CN=host.docker.internal" >/dev/null 2>&1

  cat <<CERTEXT > "$CERT_WORKDIR/server.ext"
subjectAltName = DNS:localhost,DNS:host.docker.internal,IP:127.0.0.1
extendedKeyUsage = serverAuth
keyUsage = digitalSignature, keyEncipherment
CERTEXT

  openssl x509 -req -in "$CERT_WORKDIR/server.csr" -CA "$CERT_WORKDIR/ca.crt" -CAkey "$CERT_WORKDIR/ca.key" -CAcreateserial -out "$CERT_WORKDIR/server.crt" -days 2 -sha256 -extfile "$CERT_WORKDIR/server.ext" >/dev/null 2>&1

  openssl s_server -quiet -accept "$LOCAL_PORT" -www -cert "$CERT_WORKDIR/server.crt" -key "$CERT_WORKDIR/server.key" >/dev/null 2>&1 &
  SERVER_PID=$!

  for _ in {1..20}; do
    if nc -z localhost "$LOCAL_PORT" >/dev/null 2>&1; then
      break
    fi
    sleep 0.2
  done

  echo "$CERT_WORKDIR"
}

run_extra_ca_test() {
  local cert_dir
  cert_dir=$(start_local_ca_server)
  trap "kill ${SERVER_PID:-0} >/dev/null 2>&1 || true; rm -rf '$cert_dir'" EXIT

  echo "[CA] Validating EXTRA_CA_CERT flow against local CA" >&2
  run_smoke "https://host.docker.internal:${LOCAL_PORT}" \
    --add-host host.docker.internal:host-gateway \
    -v "${cert_dir}:/certs:ro" \
    -e EXTRA_CA_CERT=/certs/ca.crt

  echo "[CA] Validating SSL_CERT_FILE fallback" >&2
  run_smoke "https://host.docker.internal:${LOCAL_PORT}" \
    --add-host host.docker.internal:host-gateway \
    -v "${cert_dir}:/certs:ro" \
    -e SSL_CERT_FILE=/certs/ca.crt

  mkdir -p "$cert_dir/dir"
  cp "$cert_dir/ca.crt" "$cert_dir/dir/custom-ca.crt"
  openssl rehash "$cert_dir/dir" >/dev/null 2>&1

  echo "[CA] Validating SSL_CERT_DIR fallback" >&2
  run_smoke "https://host.docker.internal:${LOCAL_PORT}" \
    --add-host host.docker.internal:host-gateway \
    -v "${cert_dir}:/certs:ro" \
    -e SSL_CERT_DIR=/certs/dir

  kill "${SERVER_PID:-0}" >/dev/null 2>&1 || true
  wait "${SERVER_PID:-0}" 2>/dev/null || true
  rm -rf "$cert_dir"
  trap - EXIT
}

run_public_test
run_extra_ca_test
