#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="/workspaces/mynode"
PROJECT_DIR="$ROOT_DIR/myproject"

if [[ ! -d "$PROJECT_DIR/openspec" ]]; then
  echo "OpenSpec project not found at $PROJECT_DIR/openspec" >&2
  exit 1
fi

mkdir -p "$HOME/.config/openspec"
cat > "$HOME/.config/openspec/config.json" <<'JSON'
{"featureFlags":{},"profile":"custom","delivery":"both","workflows":["propose","explore","new","continue","apply","ff","sync","archive","bulk-archive","verify","onboard"]}
JSON

cd "$PROJECT_DIR"
openspec update .

for f in "$PROJECT_DIR/.github/prompts"/opsx-*.prompt.md; do
  n="$(basename "$f" .prompt.md)"
  a="${n/opsx-/opsx:}"
  if grep -q '^name:' "$f"; then
    sed -i "s|^name:.*$|name: \"$a\"|" "$f"
  else
    sed -i "/^description:/a name: \"$a\"" "$f"
  fi
done

mkdir -p "$ROOT_DIR/.github"
rm -rf "$ROOT_DIR/.github/prompts" "$ROOT_DIR/.github/skills"
cp -r "$PROJECT_DIR/.github/prompts" "$ROOT_DIR/.github/"
cp -r "$PROJECT_DIR/.github/skills" "$ROOT_DIR/.github/"

echo "Synced prompts/skills from $PROJECT_DIR/.github to $ROOT_DIR/.github"
