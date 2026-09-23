# 0003 — `caller` is not a name to match, and the attribution is recorded rather than checked

**Status:** accepted.

## Context

Ten entries in `RulesFactory.Maps.FaaPart107` 4.0.0 are `kind: assertion`, and every one carries
`assertedBy`: the list of who the corpus says supplies or decides the fact, in the corpus's own
words. Seven of those lists are people — `remote pilot in command` on six, and the three named
persons on § 107.31(a) and § 107.33(c). Three are the single value `caller`:

| entry | locator |
|---|---|
| `collision-hazard-proximity` | § 107.37(b) |
| `reasonable-protection` | § 107.39(b) |
| `flash-rate-sufficient` | § 107.29(a)(2), (b) |

`Rules/Assertions.cs` — the shared mechanism every assertion entry answers through — checked the
caller's attribution by ordinal membership of that list, with no case for the marker. Under that
check the three entries above would accept exactly one attribution: the literal string `caller`.
That records nobody. An assertion's attribution exists so that a fact the engine did not determine
travels with whoever is answerable for it, and `caller` is not a person who can be answerable for
anything.

The first implementer of one of these three escalated the question under `AGENTS.md` §6 rather than
choosing a reading (issue #72). It was returned as answered, not ruled on: the map's own note cites
the decision that settles it, and nobody had followed the citation.

## The authority

**rules-factory decision 0025**, `an-assertion-names-who-asserts-it-and-an-operation-names-what-it-draws`,
is the decision that created `assertedBy`. It states the reading of the marker in the clause that
defines it:

> **`caller`, where the corpus names nobody.** `["caller"]` stands alone, and the entry's `note`
> says why, naming the caller. *"(as may have been agreed)"* is an agentless passive
> (corpus-map.md, gate 3). § 107.37(b)'s *"No person may operate … so close … as to create a
> collision hazard"* names the subject of a prohibition and nobody whose determination settles the
> hazard. **`caller` does not mean "anyone". It means that the corpus does not narrow who may
> assert, so the engine attributes the assertion to whoever the caller says made it.**

Two things make this the authority for *this* engine rather than an analogy from another corpus.
It names § 107.37(b) in the clause itself, quoting the very sentence this engine's
`collision-hazard-proximity` is mapped from. And 0025's "Applied" table names all three Part 107
entries by id:

> | `faa-part-107` (2026) | 10 assertions | `remote pilot in command` ×6 (4 anchored in § 107.49's
> quoted lead-in), the three named persons on § 107.31(a) and § 107.33(c), `caller` on
> `collision-hazard-proximity`, `reasonable-protection`, `flash-rate-sufficient` |

Each of the three entries' notes ends by citing it — this engine's own `collision-hazard-proximity`
note says *"so assertedBy is the caller (0025)"* — so the map is pointing at 0025, not at a reading
of it.

`AGENTS.md` §2 puts the corpus map first in the order of authority. 0025 is the map's own decision
about the map's own vocabulary. This record follows it; it does not decide anything 0025 left open.

## Decision

**Where an entry's `assertedBy` is exactly `["caller"]`, the attribution check does not constrain
the value. Any attribution the caller gives is accepted, and is recorded with the outcome exactly
as every other assertion's is.**

`Assertions.Stated` gains one predicate, `NamesNobody(entry)`, true when `entry.AssertedBy` is a
single element ordinally equal to `caller`, and the membership refusal is skipped for such an
entry. Nothing else in the mechanism moves. In particular:

- **The attribution is still recorded.** The answer is rebuilt as
  `new Assertion(entry, assertion.Holds, assertion.AssertedBy)` on the map's own `MapEntry`, so
  `AssertedBy` reaches the outcome unchanged and appears in the answer's `ToString()`. "Not
  checked" is not "not carried": an attribution that vanished on the way out would record nobody,
  which is the state this record exists to get out of.
- **`Assertion.AssertedBy` is still non-empty.** That is the record type's own invariant, older
  than this decision and unchanged by it, so "any attribution" means any non-blank string. An
  assertion with no attribution at all is still refused, for every entry.
- **The other three refusals are untouched**, for every entry: a value that is not an `Assertion`,
  an assertion about a different entry, and a missing assertion (`AssertionRequiredException`,
  which is a demand on the caller and never an unresolved result).

**Where `assertedBy` names people, the existing ordinal membership check stays exactly as it was.**
The corpus genuinely differs between sections — § 107.31(a) says *"the person manipulating the
flight control"* and § 107.33(c) says *"flight controls"* — so the map is faithful and normalizing
would have this engine paper over a distinction the corpus makes. Those seven entries behave today
exactly as they did before this record, and the test that proves it is the one the first assertion
entry already had.

**"Exactly `["caller"]`" is the test, and it is the map's rule, not this engine's convenience.**
0025 requires that `["caller"]` stand alone, and `check-map.py --only asserted-by` enforces that at
publish time. A list that named the marker alongside a person would therefore be a map defect, and
this engine refuses the attribution rather than reading a defect as permission — which is the
`AGENTS.md` §5 posture: where the map and this engine's expectation part, the engine does not widen
itself to fit.

## What was rejected

**Require the literal string `caller` as the attribution.** This is the behaviour before the
change, arrived at by not having thought about the marker. It records nobody, which is the opposite
of what an assertion's attribution is for, and it makes `collision-hazard-proximity`'s answer say
that "caller" is answerable for the fact.

**Refuse the literal string `caller`.** Tempting, because the marker is the map's word and not a
person, and a caller who passes it is probably confused. But 0025 does not say to refuse it, and
adding a constraint a decision does not state is this engine overruling the map — the same defect
as widening `assertedBy` from its own reading, in the other direction. `caller` is accepted like
any other string, and recorded like any other string. It is refused on the seven entries whose
lists are people, because it is not on those lists.

**Normalize or case-fold the comparison so that near-matches pass.** Rejected for the seven, on the
first assertion entry's own grounds (above), and moot for the three, where nothing is compared.

**Keep the relaxation local to `collision-hazard-proximity`.** The marker is the map's, not this
entry's, and two more entries carry it. A per-entry answer would have the second and third
implementers each decide the question again, which is how three engines end up with three readings
of one word.

**Treat `caller` as a fourth `UnresolvedReason`, declining the three entries.** They are
`clarity: clear`. The corpus not naming an asserter is not the corpus failing to settle the
question — 0025 says exactly that, and the fact is still one somebody determines and reports.

## Consequences

- **Three entries are bound by this, and only the first of them decides it.**
  `collision-hazard-proximity` (§ 107.37(b)) is implemented under this record.
  `reasonable-protection` (§ 107.39(b)) and `flash-rate-sufficient` (§ 107.29(a)(2), (b)) inherit
  the mechanism already changed: their implementers check that they inherited it — the shared
  refusal does not fire for their entry, and their entry's attribution reaches the answer — rather
  than re-deciding the reading or changing `Assertions.Stated` again.
- **The change is in the shared mechanism, so both halves are pinned by tests.**
  `CollisionHazardProximityEntryPointTests` proves that six different attributions are accepted and
  recorded for § 107.37(b), that the literal `caller` is among them, and — in
  `An_entry_whose_assertedBy_names_people_still_refuses_an_attribution_it_does_not_name` — that
  `sufficient-available-power` still refuses `visual observer`, and refuses the marker too. The two
  mutations that matter are recorded in `corpus-map.overlay.json`: `NamesNobody` returning `false`
  always (the marker path then accepts nothing but the marker) and returning `true` always (the
  people path then accepts anything). Each turns exactly one half red.
- **This engine reads `assertedBy` and never a string it typed.** The predicate reads
  `entry.AssertedBy` from generated `MapEntries`, so a map version that changed an entry's
  `assertedBy` changes this engine's behaviour by regeneration and not by anyone remembering to
  edit a list.
- **Nothing about the seven remote-pilot entries changes**, and no existing test was modified to
  accommodate this record.
- **`docs/decisions/**` is on this engine's semantic surface** (`.github/agent-policy.json`), so a
  change carrying this record needs the semantic verdict. That is intended: this is a reading of
  the map, which is what that verdict is for.
