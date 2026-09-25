# 0009 — The engine is published to nuget.org as one exact, versioned package, and a consumer pins it by hash

**Status:** accepted when the owner merges the pull request that closes #132. That merge is the
ruling, on distribution only: this record rules on no map entry, and changes no rule or outcome. The
first publication is a separate act, also the owner's (see "What the owner does").

## Context

`docs/rules-api-development-guide.md` §4.3 leaves one question open for Phase A (§13.1): how this
engine is distributed. A host that reports this engine's provenance has to run a build it can
name exactly. Otherwise the provenance it reports describes some build, not necessarily the one
that produced the answer.

Until now `src/FaaPart107/FaaPart107.csproj` was not packaged. A consumer could reach the engine only
through a submodule or a project reference. Either one builds the engine inside the consumer, with
the consumer's SDK, from whatever commit the consumer has checked out. The guide accepts that for
the Phase A spike and nowhere else.

Two precedents in this project answer the same question for other artifacts, and agree:

- `RulesKernel` is published from rules-kernel's `publish.yml` to nuget.org. The trigger is a
  `vX.Y.Z` tag. The tree resolves to `<VersionPrefix>-dev` everywhere except a release commit
  (rules-kernel decision 0012). The push uses Trusted Publishing.
- `RulesFactory.Maps.*` are published from rules-factory's `publish-map.yml` to nuget.org, from a
  tag, with Trusted Publishing, and nothing is pushed that the gate did not check (rules-factory
  decision 0015).

This engine already restores both from nuget.org and nothing else (`NuGet.config`), pinned by
content hash in `packages.lock.json`.

## Decision

### The artifact

**One package, `FaaPart107`, on nuget.org.** The id is the assembly name, the root namespace and
`provenance.json`'s `engine.name`. It holds:

| path | what it is |
|---|---|
| `lib/net8.0/FaaPart107.dll`, `lib/net10.0/FaaPart107.dll` | the engine, for each framework `Directory.Build.props` targets |
| `lib/<tfm>/FaaPart107.xml` | its documentation, which is where most of this engine's contract is written |
| `FaaPart107.nuspec` | id, version, licence, and `<repository url commit>`, the commit it was built from |

A symbols package (`.snupkg`) is published beside it.

Nothing else goes in: not the corpus, not the map, not the tests, and no content files. The public
API is the assembly's existing public surface, unchanged. This record makes no type more or less
visible.

**The only dependency is `RulesKernel`, at the version `RulesFactory.Packages.g.props` pins.** The
map package and `RulesKernel.Analyzers` are `PrivateAssets="all"` and do not flow to a consumer.
They are build inputs, and the evaluator never reads them at run time.

`tools/check-package.py` holds all of the above against a packed `.nupkg`, and fails by name on
anything else. It checks the id and the version, that exactly the listed files are present, that
each framework's dependency group is `RulesKernel` at `provenance.json`'s `kernel.version`, that the
repository and commit are present, and that every packed DLL embeds this tree's `provenance.json`
byte for byte. `.github/workflows/package.yml` runs it on every pull request and every push to
`main`.

### Versions

**SemVer, independent of the ruleset version.** `main` always resolves to the next version with a
`-dev` suffix (`VersionPrefix`/`VersionSuffix` in `FaaPart107.csproj`). A default `dotnet pack` on
`main` therefore produces a version nuget.org never serves. A release is a pull request that clears
`VersionSuffix` and then re-produces (see "Provenance"), and nothing else. Its merge commit is tagged. The next pull request moves
`VersionPrefix` on and restores the suffix. This is rules-kernel decision 0012's cycle, adopted for
its reason: a development tree must not be packable under a version that is also a release.

The question each bump answers is the one rules-factory 0015 asks of a map: **could a consumer that
was correct against the previous version be wrong under this one?**

- **Major** (minor while the version is below `1.0.0`) is any change that could alter an
  `OperationEvaluation` for the same `OperationFacts`, that moves the map, corpus or kernel pin, that
  changes `Ruleset.Identity` or the replay-schema version, or that removes or changes public API.
  Any change not listed under minor or patch is major.
- **Minor** (patch while below `1.0.0`) is public API added without changing any existing
  outcome.
- **Patch** changes no evaluation outcome and no public API: documentation, and packaging metadata
  that does not change the files above.

The first release is `0.1.0`. The version says nothing about completeness. The README's state
table and the map's own statuses do that.

### Provenance

The package's identity has three parts, and each is checkable:

1. **The engine's own record.** Every DLL embeds `provenance.json` byte for byte, and
   `OperationEvaluation.ProvenanceJson()` returns it. It names the factory commit, the map package
   and version with the SHA-256 of its parts, the corpus source, `asOf` and content hash, the kernel,
   and the hashes of every generated and managed file. This is what a host reports, and what it
   hashes.
2. **The source commit.** The nuspec's `<repository commit>` is the commit the package was built
   from. On a release that commit is the one tagged `vX.Y.Z`, and `publish.yml` checks it against
   `GITHUB_SHA`.
