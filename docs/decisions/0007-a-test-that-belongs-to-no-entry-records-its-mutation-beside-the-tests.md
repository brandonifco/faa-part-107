# 0007 — A test that belongs to no single map entry records its mutation beside the tests

**Status:** accepted. Issue #98.

## Context

`AGENTS.md` §7: *"Every test records the mutation that makes it fail. The overlay holds it. A test
whose named mutation was never observed to fail is a test nobody has watched fail."*

The overlay holds that record **per map entry**. `scripts/engine-gate.py named-tests` checks one
direction of it — that every test an `implemented` entry names exists and ran in every target
framework — and nothing checked the other. So a test nobody named passed silently. At the head this
record was written against, forty-four tests ran with no recorded mutation between them:

| file | tests | named by an overlay row before this change | unnamed |
|---|---|---|---|
| `tests/FaaPart107.Tests/OperationEvaluatorTests.cs` | 40 | 2 | 38 |
| `tests/FaaPart107.Tests/DeterminismTests.cs` | 5 | 0 | 5 |
| `tests/FaaPart107.Tests/SpeedWithinLimitEntryPointTests.cs` | 7 | 6 | 1 |

The issue's own table said `OperationEvaluatorTests` had **0** of 37 named, and that was wrong on
both figures: two of its tests were already filed under the entries they exercise, which is the
precedent this record builds on. The third file is not in the issue at all — its one unnamed test
was turned up by the census taken before any of this was written, and the check below would have
failed on it too. That is the point of having a check rather than a list: the gap was never
confined to the two files somebody had noticed.

The gap is not only that these tests carry no evidence. It is that the same silence hides a
**deletion**: the `weather-minimums-met` row for
`The_dictionary_dispatch_refuses_rather_than_answering_from_defaults` was dropped by an overlay
rewrite in #79 while the test still existed, and a hand review caught it rather than a check.

## The constraint, read first

The overlay is not this engine's to shape. `scripts/map-overlay.py` states rules-factory decision
0015's merge, and two of its rules decide this:

> 1. every overlay key names an entry in the package map;
> 2. every overlay item sets `status`, and holds no key outside the three and `rulings` and
>    `declines` …

So there is no key an engine may invent for a test that belongs to no entry, and no field beside
`tests` that could carry one. The packaged `check-map.py --phase consumer` holds the `tests` shape
to `{test, mutation}` with a non-empty mutation, and `map-overlay.py check` fails the gate on a key
that is not an entry id. **The overlay cannot hold this record, and bending it to is not available.**

## The classification, which comes before the question of where

The question "where does the evidence go for a test that belongs to no entry" is the second
question. The first is **which of these tests belongs to no entry**, and it is not answered by which
file a test happens to live in. A test that exercises one entry's rule *through* the evaluator has a
home already, and this repository has used it: `sufficient-available-power` and `preflight-actions`
each already name a test in `OperationEvaluatorTests.cs`.

The rule this record fixes:

> A test belongs to map entry **E** when the property it exists to prove is **E**'s own rule's
> behaviour — so that a mutation confined to **E**'s own rule reddens it, and the packet for **E**
> is improved by naming it. A test whose subject is a relation *between* entries, a census *over*
> entries, or the evaluator's own contract belongs to **no single** map entry. The issue's own
> words settle the multi-entry case: a test that belongs to no *single* entry is residue, because
> filing it under one of several would make the entry's packet claim evidence it does not have.

Assertions about other entries that stand as controls or as premises do not make a test
multi-entry; what makes it residue is that no one entry's rule is its subject. Applied to the
forty-four:

| | tests | where |
|---|---|---|
| exercise one entry's rule through the evaluator | 11 | that entry's `tests` in `corpus-map.overlay.json` |
| an entry-point test nothing had named | 1 | `speed-within-limit`'s `tests` |
| the evaluator's own contract, a census over entries, determinism, the state partition | 32 | `tests/FaaPart107.Tests/cross-cutting-mutations.json` |

The eleven are filed under `moving-aircraft-operation`, `speed-within-limit`,
`sufficient-available-power` (three), `operating-limitations`, `visual-observer-conditions`,
`over-human-beings`, `preflight-actions`, `civil-twilight-operation` and `weather-minimums-met`, and
every one of their recorded mutations is a substitution inside that entry's own rule or handler —
never inside `OperationEvaluator`. That is what makes the filing checkable rather than asserted: a
reviewer reads the substitution and sees whose code it is in.

## Decision

**The mutation for a test that belongs to no single map entry is recorded in
`tests/FaaPart107.Tests/cross-cutting-mutations.json`, in the same `{test, mutation}` shape an
overlay row uses, and `RecordedMutationTests` checks that the two records between them name every
test this engine runs.**

