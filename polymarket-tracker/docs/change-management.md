# Change Management System

## Overview

Polymarket Tracker includes a lightweight, self-hosted issue tracking system integrated with git workflows and Claude Code automation.

## Issue Lifecycle

```
Create Issue → Auto-create git branch → Work on branch → Conventional commits → Close issue
```

## Creating Issues

### Via CLI Script
```bash
./scripts/create-issue.sh "Add market search" "Implement search/filter on the markets list" "feat,frontend"
```

This will:
1. Create the issue in the database with an auto-incrementing issue number
2. Generate a git branch: `issue/42-add-market-search`
3. Checkout the branch

### Via API
```bash
curl -X POST http://localhost:5000/api/issues \
  -H "Content-Type: application/json" \
  -d '{"title": "Add market search", "description": "...", "labels": "feat,frontend"}'
```

## Working on Issues

### With Claude Code Integration
```bash
./scripts/claude-issue-hook.sh 42
```

This will:
1. Fetch issue #42 details from the API
2. Checkout the issue branch
3. Launch Claude Code with the issue context pre-loaded

### Commit Format

All commits must follow conventional commit format:

```
type(scope): description

Refs: #IssueNumber
```

**Types:** `feat`, `fix`, `refactor`, `docs`, `test`, `chore`, `style`, `perf`, `ci`, `build`, `revert`

The `commit-msg` hook auto-appends `Refs: #N` when on an issue branch.

### Install Git Hooks
```bash
./scripts/setup-hooks.sh
```

## Managing Issues

### List all open issues
```bash
curl http://localhost:5000/api/issues?status=open
```

### Close an issue
```bash
curl -X PUT http://localhost:5000/api/issues/{id} \
  -H "Content-Type: application/json" \
  -d '{"status": "closed"}'
```

## Issue Statuses

| Status | Meaning |
|--------|---------|
| `open` | New, not started |
| `in_progress` | Being worked on |
| `closed` | Completed |
| `deleted` | Soft-deleted |
