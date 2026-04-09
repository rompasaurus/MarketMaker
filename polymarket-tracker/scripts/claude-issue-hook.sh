#!/usr/bin/env bash
set -euo pipefail

# Usage: ./scripts/claude-issue-hook.sh <issue-number>
# Fetches issue details from the API, checks out the branch, and launches
# Claude Code with the issue context pre-loaded into the prompt.

API_BASE="${TRACKER_API_URL:-http://localhost:5000}"

if [ $# -lt 1 ]; then
  echo "Usage: $0 <issue-number>"
  echo "  Fetches issue details, checks out the branch, and opens Claude Code with context."
  exit 1
fi

ISSUE_NUMBER="$1"

# Fetch issue by ID (issue number is the ID in our simple system)
RESPONSE=$(curl -sf "$API_BASE/api/issues" | \
  python3 -c "
import sys, json
issues = json.load(sys.stdin)
for i in issues:
    if i['issueNumber'] == $ISSUE_NUMBER:
        print(json.dumps(i))
        sys.exit(0)
print('null')
")

if [ "$RESPONSE" = "null" ]; then
  echo "Issue #$ISSUE_NUMBER not found"
  exit 1
fi

TITLE=$(echo "$RESPONSE" | python3 -c "import sys,json; print(json.load(sys.stdin)['title'])")
DESCRIPTION=$(echo "$RESPONSE" | python3 -c "import sys,json; d=json.load(sys.stdin).get('description',''); print(d if d else 'No description')")
GIT_BRANCH=$(echo "$RESPONSE" | python3 -c "import sys,json; print(json.load(sys.stdin).get('gitBranch',''))")
LABELS=$(echo "$RESPONSE" | python3 -c "import sys,json; print(json.load(sys.stdin).get('labels','') or 'none')")

# Checkout the branch
if [ -n "$GIT_BRANCH" ]; then
  echo "Checking out branch: $GIT_BRANCH"
  git checkout "$GIT_BRANCH" 2>/dev/null || git checkout -b "$GIT_BRANCH" 2>/dev/null || true
fi

# Build the Claude Code prompt
PROMPT="Working on issue #${ISSUE_NUMBER}: ${TITLE}

Description: ${DESCRIPTION}

Labels: ${LABELS}
Branch: ${GIT_BRANCH}

Instructions:
- Use conventional commits with 'Refs: #${ISSUE_NUMBER}' in all commit messages
- Stay focused on this specific issue
- Update the issue status when done via: curl -X PUT ${API_BASE}/api/issues/{id} -H 'Content-Type: application/json' -d '{\"status\":\"closed\"}'
"

echo "========================================="
echo "Issue #$ISSUE_NUMBER: $TITLE"
echo "Branch: $GIT_BRANCH"
echo "========================================="
echo ""
echo "Launching Claude Code with issue context..."
echo ""

# Launch Claude Code with the prompt
claude --print "$PROMPT"
