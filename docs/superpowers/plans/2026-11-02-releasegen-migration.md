# ReleaseGen Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace dotnet-releaser with `ReleaseGen` (`AlastairLundy/ReleaseGen`) as CoAttribution's release pipeline: manifest-driven build/test/package/publish, NuGet trusted publishing in CI, and archives attached to a maintainer-created GitHub Release.

**Architecture:** A strict `releasegen.json` at the repo root declares one target (the CLI, `executables` + `tool-package` kinds), the test gate, and the AOT build preset over six RIDs. `.github/workflows/release.yml` is rewritten to two jobs: build→test→package (uploading the `release/` tree) and publish (trusted-publishing OIDC exchange, then `gh release upload` of archives onto the Release that triggered the workflow).

**Tech Stack:** ReleaseGen CLI (dotnet tool, .NET 10 SDK — already pinned via `global.json`), GitHub Actions (`NuGet/login@v1`, `gh`), strict JSON manifest.

**Spec:** `docs/superpowers/specs/2026-11-02-releasegen-migration-design.md`

## Global Constraints

- Manifest is strict JSON: unknown fields fail parse; no comments; no secrets in the manifest.
- ReleaseGen requires the .NET 10 SDK; runs discover `releasegen.json` by walking up from the current directory, so all stages run from the repo root.
- Stage order is build → test → package → publish; artifacts under `<invocation dir>/release/` (`release/<rid>/<project-name>/` folders for binaries, `release/<project-name>-<rid>.zip` / `.tar.gz` archives beside them, nupkgs under `release/packages/…`).
- Preset is `aot`; runtimeIdentifiers are exactly `["win-x64", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64"]`.
- Workflow trigger is `release: types: [published]` only; no `workflow_dispatch` tag inference.
- No tar.xz; archives rely on defaults (zip for Windows-class RIDs, zip + tar.gz for Unix-based RIDs).

## Review Focus

Sorted by likelihood of biting a person; each gets the step that pins it.

1. **Terminal.Gui failing NativeAOT compilation** — first real AOT publish of the CLI. Behavior expected: a working `releasegen build`. Pinned in Task 4's real build step.
2. **`NUGET_USER` secret missing** on the repo's first trusted-publishing run — publish job fails at the `NuGet/login` step. Expected: maintainer adds the secret (their nuget.org username — not an API key) before first release. Pinned by Task 2's YAML containing `user: ${{ secrets.NUGET_USER }}` and a note step.
3. **Workflow never triggers, or runs without OIDC rights** — release published but `id-token: write` / `permissions` omitted. Expected: trigger on release publish with correct permissions. Pinned by Task 2's YAML verification step.
4. **Archives not attached to the Release when the workflow re-runs** — stale/conflicting assets. Expected: `gh release upload` with `--clobber` against `github.event.release.tag_name`. Pinned by Task 2's YAML content.

---

### Task 1: Create the `releasegen.json` manifest

**Files:**
- Create: `releasegen.json` (repo root)

**Interfaces:**
- Consumes: csproj properties `PackAsTool=true` and `OutputType=Exe` in `src/CoAttribution.Cli/CoAttribution.Cli.csproj` (ReleaseGen reads them expanded to validate the declared kinds).
- Produces: `releasegen.json` consumed by the workflow in Task 2 (paths must match the manifest exactly).

- [ ] **Step 1: Write the manifest**

```json
{
  "targets": [
    {
      "project": "src/CoAttribution.Cli/CoAttribution.Cli.csproj",
      "kinds": ["executables", "tool-package"]
    }
  ],
  "build": {
    "preset": "aot",
    "runtimeIdentifiers": ["win-x64", "win-arm64", "linux-x64", "linux-arm64", "osx-x64", "osx-arm64"]
  },
  "test": {
    "projects": [
      "tests/CoAttribution.Cli.Tests/CoAttribution.Cli.Tests.csproj",
      "tests/CoAttribution.Lib.Tests/CoAttribution.Lib.Tests.csproj"
    ]
  },
  "publish": {
    "sources": []
  }
}
```

Why no `archiveFormats` key: absent means the per-rid-class defaults (zip for `win-*`, zip + tar.gz for Unix-based), which is exactly the decided set — no explicit key needed.

- [ ] **Step 2: Install the tool and validate the manifest**

Run:
```bash
dotnet tool install -g ReleaseGen
releasegen build --dry-run
```
Expected: manifest parses, passes kind cross-checks (`PackAsTool` true for `tool-package`, executable for `executables`), and prints the build plan for six RIDs without executing. Failures here (unknown rid, kind mismatch) are manifest bugs to fix before continuing.

- [ ] **Step 3: Commit**

```bash
git add releasegen.json
git commit -m "Add ReleaseGen manifest"
```

### Task 2: Rewrite the release workflow

**Files:**
- Modify: `.github/workflows/release.yml` (replace entire contents)

**Interfaces:**
- Consumes: `releasegen.json` (Task 1) and its artifact paths: `release/` tree from the package stage.
- Produces: workflow triggered by a maintainer-published Release; job named `release` uploading artifact `release-tree`; job named `publish` consuming it.

- [ ] **Step 1: Replace `release.yml` with the two-job workflow**