Three properties the location was chosen for:

- **It is on the semantic surface.** `.github/agent-policy.json` lists `tests/**` in
  `semanticPaths`, so an edit to this record requires the semantic verdict exactly as an edit to
  `corpus-map.overlay.json` does. A record that could change without a reviewer seeing it would be
  the same defect one level along.
- **It travels with the tests.** It sits in the directory whose tests it is about, is read by a test
  in that directory, and moves with them.
- **It is the engine's own file.** `scripts/factory/ownership.py` classifies what `produce` writes;
  a file the engine adds itself matches no row and is not the factory's to overwrite. Nothing in
  `provenance.json`'s `buildInputs` hashes it, so it needs no re-produce of its own.

`RecordedMutationTests.Every_test_on_the_semantic_surface_is_named_by_a_record_that_carries_its_mutation`
is the converse of `named-tests`, and asserts four things: that no test runs unnamed by either
record; that the cross-cutting record names no test that has stopped running; that no test is in
both records, so the two cannot disagree; and that no row there is empty of a mutation. Its scope is
every `[Fact]` and `[Theory]` in the built test assembly **except** those declared by a class in
`tests/FaaPart107.Tests/Generated/*.g.cs` — a `generated` row in `ownership.py`, rewritten from the
map by every `factory produce` and not this engine's to name a mutation for. The exemption is read
off that directory at run time rather than written down as a list of class names, so a hand-written
class cannot be exempted by being added to a list: it would have to be generated.

## What was rejected

**File a cross-cutting test under one of the entries it touches.** Mechanically this works, and it
is what makes it dangerous. `tools/entry-packet.py` and `named-tests` would then both say that
`Every_kernel_decline_reason_is_reported_as_a_state_of_its_own` is evidence for some entry's rule,
and it is not evidence for any entry's rule — it is about the totality of
`RequirementStates.For` over the kernel's enumeration. The engine would pass its own checks by
telling the reviewer something untrue.

**Invent an overlay key** — `"__cross-cutting"`, or an item under an entry with a fourth field.
Merge rules 1 and 2 above forbid both, `map-overlay.py check` fails on both, and the overlay is
rules-factory's interface rather than this engine's.

**Leave the evidence in the pull request body.** That is where it is today: `OperationEvaluatorTests`'
newest test had its two mutations watched and recorded in PR #100's body, and PR #112 recorded two
more in its own. A pull request body is read by no check, is not diffed against the test it
describes, and does not travel with the code. It is exactly the arrangement this issue reports as
the gap.

**Put the check in the gate.** `scripts/engine-gate.py` and `scripts/validate.sh` are both
`generated` in `ownership.py`: gate step 6 regenerates them and step 7 hashes them against
`provenance.json`, so a check added there would be reverted by the next `factory produce` and would
fail the gate before that. The converse of `named-tests` belongs upstream in rules-factory's gate
recipe **as well**, and this record is not an argument that it does not; what it says is that the
engine-owned place the gate already runs is `tests/`, that a test there runs under the gate on both
target frameworks and carries its own mutation like everything else, and that waiting for upstream
would leave the rail open in the meantime. If and when the gate recipe grows the check, this test
becomes the redundant half and can go.

**Escalate under `AGENTS.md` §6.** Nothing here is a question about what a regulation means, and no
recorded decision conflicts. The issue asked for the place to be decided and recorded, and said to
say so if the overlay cannot hold it. It cannot, and this is where it goes instead.

## Consequences

- **Forty-five tests now carry a mutation that was watched failing**: the forty-four above and
  `RecordedMutationTests`' own, whose mutation is a row removed from the cross-cutting record so
  that the check fails on the condition it exists for.
- **A test deleted along with its row is now caught from one side and a test added without a row
  from the other.** `named-tests` fails on an overlay row whose test no longer runs;
  `RecordedMutationTests` fails on a test that runs and is named by nobody, and on a cross-cutting
  row whose test no longer runs.
- **#86 has somewhere to put its scenarios' mutations.** Its criterion 5 requires every scenario to
  carry one; an end-to-end scenario through the evaluator that is about one entry goes under that
  entry, and one that is about the orchestration goes here.
- **No regulatory behaviour changes.** Nothing under `src/` changed: the mutations were applied to
  the working tree, observed, and reverted.
- **`docs/decisions/**`, `tests/**` and `corpus-map.overlay.json` are all on this engine's semantic
  surface** (`.github/agent-policy.json`), so the change carrying this record needs the semantic
  verdict.
