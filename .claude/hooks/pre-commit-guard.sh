#!/usr/bin/env bash
# Pre-commit/stash/push guard for ANcpLua.Analyzers
# Blocks git operations if documentation is out of sync.
# Exit 0 = allow, Exit 2 = block (message shown to Claude)
set -euo pipefail

# Read tool input from stdin
INPUT=$(cat)
TOOL_NAME=$(echo "$INPUT" | python3 -c "import sys,json; print(json.load(sys.stdin).get('tool_name',''))" 2>/dev/null || echo "")
COMMAND=$(echo "$INPUT" | python3 -c "import sys,json; print(json.load(sys.stdin).get('tool_input',{}).get('command',''))" 2>/dev/null || echo "")

# Only intercept Bash tool
[ "$TOOL_NAME" != "Bash" ] && exit 0

# Only intercept git commit, stash, push
if ! echo "$COMMAND" | grep -qE 'git\s+(commit|stash|push)'; then
    exit 0
fi

REPO_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
VIOLATIONS=()
BYPASS_ATTEMPT=false

# --- CHECK 1: CHANGELOG.md exists ---
if [ ! -f "$REPO_ROOT/CHANGELOG.md" ]; then
    VIOLATIONS+=("CHANGELOG.md does not exist. Create it before committing.")
fi

# --- CHECK 2: CHANGELOG.md was modified in this session ---
if [ -f "$REPO_ROOT/CHANGELOG.md" ]; then
    CHANGELOG_CHANGED=$(cd "$REPO_ROOT" && git diff --name-only HEAD -- CHANGELOG.md 2>/dev/null; git diff --cached --name-only -- CHANGELOG.md 2>/dev/null)
    OTHER_CHANGES=$(cd "$REPO_ROOT" && git diff --cached --name-only 2>/dev/null | grep -v CHANGELOG.md || true)
    if [ -n "$OTHER_CHANGES" ] && [ -z "$CHANGELOG_CHANGED" ]; then
        VIOLATIONS+=("CHANGELOG.md was NOT updated but other files are staged. You MUST document ALL changes in CHANGELOG.md before committing — including changes made by humans, not just your own.")
    fi
fi

# --- CHECK 3: Diagnostic count sync (README vs AnalyzerReleases vs csproj) ---
UNSHIPPED="$REPO_ROOT/src/ANcpLua.Analyzers/AnalyzerReleases.Unshipped.md"
README="$REPO_ROOT/README.md"
CSPROJ="$REPO_ROOT/src/ANcpLua.Analyzers/ANcpLua.Analyzers.csproj"

