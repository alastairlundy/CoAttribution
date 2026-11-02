# TODO

## 1. Add `--amend` and git flag passthrough to `commit`

**What:** `co-attr commit` currently only produces plain `git commit -m ... --trailer ...`. Add an `--amend` flag (fixing the last commit is the most common missed-attribution workflow) and a `--` passthrough so callers can forward arbitrary git commit flags (`-a`, `--no-verify`, `--signoff`, `--allow-empty`, `--author`, `--no-gpg-sign`, ...).

**Why:** CoAttribution's job is appending trailers to commits, but it wraps the narrowest slice of `git commit` usage. Today the "I committed and forgot an agent" fix requires manual `git commit --amend` trailer editing, which defeats the tool's purpose. Passthrough keeps the tool a wrapper rather than a replacement, so real commit workflows (hooks, GPG, staging) keep working through it.
