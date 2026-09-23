# 0004 — An assertion is recorded and not scored, and the evaluator states what it did not evaluate

**Status:** accepted.

## Context

Issue #51 asks for a product-facing orchestration API inside this engine:
`OperationEvaluation Evaluate(OperationFacts facts)`, over the entries the map has and this engine
has built. Its governing requirement is that a caller must always be able to tell **"we did not
say"** from **"we said no"** from **"the engine cannot determine this"** from **"the engine has not
built this"** from **"this is outside the engine's scope"**, and that collapsing any pair of those
is the defect the API exists to prevent.

Almost all of that is mechanical. The correspondence table (rules-factory `docs/corpus-map.md`)
fixes which `UnresolvedReason` an entry's row declines with, `Registry` applies it, and mapping a
reason onto a product-facing state has no regulatory content: one reason, one state, and the
mapping is total and injective.

Reading a **verdict** off a resolved value is nearly as mechanical, because the rules name their own
verdicts and say which direction is compliance in the finding type's own documentation —
`GroundspeedFinding.WithinLimit` ("whether the groundspeed is within the limit"),
`MovingVehicleFinding.Prohibited` ("true when § 107.25(b) prohibits the operation"),
`RightOfWayFinding.MayPass` ("true when § 107.37(a) does not prohibit this pass"),
`AirspaceFinding.MayOperate`, `AreaPermissionFinding.Permitted`,
`MultipleAircraftFinding.Permitted`, `AltitudeFinding.WithinLimit`,
`WeatherMinimumsFinding.MinimumsMet`. Each of those is one property, read in the direction its own
prose states.

**One family of entries does not work that way**, and that is what this record is about. Ten entries
of `RulesFactory.Maps.FaaPart107` 4.0.0 are `kind: assertion`, and all ten are implemented today.
Each answers with an `Assertion`, whose `Holds` is documented as *"True when the entry's
proposition is so, as the asserter reports it"* — and nothing anywhere says whether the proposition
being so is compliance or is the breach.

It is not a theoretical distinction. Two implemented entries answer with the same type and invert:

| entry | locator | evidence, as the map quotes it | `Holds: true` is |
|---|---|---|---|
| `sufficient-available-power` | § 107.49(d) | "ensure that there is enough available power …" | the required state |
| `collision-hazard-proximity` | § 107.37(b) | "No person may operate a small unmanned aircraft so close to another aircraft as to create a collision hazard." | the **prohibited** state |

An orchestrator that mapped `Holds` to "satisfied" would report a stated collision hazard as
compliance. One that mapped it to "violated" would report enough battery as a breach. Both are
wrong, so no uniform mapping is available.

## The question

Could the evaluator map `Holds` **per entry**, in the direction each entry's own paragraph
requires?

It could only by deciding, for each entry, whether the sentence the map quotes as `evidence`
states a requirement or a prohibition. That is a reading of the corpus, performed by the
orchestrator. `AGENTS.md` §5 is explicit that this engine implements the map and does not re-read
the corpus to decide what a rule really says, and issue #51's own fifth criterion is that the
orchestrator contains "no decision about what a rule requires". The map records `kind`, `clarity`,
`scope`, `assertedBy`, `dependsOn`, `suspendedBy` and `evidence`; it records **no polarity**, and
neither does any rule of this engine.

So the reading is not available to this change, and inventing it here is precisely the failure
that `AGENTS.md` §5 calls an unreviewed mapper.

## Decision

**A `kind: assertion` entry's supplied value is reported in a state of its own,
`RequirementState.HumanAssertionRecorded`, which is neither `Satisfied` nor `Violated`.** The
outcome carries the `Assertion` itself on `EvaluatedRequirement.Finding` — the fact, who is
answerable for it, and the map's own citation — so a caller has everything the engine has, and the
engine claims nothing the map does not record.

Three consequences of that sentence, and each is deliberate:

- **`HumanAssertionRecorded` is distinct from `HumanAssertionRequired`.** "The remote pilot in
  command says there is not enough power" and "nobody has said anything about the power" are
  different answers, and both are different from a decline. This is the "we did not say" / "we said
  no" distinction the issue exists to protect, at the one place it is easiest to lose.
