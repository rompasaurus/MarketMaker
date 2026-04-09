#!/usr/bin/env bash
set -euo pipefail

# Usage: ./scripts/create-issue.sh "Issue title" "Issue description" ["label1,label2"]

API_BASE="${TRACKER_API_URL:-http://localhost:5000}"

if [ $# -lt 1 ]; then
  echo "Usage: $0 <title> [description] [labels]"
  echo "  title:       Issue title (required)"
  echo "  description: Issue description (optional)"
  echo "  labels:      Comma-separated labels (optional)"
  exit 1
fi

TITLE="$1"
DESCRIPTION="${2:-}"
LABELS="${3:-}"

PAYLOAD=$(cat <<EOF
{
  "title": "$TITLE",
  "description": $([ -n "$DESCRIPTION" ] && echo "\"$DESCRIPTION\"" || echo "null"),
  "labels": $([ -n "$LABELS" ] && echo "\"$LABELS\"" || echo "null")
}
EOF
)

RESPONSE=$(curl -sf -X POST "$API_BASE/api/issues" \
  -H "Content-Type: application/json" \
  -d "$PAYLOAD")

ISSUE_NUMBER=$(echo "$RESPONSE" | grep -o '"issueNumber":[0-9]*' | grep -o '[0-9]*')
GIT_BRANCH=$(echo "$RESPONSE" | grep -o '"gitBranch":"[^"]*"' | cut -d'"' -f4)

echo "Created issue #$ISSUE_NUMBER: $TITLE"
echo "Branch: $GIT_BRANCH"

# Create and checkout the git branch
if [ -n "$GIT_BRANCH" ]; then
  echo "Creating git branch: $GIT_BRANCH"
  git checkout -b "$GIT_BRANCH" 2>/dev/null || git checkout "$GIT_BRANCH" 2>/dev/null || true
  echo "Switched to branch: $GIT_BRANCH"
fi
