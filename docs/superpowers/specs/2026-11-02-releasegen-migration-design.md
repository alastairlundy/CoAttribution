# ReleaseGen Migration Design

**Date:** 2026-11-02
**Status:** Approved decisions recorded from brainstorming (see Approaches)

## Intent and why

Replace dotnet-releaser with ReleaseGen (`AlastairLundy/ReleaseGen`) as the repo's release
tooling. dotnet-releaser's `publish` command did build, tool packaging, GitHub release
creation, and NuGet push in one command; ReleaseGen deliberately handles only
build → test → package → publish-to-NuGet, so the GitHub release side moves to the
workflow (or the maintainer) and the pipeline becomes manifest-driven.

## Goals

- One strict `releasegen.json` at the repo root drives build, test, package, and NuGet publish.
- Release archives (per-RID) are attached to the maintainer-created GitHub Release.
- NuGet publishing in CI uses trusted publishing (short-lived OIDC-exchanged key).
- Publish is gated on the test stage across the job boundary.

## Non-goals

- No changelog generation (unchanged from today: dotnet-releaser's changelog publishing
  was already off).
- No automation of GitHub Release creation — a maintainer creates the release and tag.
- No changes to packaging metadata (`PackAsTool`, `PackageId`, versions, etc.).

## Constraints

- ReleaseGen requires the .NET 10 SDK — already pinned via `global.json`.
- Strict manifest: unknown fields fail parse; secrets never live in the manifest.
- Stage handoff contract: stages read/write artifacts under `<invocation dir>/release/`;
  moving stages across jobs requires moving the `release/` tree intact.
- Manifest discovery walks up from the current directory, so stages run from the repo root.

## Approaches

1. **Two-job trusted publishing + release-triggered workflow (chosen).** Job 1 runs
   `releasegen build`, `releasegen test`, `releasegen package` and uploads the `release/`
   tree as a workflow artifact; job 2 downloads it, exchanges GitHub OIDC for a nuget.org
   key via `NuGet/login`, runs `releasegen publish`, then attaches the archives to the
   live GitHub Release with `gh release upload`. Matches the maintainer's release flow
   (manual Release creation, workflow triggered on `release: published`) and ReleaseGen's
   documented trusted-publishing pattern.
2. **Single job with a long-lived `NUGET_API_KEY` secret.** Simpler workflow, but rejected
   per the maintainer's answer: trusted publishing was chosen over the API-key path.
3. **Keep dotnet-releaser, add ReleaseGen beside it.** Rejected: two release tools doing
   overlapping work is the drift this migration removes.

## Decided (by the maintainer)

- **NuGet publish:** Trusted publishing, two CI jobs.
- **GitHub Release:** created manually by a maintainer; publishing the tag triggers the
  workflow, which publishes to NuGet then modifies the Release to add the per-RID archives.
- **Build content:** `aot` preset, six RIDs (`win-x64`, `win-arm64`, `linux-x64`,
  `linux-arm64`, `osx-x64`, `osx-arm64`), archives default to zip + tar.gz for the
  Unix-based RIDs and zip for the Windows-class RIDs.

## Parts of the how, as decided

- Manifest `targets`: one target — `src/CoAttribution.Cli/CoAttribution.Cli.csproj` with
  kinds `["executables", "tool-package"]` (the csproj already sets `PackAsTool=true`;
  `CoAttribution.Lib` is `IsPackable=false` so it is not a target).
- Manifest `test.projects`: the two test projects (`CoAttribution.Cli.Tests`,
  `CoAttribution.Lib.Tests`), arming the test gate.
- Manifest `publish.sources`: `[]` (nuget.org default).
- Trigger: `on: release: types: [published]`; the tag comes from
  `github.event.release.tag_name`. The old `workflow_dispatch` tag logic (infer tag from
  the csproj `Version`, create the git tag in CI) goes away because the maintainer now
  creates the tag by publishing the Release.
- Job 2 re-runs the test stage via the publish gate's cross-job consultation per the
  stage handoff contract; tests running twice is expected behavior.
- `NuGet/login@v1` (pinned) supplies the short-lived key through
  `NUGET_API_KEY`; no API key secret is needed.