```yaml
name: Release

on:
  release:
    types: [published]

permissions:
  contents: read

jobs:
  release:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v7

      - name: Setup .NET SDK
        uses: actions/setup-dotnet@v6
        with:
          global-json-file: global.json

      - name: Cache NuGet packages
        uses: actions/cache@v6
        with:
          path: ~/.nuget/packages
          key: nuget-${{ runner.os }}-${{ hashFiles('**/Directory.Packages.props', '**/*.csproj') }}
          restore-keys: |
            nuget-${{ runner.os }}-

      - name: Install ReleaseGen
        run: dotnet tool install --global ReleaseGen

      - name: Build
        run: releasegen build

      - name: Test
        run: releasegen test

      - name: Package
        run: releasegen package

      - name: Upload release tree
        uses: actions/upload-artifact@v4
        with:
          name: release-tree
          path: release/
          if-no-files-found: error

  publish:
    runs-on: ubuntu-latest
    needs: release
    permissions:
      contents: write
      id-token: write
    steps:
      - uses: actions/checkout@v7
        with:
          ref: ${{ github.event.release.target_commitish }}

      - name: Setup .NET SDK
        uses: actions/setup-dotnet@v6
        with:
          global-json-file: global.json

      - name: Cache NuGet packages
        uses: actions/cache@v6
        with:
          path: ~/.nuget/packages
          key: nuget-${{ runner.os }}-${{ hashFiles('**/Directory.Packages.props', '**/*.csproj') }}
          restore-keys: |
            nuget-${{ runner.os }}-

      - name: Install ReleaseGen
        run: dotnet tool install --global ReleaseGen

      - name: Download release tree
        uses: actions/download-artifact@v4
        with:
          name: release-tree
          path: release

      - name: NuGet login (OIDC -> temporary API key)
        uses: NuGet/login@8d196754b4036150537f80ac539e15c2f1028841 # v1.2.0
        id: login
        with:
          user: ${{ secrets.NUGET_USER }}

      - name: Publish packages to NuGet.org
        env:
          NUGET_API_KEY: ${{ steps.login.outputs.NUGET_API_KEY }}
        run: releasegen publish

      - name: Attach per-RID archives to the release
        env:
          GH_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        run: gh release upload "${{ github.event.release.tag_name }}" release/*.zip release/*.tar.gz --clobber
```

Notes:
- The `publish` job re-runs the test stage via ReleaseGen's cross-job test-gate consultation whenever the manifest declares a test section — tests intentionally run in both jobs; that is the documented gate behavior, not a bug.
- Deliberately **not** copied from ReleaseGen's own repo: the `environment: production` block (avoids requiring a new GitHub environment; nuget.org trusted-publisher enrollment does not need one).
- The maintainer's one-time prerequisite: repo secret `NUGET_USER` = their nuget.org account username; trusted-publisher enrollment for this repo is already in place (per the design review).
- `GITHUB_TOKEN` with `contents: write` is authoritative enough to upload assets to an already-created Release owned by a maintainer.

- [ ] **Step 2: Lint the workflow YAML**

Run:
```bash
npx -y js-yaml .github/workflows/release.yml
```
Expected: prints the parsed workflow as JSON with no error (valid YAML — CRLF/LF does not matter here).

- [ ] **Step 3: Review-check against the trigger and permissions**

Run:
```bash
grep -n "id-token: write\|release:\|types: \[published\]\|NUGET_USER\|--clobber" .github/workflows/release.yml
```
Expected: all five present — this pins Review Focus lines 3 and 4.

- [ ] **Step 4: Commit**

```bash
git add .github/workflows/release.yml
git commit -m "Switch release workflow from dotnet-releaser to ReleaseGen with trusted publishing"
```

### Task 3: Remove dotnet-releaser remnants

**Files:**
- Delete: `src/dotnet-releaser.toml`
- Modify: `.gitignore` — remove the two lines `# dotnet-releaser artifacts` and `artifacts-dotnet-releaser/`, add `# ReleaseGen artifacts` and `release/` in the same area

**Interfaces:**
- Consumes: nothing.
- Produces: a repo with no dotnet-releaser references and a `.gitignore` matching ReleaseGen's output directory (`release/`).

- [ ] **Step 1: Make the edits (delete the toml; swap the ignore entry)**

- [ ] **Step 2: Verify no references remain**

Run:
```bash
git grep -il "dotnet-releaser" -- . ':!docs/superpowers/*'
```
Expected: no output.

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "Remove dotnet-releaser configuration"
```

### Task 4: Local end-to-end verification (no publish)

**Files:**
- None (verification only)

**Interfaces:**
- Consumes: manifest from Task 1; the local `ReleaseGen` tool from Task 1 Step 2.
- Produces: evidence that the pipeline is healthy before the next real release uses it in CI.

- [ ] **Step 1: Real build and pack locally**

Run from the repo root:
```bash
releasegen build && releasegen test && releasegen package
```
Expected: six per-RID AOT publish directories under `release/`, zip archives beside each `win-*` folder plus a `.tar.gz` beside each Unix-based one, test stage exit 0, nupkgs under `release/packages/tool/`. This is where a Terminal.Gui-under-NativeAOT failure shows up (Review Focus line 1); if the six-RID matrix is too slow locally, the minimum acceptable proof is one RID AOT-compiling clean:
```bash
dotnet publish src/CoAttribution.Cli/CoAttribution.Cli.csproj -c Release -r win-x64 -p:PublishAot=true
```

- [ ] **Step 2: Probe publish without credentials (expected to fail only on auth, not on artifacts)**

Run:
```bash
releasegen publish --dry-run
```
Expected: lists the nupkgs it would push (`release/packages/tool/CoAttribution.Cli/*.nupkg`); exits successfully with no network call. A failure naming missing packages means the Task 3 `.gitignore`/manifest artifact layout is wrong. If the dry-run insists on reading the key channel before showing the plan, set `NUGET_API_KEY` to a dummy value for this step only — nothing is pushed.
