#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
HOOKS_DIR="$REPO_ROOT/.git/hooks"

# Ensure we're in a git repo
if [ ! -d "$REPO_ROOT/.git" ]; then
  # May be a subdirectory of a larger repo
  HOOKS_DIR="$(git rev-parse --git-dir)/hooks"
fi

mkdir -p "$HOOKS_DIR"

# Install commit-msg hook
cat > "$HOOKS_DIR/commit-msg" << 'HOOK'
#!/usr/bin/env bash
# Enforce conventional commits and auto-append issue references

COMMIT_MSG_FILE="$1"
COMMIT_MSG=$(cat "$COMMIT_MSG_FILE")

# Check conventional commit format
if ! echo "$COMMIT_MSG" | head -1 | grep -qE '^(feat|fix|refactor|docs|test|chore|style|perf|ci|build|revert)(\(.+\))?!?: .+'; then
  echo "ERROR: Commit message must follow conventional commit format:"
  echo "  type(scope): description"
  echo ""
  echo "  Types: feat, fix, refactor, docs, test, chore, style, perf, ci, build, revert"
  echo "  Example: feat(markets): add search filtering"
  exit 1
fi

# Auto-append issue reference if on an issue branch
BRANCH=$(git symbolic-ref --short HEAD 2>/dev/null || true)
if [[ "$BRANCH" =~ ^issue/([0-9]+)- ]]; then
  ISSUE_NUM="${BASH_REMATCH[1]}"
  if ! grep -q "Refs: #$ISSUE_NUM" "$COMMIT_MSG_FILE"; then
    echo "" >> "$COMMIT_MSG_FILE"
    echo "Refs: #$ISSUE_NUM" >> "$COMMIT_MSG_FILE"
  fi
fi
HOOK

chmod +x "$HOOKS_DIR/commit-msg"

echo "Git hooks installed:"
echo "  commit-msg → conventional commits + auto issue reference"
echo ""
echo "Done!"