- **The test is the map's, not a list of types this engine keeps.** An entry whose first
  correspondence row is row 8 (`RegisteredEntry.Row == CorrespondenceRow.Assertion`) is
  `kind: assertion`, and a value it resolved to is recorded. Five of the ten answer with a bare
  `Assertion`; the other five wrap it in a finding of their own so that the § 107.205 waiver
  statement travels beside it — `UnaidedVisualContactFinding.Contact`,
  `ObserverCoordinationFinding.Coordination`, `IntensityReductionFinding.Determination`,
  `FlashRateSufficientFinding.Sufficiency`, `ReasonableProtectionFinding.Protection`. The row test
  covers all ten without naming any of them or any of their types, so an assertion entry a later
  map version adds is covered the day it is built rather than the day somebody remembers this
  record. The gate decides whether the entry is reachable; it does not add a polarity the corpus
  did not state.
- **It is not `Informational`.** `Informational` is what the *rule* says — a printed figure, or a
  finding whose own documentation disclaims a verdict. An assertion is what a *person* said. Those
  are different sources and the result type keeps them apart.

**Where a finding type does state its own direction, the evaluator reads it, one property, in that
direction, and never combines two into a verdict.** The one place it reads a second property is
where the rule itself draws the distinction between "not met" and "not yet obtained" in a field of
its own — `AirspaceFinding.AuthorizationRequired`, `AreaPermissionFinding.PermissionRequired` —
which produce `RequirementState.ActionRequired`. That distinction is the rule's and is never
invented for a rule that does not draw it.

**A resolved value the evaluator has no reading for is `Informational`, never a verdict.** A
finding type added later for an entry that is *not* row 8, and not named in the evaluator's table,
therefore loses detail and never gains a wrong verdict. The failure mode is chosen: it is the one
that cannot convict or excuse.

## What was rejected

**Map `Assertion.Holds` to satisfied/violated per entry.** The reading the map does not record,
made by the orchestrator. It is the whole subject of this record.

**Map `Assertion.Holds` to satisfied/violated uniformly.** Wrong for § 107.37(b) in one direction
and wrong for § 107.49(d) in the other, whichever direction is picked.

