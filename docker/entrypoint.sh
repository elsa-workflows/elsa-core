#!/bin/sh
set -e

log() {
  printf '%s\n' "$*" >&2
}

normalise_dest_name() {
  name=$(basename "$1")
  case "$name" in
    *.crt|*.pem) printf '%s\n' "$name" ;;
    *) printf '%s.crt\n' "$name" ;;
  esac
}

install_extra_certificates() {
  cert_source="$1"
  if [ ! -e "$cert_source" ]; then
    log "EXTRA_CA_CERT path '$cert_source' does not exist; skipping installation."
    return
  fi

  target_root="/usr/local/share/ca-certificates/extra"
  mkdir -p "$target_root"
  rm -f "$target_root"/* 2>/dev/null || true

  copied=0
  if [ -f "$cert_source" ]; then
    dest_name=$(normalise_dest_name "$cert_source")
    cp "$cert_source" "$target_root/$dest_name"
    copied=1
  elif [ -d "$cert_source" ]; then
    for file in "$cert_source"/*.crt "$cert_source"/*.pem; do
      [ -f "$file" ] || continue
      dest_name=$(normalise_dest_name "$file")
      cp "$file" "$target_root/$dest_name"
      copied=1
    done
  else
    log "EXTRA_CA_CERT path '$cert_source' is neither a file nor a directory; skipping installation."
    return
  fi

  if [ "$copied" -eq 0 ]; then
    log "No certificate files found at '$cert_source'; skipping installation."
    return
  fi

  if command -v update-ca-certificates >/dev/null 2>&1; then
    if ! update-ca-certificates >/dev/null 2>&1; then
      update-ca-certificates
    fi
    log "Installed custom certificate(s) from '$cert_source'."
  elif command -v trust >/dev/null 2>&1; then
    # Shellcheck disable because we intentionally glob.
    # shellcheck disable=SC2086
    for cert in "$target_root"/*.crt; do
      [ -f "$cert" ] || continue
      trust anchor "$cert"
    done
    log "Installed custom certificate(s) using 'trust' utility."
  else
    log "No known certificate installation tool found; custom CA may not be applied."
  fi
}

maybe_instrument_with_otel() {
  if [ "${ELSA_SKIP_OTEL_AUTO:-0}" = "1" ]; then
    return
  fi

  if [ -z "${OTEL_DOTNET_AUTO_HOME:-}" ]; then
    return
  fi

  instrument_script="${OTEL_DOTNET_AUTO_HOME%/}/instrument.sh"
  if [ ! -f "$instrument_script" ]; then
    return
  fi

  if ! command -v bash >/dev/null 2>&1; then
    log "OpenTelemetry auto-instrumentation requested but bash is unavailable; skipping."
    return
  fi

  tmp_wrapper="/tmp/elsa-otel-wrapper.sh"
  cat <<'WRAPPER' > "$tmp_wrapper"
#!/usr/bin/env bash
set -e
. "${OTEL_DOTNET_AUTO_HOME%/}/instrument.sh"
exec "$@"
WRAPPER
  chmod +x "$tmp_wrapper"
  exec "$tmp_wrapper" "$@"
}

if [ -n "${EXTRA_CA_CERT:-}" ]; then
  install_extra_certificates "$EXTRA_CA_CERT"
fi

maybe_instrument_with_otel "$@"

exec "$@"
