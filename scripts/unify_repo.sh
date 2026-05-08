#!/usr/bin/env bash
#
# Unify the project: keep this Unity project as the root, point its git remote
# at the existing denizjafari/UpperBodyTracking GitHub repo, push.
#
# Run from inside UpperLimbRehabilitation/:
#   bash scripts/unify_repo.sh                # DRY RUN — prints plan
#   bash scripts/unify_repo.sh --apply        # do it (force-push, default)
#   bash scripts/unify_repo.sh --apply --merge  # safer: pull-merge instead
#
# The Unity project + my scaffolding are already pre-staged in this folder
# (Assets/Scripts/, Assets/MiniGames/, Assets/StreamingAssets/, docs/, src/, etc.).
# This script only handles the git remote dance.

set -euo pipefail

APPLY=0
MODE="force"   # "force" (default) or "merge"
REMOTE_URL="git@github.com:denizjafari/UpperBodyTracking.git"

for a in "$@"; do
  case "$a" in
    --apply)  APPLY=1 ;;
    --merge)  MODE="merge" ;;
    --force)  MODE="force" ;;
    --remote=*) REMOTE_URL="${a#--remote=}" ;;
    -h|--help)
      sed -n '1,/^set -e/p' "$0" | sed -n '2,/^$/p'
      exit 0 ;;
    *) echo "Unknown arg: $a"; exit 1 ;;
  esac
done

REPO_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "${REPO_ROOT}"

# --------- preflight ---------
echo "============================================================"
echo "Unify repo plan"
echo "============================================================"
echo "  repo root:   ${REPO_ROOT}"
echo "  remote URL:  ${REMOTE_URL}"
echo "  mode:        ${MODE}"
echo "  apply:       $([ ${APPLY} = 1 ] && echo YES || echo no — DRY RUN)"
echo ""

# Sanity: this is a Unity project
[ -d "Assets" ] && [ -d "Packages" ] && [ -d "ProjectSettings" ] || {
  echo "Error: not a Unity project root. Expected Assets/ Packages/ ProjectSettings/."
  exit 1
}

# Sanity: scaffolding is staged
for d in Assets/Scripts Assets/MiniGames Assets/StreamingAssets docs src; do
  if [ ! -d "$d" ]; then
    echo "Error: $d/ missing — pre-staging step didn't complete. Re-run the file copy."
    exit 1
  fi
done

# Show what git sees
echo "Current git state:"
if [ -d .git ]; then
  echo "  branch:        $(git branch --show-current 2>/dev/null || echo '(detached)')"
  echo "  current remote:$(git remote get-url origin 2>/dev/null || echo '(none)')"
  echo "  HEAD:          $(git log --oneline -1 2>/dev/null || echo '(no commits)')"
else
  echo "  not a git repo — will run 'git init'"
fi
echo ""

if [ ${APPLY} != 1 ]; then
  echo "Plan:"
  if [ ! -d .git ]; then
    echo "  1. git init -b main"
  fi
  echo "  2. set remote 'origin' to ${REMOTE_URL}"
  if [ "${MODE}" = "merge" ]; then
    echo "  3. fetch + merge origin/main with --allow-unrelated-histories"
    echo "     (you may need to resolve duplicate Scripts/ paths manually)"
  else
    echo "  3. force-push the unified state to origin/main"
    echo "     ⚠ destroys the existing GitHub history (only commits f9164be + be67025"
    echo "       which are old layouts — content survives in UpperBodyTracking/ locally)"
  fi
  echo "  4. stage all + commit"
  echo "  5. push"
  echo ""
  echo "DRY RUN. Re-run with --apply to perform."
  exit 0
fi

# --------- 1. ensure git repo ---------
if [ ! -d .git ]; then
  git init -b main
fi

# --------- 2. clear stale lock + set remote ---------
[ -f .git/index.lock ] && rm -f .git/index.lock || true
git remote remove origin 2>/dev/null || true
git remote add origin "${REMOTE_URL}"
echo "Remote set: $(git remote get-url origin)"

# --------- 3. integrate with remote ---------
case "${MODE}" in
  merge)
    git fetch origin || true
    if git rev-parse --verify --quiet origin/main >/dev/null; then
      git merge --allow-unrelated-histories -X ours origin/main \
        || echo "Merge had conflicts — resolve, then 'git add -A && git commit && git push'"
    fi
    ;;
  force)
    # We'll force-push at step 5.
    ;;
esac

# --------- 4. stage + commit ---------
git add -A

if git diff --cached --quiet; then
  echo "Nothing to commit."
else
  git commit -m "Unify repo: Unity project + IMU pipeline + scaffolding under one tree

Folder layout (root = Unity project):
- Assets/Scripts/             — manager + input + avatar + feedback layers
- Assets/MiniGames/           — per-game template + Garden Meditation
- Assets/StreamingAssets/     — runtime config JSON
- Assets/{Settings,Resources,Plugins,Oculus,XR,HintsStarsLite,...}
                              — existing Unity project (Meta XR SDK 201, URP)
- Packages/                   — Meta XR + Unity package pin
- ProjectSettings/            — Unity 6000.4.4f1 + Quest 3S build config
- docs/                       — phase-by-phase implementation guides
- src/                        — Python (Raspberry Pi IMU pipeline)
- scripts/                    — commit + unify automation

Phases delivered: 0 (foundation), 1 (managers), 2 (input), 3 (avatar),
4 (feedback), 5 (welcome+prefs), 6 (calib A), 7 (calib B/ROM),
8 (session setup), 9.0 (per-game template), 9.5 (Garden Meditation)."
fi

# --------- 5. push ---------
case "${MODE}" in
  force)
    echo ""
    echo "Force-pushing to ${REMOTE_URL} ..."
    git push --force --set-upstream origin main
    ;;
  merge)
    echo ""
    git push --set-upstream origin main || {
      echo "Push rejected. Either rebase, fix conflicts, or re-run with --force."
      exit 1
    }
    ;;
esac

echo ""
echo "Done. Repository now mirrors GitHub remote ${REMOTE_URL}."
echo ""
echo "Next:"
echo "  - Open this folder in Unity Hub. First run rebuilds Library/."
echo "  - Optional: rename the GitHub repo on github.com/denizjafari/UpperBodyTracking"
echo "    to anything you prefer (UpperLimbRehabilitation, VRRehab, …) — git auto-redirects."
echo "  - When you're confident, you can archive UpperBodyTracking/ on disk."