**Report a supplied assertion as `Informational`.** It is not the rule speaking, and folding the
two together would lose the source of the fact — which, for an assertion, is most of what the
answer is worth (`Assertion`'s own remarks, and rules-factory decision 0025).

**Report a supplied assertion as `HumanAssertionRequired` regardless of the value.** That collapses
"asserted" into "not asserted", which is the exact defect this API exists to prevent.

**Ask the map for a polarity field, and block issue #51 on it.** A map is corrected where maps are
corrected — a new, checked, published version — and the evaluator is faithful without one. The
missing field is worth an upstream question; it is not worth an engine that either guesses or does
not ship. The question is filed as **rules-factory#453**, *"An assertion records who asserts it but
not which way it points, so an engine cannot tell a satisfied assertion from a breached one"*, so a
later reader can find out whether it was ever answered rather than taking this record's word that
it was asked. If the map ever records polarity, this engine reads it from generated `MapEntries`
the way `Assertions.Stated` reads `assertedBy`, and this record is superseded rather than worked
around.

**Note that a polarity field may not be the right fix, and this record does not assume it is.** One
defensible answer to #453 is that the map needs no such field at all: an assertion is a *fact*, and
its consequence is always some consuming entry's to compute — § 107.31(b) is what makes
§ 107.31(a)'s ability into a requirement, and nothing in § 107.31(a) alone says which way it
points. On that reading the defect is not a missing schema field but an assertion entry with no
consumer, and the question becomes one of map completeness. This engine behaves identically either
way: it records the fact and lets a consumer, when the map has one, do the computing. Which reading
upstream takes is upstream's to decide, and #453 carries both.

**Escalate under `AGENTS.md` §6 and stop.** §6 is for an ambiguity the engine cannot answer
faithfully. This one it can: reporting the asserted fact, its asserter and its citation, without a
compliance verdict, is a true and complete statement of what this engine knows. Declining to score
is the engine working, not the engine failing — the same posture §6 records for a decline.

## Consequences

- **The aggregate cannot be read as "legal to fly", and the type will not let it be.**
  `OperationEvaluation` and `EvaluatedRequirement` expose no `bool` at all — no `IsCompliant`, no
  `Passed`, no `IsValid` — and `OperationEvaluatorTests.The_evaluation_does_not_collapse_to_a_boolean_and_offers_no_way_to_claim_one`
  checks that by reflection over every public member of both types, so a later `bool` added in
  good faith fails the gate. The aggregate a caller can read is a count per state, plus
  `Unanswered`, which is the engine naming its own holes.
- **All ten `kind: assertion` entries are bound by this**, and every one of them is now built:
  `collision-hazard-proximity`, `reasonable-protection`, `flash-rate-sufficient`,
  `intensity-reduction-in-interest-of-safety`, `unaided-visual-contact`, `observer-coordination`,
  `preflight-risk-assessment`, `participant-briefing`, `sufficient-available-power` and
  `attached-object-no-adverse-effect`. An entry a later map version adds inherits this from its
  row, with no per-entry decision to make and none to re-make.
  `OperationEvaluatorTests.No_assertion_entry_is_ever_reported_as_satisfied_or_violated` walks the
  registry rather than a list, so it covers them as they land.
- **What this costs a caller, measured rather than asserted.** A consuming entry supplies the
  polarity by *using* the fact: where a built entry's own verdict requires an assertion to hold,
  that entry has said which way the assertion points, and a product reads compliance off the
  consumer's finding rather than off the assertion. The map is now fully built, and of the ten
  implemented row-8 entries **nine** have such a consumer:

  | assertion entry | built consumer | how the consumer fixes the polarity |
  |---|---|---|
  | `unaided-visual-contact` | `visual-line-of-sight` | `Maintained => Ability.Holds && Exercised` |
  | `unaided-visual-contact` | `visual-observer-conditions` | § 107.33(b), answered through `visual-line-of-sight` |
  | `reasonable-protection` | `over-human-beings` | § 107.39(b)'s excepted case is met only where the standard `Holds`, and `MayOperate` is met only where some excepted case is |
  | `flash-rate-sufficient` | `anti-collision-lighting` | `Met` requires `FlashRate is { Holds: true }` |
  | `intensity-reduction-in-interest-of-safety` | `anti-collision-lighting` | `WithinTheBound` requires `Reduction is { Holds: true }`, **and only where the intensity is stated reduced** — the clause bounds what may be done to the lighting, so where nothing was done the determination is not read |
  | `observer-coordination` | `visual-observer-conditions` | § 107.33(c), `finding => finding.Holds`, **and only where a visual observer is used** — § 107.33's chapeau is a condition |
  | `preflight-risk-assessment` | `preflight-actions` | § 107.49(a): an obligation answered **not done** settles the section's conjunction against the operation |
  | `participant-briefing` | `preflight-actions` | § 107.49(b), the same way |
  | `sufficient-available-power` | `preflight-actions` | § 107.49(d), the same way |
  | `attached-object-no-adverse-effect` | `preflight-actions` | § 107.49(e)'s second conjunct, the same way |

  The four § 107.49 constituents are worth one qualification, because it is the difference between
  a direction and a verdict. `preflight-actions` can never resolve *complete* — § 107.49(c) and the
  "is secure" half of (e) state no measure, so an obligation left undetermined makes the whole entry
  decline — so what it establishes is that an assertion answered **false** settles § 107.49 against
  the operation. It never confirms the other direction. That is enough to tell a product which way
  the fact points, which is what this bullet measures, and it is less than a full verdict.

  **One has none, and it is the structural case**: `collision-hazard-proximity` has no consumer
  **in the map at all**, because § 107.37(b) is a prohibition standing on its own and nothing
  entails anything from it. The temporary half of this cost is now zero — every assertion entry the
  map gives a consumer has that consumer built — so what remains is exactly the gap rules-factory#453
  asks about, and nothing that building more entries would close.

  So today a product that wants to know whether an asserted `collision-hazard-proximity = true` is
  good news or bad has nowhere in this engine to read it, and would have to go to the CFR itself —
  which is the second unreviewed reading `AGENTS.md` §5 exists to prevent, relocated to outside the
  engine. That is the map's gap and not this engine's to close, and it is named here so that nobody
  has to rediscover it.

  **This census is checked, not proof-read.** It was written at one head and not re-measured when
  the branch moved, and was wrong.
  `OperationEvaluatorTests.The_cost_recorded_in_decision_0004_is_the_cost_the_engine_actually_has`
  now measures it from the engine — an assertion entry has a built consumer exactly when flipping
  the asserted fact moves some other entry's answer — and fails naming this record when the answer
  changes. It earned itself twice while this change was in review: `over-human-beings` landing
  moved `reasonable-protection` from the second list to the first, and `preflight-actions` landing
  moved the four § 107.49 constituents, taking the count from one to five to nine. Neither was
  noticed by reading; both were the test going red. Nothing in the map will move it again — every
  entry is built — so the next thing that could is a new map version.
- **Nothing in any handler or rule changes.** This record is about how the orchestrator reads what
  the handlers already return; every regulatory answer in the output is still a handler's.
- **`docs/decisions/**` is on this engine's semantic surface** (`.github/agent-policy.json`), so
  the change carrying this record needs the semantic verdict. That is intended: it is a statement
  about what this engine may and may not read out of the map.
