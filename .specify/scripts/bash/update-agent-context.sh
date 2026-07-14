#!/usr/bin/env bash

# Retained as a Speckit compatibility entry point. Plan-derived agent context
# is intentionally disabled so feature plans cannot rewrite repository-wide
# instructions with stale technology or historical-change summaries.

set -euo pipefail

echo "Plan-derived agent context generation is disabled; maintain AGENTS.md directly."
