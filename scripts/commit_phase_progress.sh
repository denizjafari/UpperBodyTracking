#!/usr/bin/env bash
# Commit + push the current Phase 0–9.5 scaffolding to GitHub.
# Run this from the repo root on your local machine:
#   bash scripts/commit_phase_progress.sh
#
# Idempotent: if there's nothing to commit, it just pushes whatever's there.

set -euo pipefail

cd "$(dirname "$0")/.."

# Drop any stale index lock that may have come from a crashed editor.
[ -f .git/index.lock ] && rm -f .git/index.lock

# Stage the things we care about. Existing files (README, src/, the .docx)
# are left alone — add them yourself if you want them in this commit.
git add \
  .gitignore \
  IMPLEMENTATION_PLAN.md \
  STATUS.md \
  unity/ \
  scripts/

if git diff --cached --quiet; then
  echo "Nothing new to commit. Pushing whatever's local..."
else
  git commit -m "Phase 0–9.5 scaffolding: managers, calibration, session setup, Garden Meditation

- Phase 0: Unity Editor checklist (manual)
- Phase 1: Core scene + 11 managers + service locator + SceneEntry contract
- Phase 2: UDP/Controller/Simulator input adapters with timeout detection
- Phase 3: Humanoid avatar driver with mirror + demo-clip hook
- Phase 4: Feedback channels (text/audio/avatar-highlight/haptic) + severity tiers
- Phase 5: Welcome + User Preferences scenes
- Phase 6: IMU bias calibration (Calibration A)
- Phase 7: ROM calibration (Calibration B) — four-phase per-joint state machine
- Phase 8: Session setup with availability gating
- Phase 9.0: Per-game module template (MiniGameSceneEntry + MiniGameRouter)
- Phase 9.5: Garden Meditation — first complete mini-game

50 .cs files + 3 JSON configs + 11 docs.
Pinned to Unity 6.2 + Meta XR SDK v85, Quest 3S target."
fi

git push origin "$(git branch --show-current)"
