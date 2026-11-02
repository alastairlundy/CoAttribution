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