if [ -f "$UNSHIPPED" ] && [ -f "$README" ] && [ -f "$CSPROJ" ]; then
    # Count actual diagnostics from source of truth
    ACTUAL_COUNT=$(grep -cE '^AL[0-9]{4}' "$UNSHIPPED" 2>/dev/null || echo "0")

    # Extract counts using python3 (macOS grep lacks -P)
    README_COUNT=$(python3 -c "
import re
with open('$README') as f:
    m = re.search(r'\*\*(\d+) diagnostics\*\*', f.read())
    print(m.group(1) if m else '0')
" 2>/dev/null || echo "0")

    CSPROJ_COUNT=$(python3 -c "
import re
with open('$CSPROJ') as f:
    m = re.search(r'<Description>(\d+) Roslyn', f.read())
    print(m.group(1) if m else '0')
" 2>/dev/null || echo "0")

    if [ "$ACTUAL_COUNT" != "$README_COUNT" ]; then
        VIOLATIONS+=("README.md says $README_COUNT diagnostics but AnalyzerReleases.Unshipped.md has $ACTUAL_COUNT. Update README.md.")
    fi
    if [ "$ACTUAL_COUNT" != "$CSPROJ_COUNT" ]; then
        VIOLATIONS+=("csproj Description says $CSPROJ_COUNT diagnostics but AnalyzerReleases.Unshipped.md has $ACTUAL_COUNT. Update csproj Description.")
    fi

    # Code fix count sync
    ACTUAL_FIXES=$(find "$REPO_ROOT/src/ANcpLua.Analyzers.CodeFixes/CodeFixes/" -name "AL0*CodeFixProvider.cs" 2>/dev/null | wc -l | tr -d ' ')

    README_FIXES=$(python3 -c "
import re
with open('$README') as f:
    m = re.search(r'\*\*(\d+) automatic code fixes\*\*', f.read())
    print(m.group(1) if m else '0')
" 2>/dev/null || echo "0")

    CSPROJ_FIXES=$(python3 -c "
import re
with open('$CSPROJ') as f:
    m = re.search(r'Includes (\d+) automatic', f.read())
    print(m.group(1) if m else '0')
" 2>/dev/null || echo "0")

    if [ "$ACTUAL_FIXES" != "$README_FIXES" ]; then
        VIOLATIONS+=("README.md says $README_FIXES code fixes but $ACTUAL_FIXES exist. Update README.md.")
    fi
    if [ "$ACTUAL_FIXES" != "$CSPROJ_FIXES" ]; then
        VIOLATIONS+=("csproj Description says $CSPROJ_FIXES code fixes but $ACTUAL_FIXES exist. Update csproj Description.")
    fi

    # README table row count must match actual diagnostic count
    README_ROWS=$(grep -cE '^\| \[AL[0-9]{4}\]' "$README" 2>/dev/null || echo "0")
    if [ "$ACTUAL_COUNT" != "$README_ROWS" ]; then
        VIOLATIONS+=("README.md table has $README_ROWS rows but $ACTUAL_COUNT diagnostics exist. Add missing rows to the table.")
    fi
fi

# --- CHECK 4: CLAUDE.md line count ≤ 500 ---
CLAUDE_MD="$REPO_ROOT/CLAUDE.md"
if [ -f "$CLAUDE_MD" ]; then
    CLAUDE_LINES=$(wc -l < "$CLAUDE_MD" | tr -d ' ')
    if [ "$CLAUDE_LINES" -gt 500 ]; then
        VIOLATIONS+=("CLAUDE.md is $CLAUDE_LINES lines (max 500). STOP IMMEDIATELY. Do NOT continue — ask the human to trim it.")
        BYPASS_ATTEMPT=true
    fi
fi

# --- CHECK 5: Memory files line count ≤ 500 ---
MEMORY_DIR="$HOME/.claude/projects/-Users-ancplua-ANcpLua-Analyzers/memory"
if [ -d "$MEMORY_DIR" ]; then
    for memfile in "$MEMORY_DIR"/*.md; do
        [ -f "$memfile" ] || continue
        MEMLINES=$(wc -l < "$memfile" | tr -d ' ')
        if [ "$MEMLINES" -gt 500 ]; then
            VIOLATIONS+=("Memory file $(basename "$memfile") is $MEMLINES lines (max 500). STOP IMMEDIATELY. Do NOT continue — ask the human to trim it.")
            BYPASS_ATTEMPT=true
        fi
    done
fi

# --- VERDICT ---
if [ ${#VIOLATIONS[@]} -gt 0 ]; then
    {
        echo ""
        echo "============================================================"
        echo "  COMMIT BLOCKED — DOCUMENTATION OUT OF SYNC"
        echo "============================================================"
        echo ""
        echo "You MUST fix ALL of the following before committing:"
        echo ""
        for v in "${VIOLATIONS[@]}"; do
            echo "  ✗ $v"
        done
        echo ""
        echo "MANDATORY ACTIONS (non-negotiable):"
        echo "  1. Fix every violation listed above"
        echo "  2. Stage the fixed files"
        echo "  3. Then retry the commit"
        echo ""
        echo "PROHIBITED:"
        echo "  - Do NOT use --no-verify"
        echo "  - Do NOT bypass, skip, or work around this check"
        echo "  - Do NOT commit without fixing ALL violations"
        echo "============================================================"
    } >&2

    if [ "$BYPASS_ATTEMPT" = true ]; then
        echo "" >&2
        echo "HARD STOP: Line count limit exceeded." >&2
        echo "You are NOT allowed to continue. Ask the user for help NOW." >&2
        # Signal the parent process to stop if bypass is attempted again
        echo "If you see this message a second time, the process will be terminated." >&2
    fi

    exit 2
fi

exit 0
