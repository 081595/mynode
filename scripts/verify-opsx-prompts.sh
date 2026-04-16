#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="/workspaces/mynode"
PROMPTS_DIR="$ROOT_DIR/.github/prompts"

print_banner() {
  local status="$1"
  local detail="$2"

  echo
  echo "========================================"
  echo "OPSX PROMPT CHECK: $status"
  echo "========================================"
  echo "$detail"
  echo
}

if [[ ! -d "$PROMPTS_DIR" ]]; then
  print_banner "FAILED" "Missing prompts directory: $PROMPTS_DIR" >&2
  exit 1
fi

count=$(find "$PROMPTS_DIR" -maxdepth 1 -name 'opsx-*.prompt.md' | wc -l | tr -d ' ')

if [[ "$count" -eq 0 ]]; then
  print_banner "FAILED" "No opsx prompt files found under $PROMPTS_DIR" >&2
  exit 1
fi

print_banner "OK" "Verified $count prompt files in $PROMPTS_DIR"
echo "Next step: if /opsx-* still does not appear, run 'Developer: Reload Window' once"