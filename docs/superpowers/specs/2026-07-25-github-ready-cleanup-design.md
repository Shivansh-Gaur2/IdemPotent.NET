# GitHub-ready repository cleanup

## Goal

Prepare the repository for its initial GitHub push while keeping only intentional project files under version control.

## Scope

- Ignore Visual Studio state and .NET build artifacts.
- Remove generated `.vs`, `bin`, and `obj` content from the working tree.
- Add a concise README describing the project, requirements, build command, and license.
- Commit the resulting repository as the initial commit.

## Exclusions

No application behavior, project configuration, CI workflow, package metadata, or source code will be changed.

## Validation

The solution will be built after cleanup, and Git status will be checked to confirm generated artifacts are ignored and only intentional files are staged.