3. **The bytes.** nuget.org never replaces a version. A consumer's `packages.lock.json` records the
   package's `contentHash`, and a locked restore refuses any other bytes under that version.

**What the embedded record does not claim.** Its `buildInputs` list hashes the engine-owned files,
this `.csproj` among them, as of the last `factory produce`. `scripts/engine-gate.py provenance`
deliberately does not hold those hashes between produces: engine-owned edits are the engine's own
acts. So the packaging properties this record adds leave `buildInputs[src/FaaPart107/FaaPart107.csproj]`
recording the pre-packaging bytes until the next produce refreshes it. None of those properties
changes what the engine computes. The one identity they do change is `AssemblyVersion`, which moves
from the SDK default `1.0.0.0` to `<VersionPrefix>.0`. Nothing in the engine reads it. The source
commit in (2) identifies the exact tree.

A release must not carry that lag, because `factory provenance --engine` compares every build input
and would report the mismatch at the tagged commit. **So the release pull request finishes with
`tools/re-produce.sh`.** It clears `VersionSuffix`, then re-produces, so the record it embeds hashes
the very `.csproj` being tagged. That is the same rule that already finishes an overlay change
(`AGENTS.md` §7). Between releases, `main`'s record may lag an engine-owned edit, as it always could.

`dotnet pack` is not byte-reproducible: the `.nupkg` container carries timestamps and a random part
name, as rules-factory 0015 measured. The compiled assemblies are deterministic
(`Deterministic`, and `ContinuousIntegrationBuild` in CI). That is why `publish.yml` pushes the very
files its `gate` job checked instead of packing a second time, and why the consumer pins the
`contentHash` of what was published rather than of a rebuild.

### How a consumer pins it

```xml
<PackageVersion Include="FaaPart107" Version="[0.1.0]" />
```

It uses an exact version range, `RestorePackagesWithLockFile`, and `--locked-mode` restore in CI.
The engine's `RulesKernel` dependency is a floor, `0.3.0`, as NuGet writes a plain version. A
consumer that pins `RulesKernel` higher would run the engine on a kernel its provenance does not
name, so a consumer checks that the resolved kernel equals `ProvenanceJson()`'s `kernel.version`.

### Release

A release is a `vX.Y.Z` tag on a `main` commit whose resolved version has no suffix. **The owner
pushes it.** `publish.yml` then does the following:

1. requires the tagged commit to be on `main`;
2. runs `./scripts/validate.sh full`;
3. requires the resolved `PackageVersion` to equal the tag;
4. packs, and runs `tools/check-package.py` on the `.nupkg` with the tag's version and the commit.
   The `.snupkg` holds only the symbols built by the same `pack`, and is checked for presence alone;
5. hands exactly those files to a job that alone holds `id-token: write`, which exchanges an OIDC
   token for a short-lived nuget.org key and pushes them, with no `--skip-duplicate`. If the
   `.nupkg` is accepted and the symbols push then fails, a retry fails on the duplicate by design.
   The version exists, and its symbols are pushed by hand.

### What the owner does

- Before the first release, add a nuget.org Trusted Publishing policy: owner `brandonifco`,
  repository `faa-part-107`, workflow `publish.yml`. The kernel and the maps already have the same
  kind of policy.
- For each release, merge the release pull request (suffix cleared, then re-produced) and push the
  tag. A published version can be unlisted and never deleted, so no agent pushes a release tag.

## What was rejected

- **GitHub Packages.** Its NuGet feed requires a credential to restore, even for a public package.
  Every consumer would carry a token for a dependency that is public by licence. The kernel and the
  maps rejected it for the same reason.
- **A git submodule or project reference as the lasting answer.** It builds the engine inside the
  consumer, with the consumer's SDK. That leaves no published bytes to pin, and no lock-file hash to
  record against an evaluation. The guide keeps it for the Phase A spike only.
- **Versioning by the ruleset version, or by the corpus date.** `Ruleset.Identity`'s version does
  not move when a handler bug is fixed, and a corpus date does not move when the map does. Neither
  answers whether a consumer's stored results still hold.
- **Packing twice and comparing.** `publish-map.yml` does this because its packer is deterministic.
  `dotnet pack` is not, so two packs would never agree. Pushing the checked files is the same
  guarantee, reached another way.
- **Pinning `RulesKernel` as `[0.3.0]` in the package.** The pin lives in the generated
  `RulesFactory.Packages.g.props`, which this engine does not edit. Restating it in the `.csproj`
  would be a second copy that a `factory produce` does not update. The floor plus the consumer's
  check says the same thing with one source.

## Consequences

- `FaaPart107.csproj` carries the package properties. `package.yml`, `publish.yml` and
  `tools/check-package.py` are new, engine-owned files. No generated or managed file changes, and no
  engine behaviour changes.
- `brandonifco/rules-api` replaces its Phase A spike reference with `FaaPart107` at an exact version
  once `0.1.0` is published.
- A `factory produce` that moves the kernel pin moves the package's dependency with it, and
  `check-package.py` fails if the two ever disagree.
