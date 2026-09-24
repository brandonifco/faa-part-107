# 0007 — The § 107.205 waiver gate precedes every other demand a gated entry makes

**Status:** accepted.

## Context

The map gives twenty-seven entries `suspendedBy: ["waivable-regulations"]`. Each one's rule reads
§ 107.205's gate — `Waivers.Suspension` — and declines `OutsideCurrentScope` citing § 107.205 while
the caller states a certificate of waiver of that entry's regulation is in force.

Fifteen of those entries also demand a typed input of their own. They split on whether the gate is
read **before** or **after** those inputs are demanded, and the split is observable: a caller who
states a waiver in force and leaves a typed input unset got two different answers from the same
shape of request.

Issue #93 recorded the split as four entries one way and two the other. Measured through
`OperationEvaluator` on this branch's parent (`4fe7276`), against `OperationFacts.Nothing` plus one
`WaiverStatement.Held` for the entry's own regulation, it was **thirteen to two**:

| what a waived, otherwise-undescribed request was told | entries |
|---|---|
| `FactRequired`, `MissingInput` naming a typed input | `speed-within-limit` (`Groundspeed`), `altitude-within-limit` (`AltitudeAboveGroundLevelFeet`), `weather-minimums-met` (`FlightVisibilityStatuteMiles`), `operating-limitations` (`Person`), `single-aircraft` (`Person`), `airspace-authorized` (`Airspace`), `moving-vehicle-operation` (`FromMovingLandOrWaterBorneVehicle`), `moving-aircraft-operation` (`FromAMovingAircraft`), `civil-twilight-operation` (`Place`), `right-of-way` (`Encountered`), `anti-collision-lighting` (`Lighting`), `visual-line-of-sight` (`Exercise`), `visual-observer-conditions` (`Use`) |
| `OutsideCurrentScope`, citing § 107.205 | `reasonable-protection`, `over-human-beings` |

The other twelve gated entries demand no typed input at all, so the ordering is not observable on
them; they already declined.

**The divergence lived entirely in the handler layer.** Every one of the fifteen rules reads the
gate as its first statement. What differed was whether the handler wrapped the rule's other
arguments in `Demand(...)` — which throws `ArgumentException` at the call site, before the rule is
entered — or passed them through as the caller left them so the rule could demand them after the
gate. No fact in the map discriminates the two groups: all twenty-seven carry the same
`suspendedBy`.

## The two arguments

**Gate first.** `Rules/Protection.cs` already recorded this one, and recorded it in fully general
form: *"The gate comes first, before either of the other two facts. … asking for either first would
demand a fact the waiver has made irrelevant, and a refusal is what the caller would get for an
entry that had nothing to ask."* Nothing in that sentence is about § 107.39(b). It applies verbatim
to every entry whose row carries `suspendedBy: waivable-regulations`, including all thirteen on the
other side — which is what made the split an unrecorded inconsistency rather than two defensible
local choices.

**Demand first.** The typed inputs are the arguments of the call, so demanding them is what makes
the call possible at all, and a request that cannot be formed is the caller's error whatever the
waiver says. This is not a silly argument: it puts every one of an entry's caller facts on the same
footing and refuses to let the answer to "what do I still owe?" depend on another answer.

## Decision

**A § 107.205 gate is read before the entry demands anything else. A handler demands the waiver
statement and nothing else; every other input is handed to the rule as the caller left it, and the
rule demands it after the gate.**

So, for every gated entry, a request that states a waiver of its regulation is in force is declined
`UnresolvedReason.OutsideCurrentScope` citing § 107.205 and naming that entry — whatever else the
caller did or did not state. `RequirementState.OutsideCurrentScope`, `MissingInput` null.

### Why, on a ground that is not "the minority reads better"

**`FactRequired` is a claim about what the caller owes, and under a waiver the caller owes
nothing.** `RequirementState`'s own documentation groups `ActionRequired`, `HumanAssertionRequired`
and `FactRequired` as *"what the caller still owes"*, and `OperationEvaluation.Outstanding` is
documented as *"Everything the caller still owes this engine before it could say more."* For a
suspended entry there is no `Person`, no `Use`, no `Lighting`, no `Place` the caller could supply
that would make the entry say more, because the entry is not in play. Thirteen entries filed
themselves under `Outstanding` anyway, each naming a specific fact on `MissingInput` — so a product
rendering that list would ask somebody for their `Person` for a rule it had just told them does not
apply.

That is `docs/decisions/0005`'s own argument, met from the other side. 0005 removed a wrong
`Outstanding` entry by reading a rule's verdict properly: *"The caller owed nothing. The engine had
answered, and the answer was no."* Here the engine has not answered and never will on this entry,
which is a different case — but the thing being claimed by `Outstanding` is the same claim, and it
is false in the same way.

**The same three states are what `OutsideCurrentScope` is for.** Its own member documentation names
this case in terms: *"the caller stated a certificate of waiver authorizing deviation from the
regulation that states it, which suspends the entry (§ 107.205, and rules-factory decision 0021)."*
It sits in the group the enumeration calls *"what this engine cannot answer at all"* — which is
exactly the right thing to say about a suspended entry, and is not what the thirteen were saying.

**The majority's defence does not survive contact with the code.** Three things are wrong with
"the inputs are the arguments of the call":

