# IdemShield Repository and Profile Design

## Goal

Present IdemShield as a credible, actively maintained .NET package family that developers can evaluate, install, and operate with confidence. The repository and maintainer profile must use verifiable evidence rather than unsubstantiated adoption or reliability claims.

## Public Identity

The project will be renamed from IdemPotent.NET to **IdemShield.NET** and remain under Shivansh Gaur's existing GitHub account. A separate organization will not be created until the project has multiple maintainers or a broader product family.

The public package family will be:

- `IdemShield.AspNetCore` for middleware, configuration, and storage abstractions.
- `IdemShield.Redis` for the Redis storage provider.
- `IdemShield.SqlServer` for the SQL Server provider and expiry cleanup worker.

The repository, solution, projects, assemblies, namespaces, tests, examples, and documentation will consistently use the IdemShield name. The initial public version remains `0.1.0` while the API and operational behavior are still being stabilized.

The repository description will use this positioning:

> Production-grade idempotency middleware for ASP.NET Core. Safely retry API requests using Redis or SQL Server without duplicate side effects.

The phrase "production-grade" must be supported by the validation described below before it appears publicly.

## Repository Trust Surface

The repository landing page must quickly answer:

1. What problem does IdemShield solve?
2. How is it installed and configured?
3. What behavior and limitations should adopters expect?
4. Which environments and providers are supported?
5. What evidence shows that the project is maintained and safe to evaluate?

The root README will include:

- A concise product statement and CI, NuGet, license, and .NET badges.
- A 60-second installation and configuration path.
- A minimal request example showing the idempotency header.
- A provider comparison for Redis and SQL Server.
- Documented replay, conflict, concurrency, expiry, and multi-tenant behavior.
- Explicit limitations and operational considerations.
- Links to focused documentation and contribution/security policies.

The repository will add or complete:

- `CHANGELOG.md`
- `SECURITY.md`
- `CONTRIBUTING.md`
- `CODE_OF_CONDUCT.md`
- Support and versioning guidance
- GitHub issue forms
- A pull-request template
- Dependabot configuration
- Automated dependency and security validation

Documentation will cover architecture, configuration, provider setup, concurrency semantics, multi-tenancy, failure behavior, schema ownership, expiry cleanup, and version migration.

## Validation and Release Confidence

The CI pipeline will continue to build and test on every push and pull request and will be extended to validate the public distribution path.

Required evidence before presenting the packages as production-grade:

- A warning-free Release build.
- Passing unit tests.
- Passing Redis integration tests against a real Redis service.
- Passing SQL Server integration tests against a real SQL Server service.
- A consumer smoke test that installs generated `.nupkg` files into a clean ASP.NET Core application and compiles a representative configuration.
- Validation of all package metadata, README inclusion, assemblies, XML documentation, symbols, and dependency relationships.
- A NuGet vulnerability audit with no unresolved known vulnerabilities.

A release workflow will build from a versioned tag, repeat validation, create immutable package artifacts, and publish only after explicit owner-controlled authorization. NuGet credentials or trusted publishing configuration will not be stored in the repository. Package IDs are not considered reserved until a successful NuGet publication.

The `main` branch should require the CI checks before merge. Releases will use semantic versioning and GitHub release notes derived from the changelog.

## GitHub Repository Presentation

After local implementation and verification, the public repository will be renamed to `IdemShield.NET`. GitHub metadata will include:

- The approved description.
- Relevant topics such as `dotnet`, `aspnetcore`, `idempotency`, `redis`, `sql-server`, `middleware`, and `distributed-systems`.
- The project or documentation homepage when one exists.
- Versioned tags and GitHub releases once packages are ready to publish.

The repository rename and metadata updates happen only after the codebase rename is reviewed and committed. GitHub redirects from the old repository URL may be relied upon for transition convenience, but all controlled links will be updated to the new canonical URL.

## Maintainer Profile Presentation

The GitHub profile will identify IdemShield as the flagship open-source project near the top. Its entry will state the problem solved, supported providers, current release maturity, and link to the repository and packages once published.

The existing terminal-inspired visual identity may remain, but the content will prioritize verifiable engineering work. Unsupported scale, performance, adoption, contribution, ranking, or uptime claims will be removed or explicitly contextualized. Package badges will not link to NuGet until the matching package is publicly available.

IdemShield should become the first pinned repository. If pin ordering cannot be changed through available GitHub access, the implementation handoff will include the exact manual action.

The profile repository is separate from the product repository. Its proposed README diff must be reviewed before it is published.

## Delivery Sequence

1. Rename the codebase and update package metadata, project references, namespaces, examples, documentation, and tests.
2. Add repository trust files, CI hardening, provider integration tests, and package-consumer validation.
3. Run the full Release build, tests, package inspection, vulnerability audit, and local-package consumer test.
4. Review and commit repository changes.
5. Rename and update the public GitHub repository, then push the reviewed branch.
6. Prepare and review the profile README change before publishing it to the profile repository.
7. Configure protected-branch and release settings that are supported by available access; document any required manual settings.
8. Publish to NuGet only after a separate explicit authorization and credential/trusted-publishing setup.

## Boundaries

- The work does not invent stars, download totals, coverage percentages, testimonials, compatibility claims, or performance results.
- The work does not publish packages or create releases without explicit authorization.
- The work does not create an empty organization to imply a team that does not exist.
- The work preserves unrelated local files and changes.
- A general web name search and package-ID availability check are preliminary collision checks, not legal trademark clearance.

## Success Criteria

The work is complete when a developer can discover IdemShield from the maintainer profile, understand and install the correct package from the repository, verify its supported providers and operational behavior, see green automated validation, inspect its maintenance/security policies, and follow a controlled release trail without encountering inconsistent naming or unsupported claims.
