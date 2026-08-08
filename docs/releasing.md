# Release Process

Releases are intentionally manual and owner-controlled.

## One-time GitHub setup

1. Create a GitHub environment named `nuget`.
2. Add the repository secret `NUGET_API_KEY` to that environment.
3. Configure Shivansh Gaur as a required reviewer for the environment.
4. Protect `main` and require the CI, CodeQL, and dependency-review checks before merge.

The workflow also requires an explicit `publish_to_nuget` confirmation, so validation-only runs cannot publish packages even when the environment is misconfigured.

## Prepare a release

1. Move the relevant entries from `[Unreleased]` in `CHANGELOG.md` to a version heading with the release date.
2. Confirm that the three package IDs are owned by the intended NuGet account.
3. Run the Release build, full test suite, package verifier, and vulnerability audit locally.
4. Merge the release commit into protected `main` and wait for all required checks to pass.
5. Create an annotated `vMAJOR.MINOR.PATCH` tag from that exact `main` commit and push the tag to GitHub.
6. Run the **Release packages** workflow from `main`, enter the tag's exact `MAJOR.MINOR.PATCH` version, and leave publication disabled for a validation-only rehearsal. The workflow checks out the existing tag and rejects tags that are not on `main`.
7. Review the generated artifacts.
8. Run the workflow again with `publish_to_nuget` enabled and approve the `nuget` environment deployment.

The publishing job pushes all three packages, creates the matching `vMAJOR.MINOR.PATCH` GitHub release, attaches package artifacts, and uses the version section from `CHANGELOG.md` as release notes.

Package publication is immutable. Do not reuse a version after it has been pushed to NuGet.