- The waiver statement is *also* an argument of the call, and it was already demanded ahead of the
  others in all fifteen. So the convention was never "all the arguments, together": it was already
  ordered, and the question was only where the rest sat relative to the gate.
- Every request property is a nullable `init` property, so no request is ever unformed. There is no
  call that "cannot be made" — `Registry.Resolve(entryId, RuleRequest.Empty)` makes every one of
  them today.
- What the thirteen actually implemented was *source order* — `Person` before `Groundspeed` before
  `AltitudeAboveGroundLevelFeet` — which is not a principle about anything.

**And the gate validates the statement itself.** `Waivers.Suspension` refuses a statement about
another regulation. Under the old ordering a caller who supplied a wrong-regulation waiver *and*
left a typed input unset was told about the input, and only told about the mis-addressed waiver
after fixing it. Now the statement is checked first.

### What follows, and is deliberate

**An entry asked with nothing stated now names its waiver statement first.** Before, thirteen named
a typed input and fourteen named the waiver; now all twenty-seven name the waiver. That uniformity
is a consequence of the decision rather than a separate one: the gate's own input is the first thing
a gated entry owes, and `OperationFacts.Nothing`'s documentation already described the gated
assertion entries that way.

**An entry may demand later still, and two do.** The gate first is a floor, not a schedule.
`over-human-beings` demands `Shelter` only once the caller has said the human being is under one of
§ 107.39(b)'s two places, because a human being under neither makes the place the standard was
asserted over irrelevant in its own right; `reasonable-protection` demands `Shelter` after the gate
and before the assertion. Both satisfy this record. What no gated entry may do is demand a
non-waiver input *before* the gate.

**A malformed value is checked after the gate too**, because it is checked from the value the entry
demanded and the entry demands nothing until the gate has run. `Altitude.Within`'s negative-altitude
check and `MultipleAircraft.AtTheSameTime`'s blank-person check moved with their demands. A caller
who states a waiver in force and a negative altitude is now declined rather than thrown at, which is
the same answer for the same reason: the entry is not in play.

**No verdict changes.** Nothing here touches what any rule requires or what any finding says. The
only outcomes that move are ones that were `FactRequired` under a waiver in force and are now
`OutsideCurrentScope`, and ones that named a typed input on a request that stated nothing at all and
now name the waiver statement.

## What was rejected

**Demand first, and change `reasonable-protection` and `over-human-beings` to match.** This was the
cheaper change by an order of magnitude — two files rather than twenty-six — and the majority's
argument for it is a real argument, not a rationalisation. It was rejected because the answer it
gives is one this engine's own API documentation contradicts: it files a suspended entry under
"everything the caller still owes this engine", and names a fact on `MissingInput` that no caller
could usefully supply. Choosing it would also have required deleting the general argument already
recorded in `Rules/Protection.cs` — and that argument is right.

**Read the gate in the handler, keeping the demands there too.** This would have left every rule
signature alone: the handler calls `Waivers.Suspension` first and short-circuits, then demands as
before. Rejected because it puts § 107.205's gate in two places for one entry — the handler's copy
and the rule's — and a handler that reads the gate is a handler deciding something. The handler
layer's job here is to demand the one input the gate needs and get out of the way.

**Demand every input at the point it is owed, everywhere.** The fully lazy version: each rule
demands each input immediately before the first use that needs it. This is strictly more of the same
good thing, but it is a larger behavioural change than the issue asks for — it would alter what
several entries say on requests where no waiver is in force at all (`visual-observer-conditions`
with no visual observer used would stop demanding `Exercise`, for one) — and those are separate
questions about separate paragraphs. The fifteen rules demand their inputs immediately after the
gate, in the order the handler used to, so nothing but the gate's position moved.

**Escalate under `AGENTS.md` §6.** The issue commissions this record by name, and the question is
not one the corpus or the map is silent on in the relevant sense — it is an engine-wide convention
about what the engine tells a caller, which is precisely what `docs/decisions/` is for. #91's
implementer was right to escalate rather than settle it inside one entry's pull request; that is
what produced this.

## Consequences

- **Thirteen handlers and thirteen rules changed.** Each handler now passes its non-waiver inputs
  through unresolved; each rule takes them as nullable and demands them with `Demands.Of` after the
  gate. `Handlers.Missing` and `Demands.Missing` are one method, so a missing input is refused in the
  same words wherever it is demanded from.
- **`WaiverGateOrderingTests` pins it across the whole gated surface**, not one entry: every gated
  entry's waived request is declined `OutsideCurrentScope` citing § 107.205; none of them is
  reported as something the caller still owes;
  `The_table_is_every_entry_whose_request_carries_a_waiver_statement_of_its_own` compares the
  table with the generated request types by reflection, so a gated entry built later cannot be left
  out of it silently.
- **`CivilTwilightOperationEntryPointTests`' separating assertion is inverted rather than deleted.**
  That test's waived-request-with-`Place`-unset case is the one the issue names as the shape to
  generalise; it now asserts the decline, and asserts beside it that the same request stated with no
  waiver in force still owes `Place` by name.
- **`docs/decisions/**` is on this engine's semantic surface** (`.github/agent-policy.json`), so the
  change carrying this record needs the semantic verdict.
- **This record is the general form of what `Rules/Protection.cs` had been saying since § 107.39(b)
  was built.** That paragraph is now a citation of this file rather than an argument made in one
  entry's remarks.