- `gh release upload "$TAG" release/*.zip release/*.tar.gz` attaches archives; only the
  archives, not the per-RID folders or the nupkgs, go to the Release.

## Left to the builder

- Exact `NuGet/login` version pin and any pinned-hash conventions the repo uses.
- NuGet-side prerequisite (outside the repo): the CoAttribution package owner must
  register this repo as a trusted publisher on nuget.org; the workflow assumes it exists.
- Cleanup details: remove `src/dotnet-releaser.toml`, replace the
  `artifacts-dotnet-releaser/` .gitignore entry with `release/`, and verify the csproj's
  dead `PublishAot Condition="'$(DotNetPublish)' == 'true'"` property stays inert under
  the preset-emitted switch (leave it untouched).
- First-run verification: a `--dry-run` of the build stage locally before trusting CI,
  to bury Terminal.Gui-meets-NativeAOT surprises early.

## Amendment 1 (2026-11-02): rid-subset override + three-OS build matrix

**Trigger:** the first real build proved NativeAOT is same-OS-only (`Cross-OS native
compilation is not supported` for `linux-x64` on a Windows host), so no single runner can
build all six RIDs with the `aot` preset, and ReleaseGen 0.2.0 offers no per-invocation
rid subset. The maintainer chose to fix it upstream: ExtensionGen gains a rid-subset
override, and the workflow splits the build across three runner OSes.

**Upstream (AlastairLundy/ReleaseGen, next feature release, 0.3.0):**

- CLI surface: `releasegen build --rids <comma-separated list>` — a per-invocation subset
  of the manifest's `runtimeIdentifiers`. `build` only (the matrix need); `run`, `test`,
  `package`, `publish` unchanged. Like `--preset`, the flag never writes the manifest; the
  manifest remains the declared source of truth.
- Validation (refused in `BuildCommand`, mirroring the unknown-preset refusal, exit 1,
  before any planning): empty list, blank entry, deduplicated entries checked
  case-insensitively against the manifest's configured rids; a value outside the manifest's
  set fails naming it and the known set. The manifest's spelling is used downstream so
  output paths stay deterministic.
- Implementation seam: new subset field on `BuildStageRequest`; `BuildStageRunner` iterates
  the subset when set instead of `manifest.Build.RuntimeIdentifiers` (summary line reports
  the subset count; "runtime identifier(s)" wording unchanged).
- Tests (TUnit, following `CliHarness`/`TestWorkspace` conventions): subset run
  publishes/archives only the named rids; refusal of unknown/blank/empty values with the
  error naming the manifest set; `--verbose`/`--dry-run`/`--quiet` compose.
- Docs: `docs/commands.md` build section + a matrix note in `docs/trusted-publishing.md`
  and `docs/stage-handoff.md`; `CHANGELOG.md` entry; csproj `<Version>` bumped to 0.3.0.
  AI assistance disclosed in the commit/PR per ReleaseGen's CONTRIBUTING.

**CoAttribution side (this repo):**
- Manifest unchanged: six RIDs, `aot` preset — the subset lives at the invocation.
- `release.yml` build job becomes a matrix: `win-x64,win-arm64` on `windows-latest`,
  `linux-x64,linux-arm64` on `ubuntu-latest`, `osx-x64,osx-arm64` on `macos-latest`; each
  leg uploads its `release/` tree as `release-bin-<os>`; the publish job downloads all
  three with `merge-multiple: true` into one `release/` tree (archive names are
  (project, rid)-unique, so legs cannot collide), runs the package stage there, then
  `releasegen publish` and `gh release upload`.
- ReleaseGen is installed unpinned until the 0.3.0 release is on nuget.org, then pinned;
  until the pin, a Leg job fails loudly if the installed tool lacks `--rids` (unknown
  option exits nonzero), which is the honest gate rather than a silent downward-compatible
  drift.

**Approaches considered for the subset seam:** a manifest-level per-rid assignment key
(rejected: the manifest is the release's declared source of truth and the subset is an
invocation escape, like `--preset`); repeatable/space-separated `--rids` arguments
(rejected: comma-separated single value parses identically across CI shells and avoids
quoting differences); three OS-scoped manifests (rejected: ReleaseGen v1 mandates one
manifest per repository).

