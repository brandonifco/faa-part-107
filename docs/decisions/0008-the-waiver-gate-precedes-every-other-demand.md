# 0008 — The § 107.205 waiver gate precedes every other demand a gated entry makes

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
`OperationEvaluator` at `4fe7276` — this branch's parent before it was rebased onto `684cb7d` —
against `OperationFacts.Nothing` plus one `WaiverStatement.Held` for the entry's own regulation, it
was **thirteen to two**:

| what a waived, otherwise-undescribed request was told | entries |
|---|---|
| `FactRequired`, `MissingInput` naming a typed input | `speed-within-limit` (`Groundspeed`), `altitude-within-limit` (`AltitudeAboveGroundLevelFeet`), `weather-minimums-met` (`FlightVisibilityStatuteMiles`), `operating-limitations` (`Person`), `single-aircraft` (`Person`), `airspace-authorized` (`Airspace`), `moving-vehicle-operation` (`FromMovingLandOrWaterBorneVehicle`), `moving-aircraft-operation` (`FromAMovingAircraft`), `civil-twilight-operation` (`Place`), `right-of-way` (`Encountered`), `anti-collision-lighting` (`Lighting`), `visual-line-of-sight` (`Exercise`), `visual-observer-conditions` (`Use`) |
| `OutsideCurrentScope`, citing § 107.205 | `reasonable-protection`, `over-human-beings` |

The other twelve gated entries demand no typed input at all, so the ordering is not observable on
them; they already declined.

**The divergence lived entirely in the handler layer.** Every one of the fifteen rules reached its
gate before it did anything with a non-waiver input. (Not before *everything*: the gate was the
rule's first statement in only three of the fifteen — `speed-within-limit`,
`moving-vehicle-operation` and `moving-aircraft-operation` — and the other twelve validated their
arguments first. That distinction is drawn where it bites, under "The majority's defence" below.)
What differed was whether the handler wrapped the rule's other arguments in `Demand(...)` — which
throws `ArgumentException` at the call site, before the rule is entered — or passed them through as
the caller left them so the rule could demand them after the gate. No fact in the map discriminates
the two groups: all twenty-seven carry the same `suspendedBy`.

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

**The engine's own type documentation already puts a waiver-suspended entry on the other side of
this line, in one sentence.** `OperationEvaluation.Unanswered` is *"Everything this engine cannot
answer **whatever the caller supplies**: the corpus does not settle it, the map puts it out of scope
**or a waiver suspends it**, this engine has not built it, the structured data is absent, or the
combination is unresolved. None of these is a finding about the operation."* It names this case
outright, and "whatever the caller supplies" is the whole of the argument.

**`FactRequired` claims the opposite of that sentence.** `RequirementState` groups `ActionRequired`,
`HumanAssertionRequired` and `FactRequired` as *"what the caller still owes"*, and
`OperationEvaluation.Outstanding` is *"Everything the caller still owes this engine before it could
say more."* Thirteen entries filed a waiver-suspended entry under `Outstanding` anyway, each naming
a specific fact on `MissingInput` — so a product rendering that list would ask somebody for their
`Person` for a rule it had just told them does not apply.

**Within this engine's model of the gate**, there is then no `Person`, no `Use`, no `Lighting`, no
`Place` the caller could supply that would make the entry say more, because the entry is not in
play. The qualifier is doing real work and is not a hedge. § 107.205(a) and (c) each carry a
proviso, in the corpus's own words: *"However, no waiver of this provision will be issued to allow
the carriage of property of another by aircraft for compensation or hire."* And § 107.200(d)(1)
makes a certificate authorise deviation only "to the extent specified".

**This engine reads none of that**, and could not apply it from the inputs it has. The proviso's
fact — carriage of property of another *by aircraft* for compensation or hire — is on no request in
this engine. The nearest thing is
`MovingVehicleOperationRequest.TransportingAnotherPersonsPropertyForCompensationOrHire`, and it is
a **different** fact: it is § 107.25(b)'s own condition, about an operation from a moving land or
water-borne vehicle, while the proviso is about carriage by aircraft. § 107.205(a) suspends the
whole of § 107.25 — both `moving-vehicle-operation` and `moving-aircraft-operation` — and that
input is on one of the two requests; § 107.205(c)'s identical proviso reaches
`visual-line-of-sight`, whose request has nothing of the kind at all. So the engine records the
waiver statement the caller makes and does not second-guess its extent, which
`docs/decisions/0001` and rules-factory decision 0021 settled before this record existed. That is
unchanged here, and changing it is its own issue: a gate that read the provisos would answer a
question about what the Administrator may issue, from a corpus that states the condition and not
the test.

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

- The arguments were already ordered, inside the rule. **No rule read a demanded input before its
  gate**: in all fifteen, the first thing done with a non-waiver input was done after
  `Waivers.Suspension` had run. That is the narrow true claim, and it is worth stating narrowly,
  because the gate was the rule's *first statement* in only **three** of the fifteen —
  `speed-within-limit`, `moving-vehicle-operation` and `moving-aircraft-operation`. The other
  twelve validated their arguments first, and three of those validations were observable, not
  merely defensive: `Altitude.Within`'s `ThrowIfNegative(altitudeAboveGroundLevelFeet)`,
  `Weather.MinimumsMet`'s `ThrowIfNegative(flightVisibilityStatuteMiles)` and
  `MultipleAircraft.AtTheSameTime`'s `ThrowIfNullOrWhiteSpace(person)` — the same three this record
  moves behind the gate below, which is why the claim has to be about *demands* and not about
  statement order. So the convention was never "all the arguments at once, and then the rule": the
  rule already had an order, and the only question was where the demands sat relative to it. (The
  handlers demanded the waiver statement **last** in thirteen of the fifteen, which is why those
  thirteen named a typed input on a request that stated nothing at all; that is the accident this
  record removes, not an ordering anyone chose.)
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
demanded and the entry demands nothing until the gate has run. Three checks moved with their
demands, and this is the complete list:

| rule | check | what a waived request with that value used to get | and gets now |
|---|---|---|---|
| `Altitude.Within` | `ArgumentOutOfRangeException.ThrowIfNegative(altitudeAboveGroundLevelFeet)` | `ArgumentOutOfRangeException`, which `OperationEvaluator.Evaluate` rethrows and which abandons the whole evaluation | `OutsideCurrentScope` citing § 107.205 |
| `Weather.MinimumsMet` | `ArgumentOutOfRangeException.ThrowIfNegative(flightVisibilityStatuteMiles)` | `ArgumentOutOfRangeException`, likewise abandoning the whole evaluation | `OutsideCurrentScope` citing § 107.205 |
| `MultipleAircraft.AtTheSameTime` | `ArgumentException.ThrowIfNullOrWhiteSpace(person)` | `ArgumentException`, reported `FactRequired` naming `person` | `OutsideCurrentScope` citing § 107.205 |

A caller who states a waiver in force and a negative altitude is now declined rather than thrown at,
which is the same answer for the same reason: the entry is not in play. Each keeps its original
`paramName`, so where no waiver is in force nothing about any of them moved.

**No verdict changes.** Nothing here touches what any rule requires or what any finding says. The
only outcomes that move are ones that were `FactRequired` under a waiver in force and are now
`OutsideCurrentScope`, and ones that named a typed input on a request that stated nothing at all and
now name the waiver statement.

## What was rejected

**Demand first, and change `reasonable-protection` and `over-human-beings` to match.** This was the
cheaper change by an order of magnitude — two files rather than twenty-six — and the majority's
argument for it is a real argument, not a rationalisation. Stated at its strongest: the request type
is the entry's contract, every property on it marked *Required* is a fact the entry needs, and
demanding them all at the door makes "what does this entry want from me?" answerable without the
caller knowing anything about § 107.205 — one list, one order, independent of any other answer. It
was rejected because the answer it gives is one this engine's own API documentation contradicts:
`OperationEvaluation.Unanswered` says a waiver-suspended entry cannot be answered *whatever the
caller supplies*, and demand-first files that same entry under "everything the caller still owes
this engine" with a fact named on `MissingInput`. Choosing it would also have required deleting the
general argument already recorded in `Rules/Protection.cs` — and that argument is right.

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
- **Two of those tests need a mutation of their own, and have one.** The fifteen per-entry mutations
  that move a handler's `Demand(...)` back in front of the gate cannot redden
  `Stated_that_no_waiver_is_in_force_the_same_request_is_refused_for_the_input_the_entry_owes` **by
  construction** — on the no-waiver path re-wrapping an input throws the same `ArgumentException`
  with the same `ParamName` — and they cannot touch the reflection test at all, which reads no
  handler. So that test is reddened instead by a rule that stops demanding (`Speed.Within`
  defaulting the groundspeed) and by a rule that demands out of order (`Compliance.CompliedWith`
  swapping `Person` and `Groundspeed`), and the reflection test by deleting a row from its own
  table. This matters more than the usual bookkeeping: those two tests are what stop this record
  being read as the fully lazy change, and what closes the "built later, left out silently" hole —
  the two things the rest of the argument leans on.
- **Two of this file's five tests are censuses, and are recorded where a census belongs.**
  `The_table_is_every_entry_whose_request_carries_a_waiver_statement_of_its_own` and
  `Nothing_a_stated_waiver_suspends_is_reported_as_something_the_caller_still_owes` each assert over
  every gated entry — one with `Assert.All` over all twenty-seven — so neither belongs to a single
  entry, and both are named by `tests/FaaPart107.Tests/cross-cutting-mutations.json` rather than by
  an entry's overlay row (`docs/decisions/0007-a-test-that-belongs-to-no-entry-records-its-mutation-beside-the-tests.md`:
  filing a census under one entry "would make the entry's packet claim evidence it does not have").
  The three per-entry theories stay in the overlay, one row per entry they cover.
- **`CivilTwilightOperationEntryPointTests`' separating assertion is inverted rather than deleted.**
  That test's waived-request-with-`Place`-unset case is the one the issue names as the shape to
  generalise; it now asserts the decline, and asserts beside it that the same request stated with no
  waiver in force still owes `Place` by name.
- **Fifteen public rule signatures took nullable parameters**, which is a change on the rules'
  surface and not only inside it. A caller who reaches a rule directly and passes `null` for an
  input now gets `ArgumentException` — "the caller did not state it" — where it used to get
  `ArgumentNullException` from a `ThrowIfNull` guard; and `decimal` → `decimal?` on
  `Altitude.Within`, `Weather.MinimumsMet` and `Compliance.CompliedWith` is a binary-breaking
  signature change. Through `EntryPoints` and `Registry`, which is how the engine is meant to be
  reached and how every test reaches it, nothing moves: those callers were passing the request's
  own nullable property all along.
- **Thirty mutation records elsewhere quote code this change renamed or deleted, and this is the
  list.** Making each rule's demanded value a new local meant renaming the use sites — `lighting`
  became `stated`, `place` became `where`, `use` became `stated`, `fromAMovingAircraft` became
  `stated`, `person` became `asked`, `flightVisibilityStatuteMiles` became `miles`, `authorization`
  became `held`, `exercise` became `exercised` — and thirteen handler `Demand(...)` calls were
  deleted outright. Every record below was true when it was observed and none of its *claims*
  changes; what changes is that re-running one means translating an identifier, or moving the
  substitution from a handler to the rule. The list lives here rather than in the pull request
  because `docs/decisions/0007` rejects the pull request as a place for mutation evidence in terms
  — it is read by no check, is not diffed against the test it describes, and does not travel with
  the code — and after merge this is the only translation key there is.

| entry | test whose record quotes it | the fragment |
|---|---|---|
| `airspace-authorized` | `AirspaceAuthorizedEntryPointTests.An_ATC_authorization_that_was_not_prior_does_not_satisfy_107_41` | `var mayOperate = !required || (authorization.Held && authorization.ObtainedBeforeTheOperation);` |
| `airspace-authorized` | `AirspaceAuthorizedEntryPointTests.Each_airspace_107_41_names_may_be_operated_in_with_prior_ATC_authorization_citing_107_41` | `var mayOperate = !required || (authorization.Held && authorization.ObtainedBeforeTheOperation);` |
| `airspace-authorized` | `AirspaceAuthorizedEntryPointTests.Without_an_ATC_authorization_statement_it_refuses_rather_than_infer_one` | `Demand(request.Authorization,` |
| `airspace-authorized` | `AirspaceAuthorizedEntryPointTests.Without_an_airspace_it_refuses_rather_than_infer_one` | `Demand(request.Airspace,` |
| `altitude-within-limit` | `AltitudeWithinLimitEntryPointTests.Without_a_structure_statement_it_refuses_rather_than_assume_there_is_no_structure` | `Demand(request.Structure,` |
| `altitude-within-limit` | `AltitudeWithinLimitEntryPointTests.Without_an_altitude_it_refuses` | `Demand(request.AltitudeAboveGroundLevelFeet,` |
| `anti-collision-lighting` | `AntiCollisionLightingEntryPointTests.An_aircraft_with_no_anti_collision_lighting_does_not_meet_it_and_no_fact_about_lighting_it_lacks_is_demanded` | `if (!lighting.Lighted)` |
| `anti-collision-lighting` | `AntiCollisionLightingEntryPointTests.The_determination_is_asked_only_where_the_intensity_was_reduced` | `rate => lighting.IntensityReduced` |
| `anti-collision-lighting` | `AntiCollisionLightingEntryPointTests.Without_the_facts_the_clause_turns_on_it_refuses_rather_than_assume_them_including_through_the_dictionary_dispatch` | `Demand(request.Lighting, request.EntryId, nameof(request.Lighting))` |
| `civil-twilight-operation` | `CivilTwilightOperationEntryPointTests.In_Alaska_the_definition_is_civil_twilight_alaskas_and_this_entry_declines_MissingRulesData_citing_107_29_c_3` | `place == OperationPlace.InAlaska` |
| `civil-twilight-operation` | `CivilTwilightOperationEntryPointTests.The_places_and_the_periods_107_29_c_names_are_closed_sets_positively_stated_and_neither_is_a_default` | `Demand(request.Place,` |
| `civil-twilight-operation` | `CivilTwilightOperationEntryPointTests.Without_the_facts_the_paragraph_turns_on_it_refuses_rather_than_assume_them_including_through_the_dictionary_dispatch` | `Demand(request.Lighting,` |
| `civil-twilight-operation` | `OperationEvaluatorTests.A_stated_place_can_move_an_entry_into_what_the_engine_cannot_determine` | `if (place == OperationPlace.InAlaska)` |
| `moving-aircraft-operation` | `MovingAircraftOperationEntryPointTests.Without_a_statement_whether_the_aircraft_is_moving_it_refuses_rather_than_infer_one` | `Demand(request.FromAMovingAircraft,` |
| `moving-vehicle-operation` | `MovingVehicleOperationEntryPointTests.Without_the_facts_the_rule_turns_on_it_refuses_rather_than_infer_them` | `Demand(request.FromMovingLandOrWaterBorneVehicle,` |
| `operating-limitations` | `OperatingLimitationsEntryPointTests.An_ordinary_clear_air_operation_is_not_reported_as_breaking_the_operating_limitations` | `Weather.MinimumsMet(flightVisibilityStatuteMiles, cloud, waiver)` |
| `operating-limitations` | `OperatingLimitationsEntryPointTests.Both_persons_the_introductory_text_names_are_bound_and_are_answered_alike` | `Weather.MinimumsMet(flightVisibilityStatuteMiles, cloud, waiver)`; `new OperatingLimitationsFinding(person, limitations, waiver)` |
| `operating-limitations` | `OperatingLimitationsEntryPointTests.Without_the_facts_the_limitations_are_tested_against_it_refuses_rather_than_assume_them` | `Demand(request.Person,` |
| `right-of-way` | `RightOfWayEntryPointTests.Without_the_object_or_the_pass_it_refuses` | `Demand(request.Encountered,` |
| `single-aircraft` | `SingleAircraftEntryPointTests.Without_the_aircraft_the_person_is_engaged_with_it_refuses_rather_than_infer_them` | `Demand(request.Engagements,` |
| `single-aircraft` | `SingleAircraftEntryPointTests.Without_the_person_it_refuses_rather_than_infer_one` | `Demand(request.Person,` |
| `visual-line-of-sight` | `VisualLineOfSightEntryPointTests.The_dictionary_dispatch_answers_and_refuses_the_same_way` | `Demand(request.Exercise, request.EntryId, nameof(request.Exercise))` |
| `visual-observer-conditions` | `VisualObserverConditionsEntryPointTests.A_waiver_of_107_31_leaves_paragraph_b_undetermined_and_is_carried_as_the_constituents_own_account` | `LineOfSight.Maintained(exercise, visualLineOfSightWaiver, assertions)` |
| `visual-observer-conditions` | `VisualObserverConditionsEntryPointTests.No_visual_observer_used_means_107_33_states_no_requirement_and_no_constituent_is_asked` | `if (use != VisualObserverUse.Used)` |
| `weather-minimums-met` | `OperationEvaluatorTests.A_value_a_rule_refuses_is_a_fault_and_is_not_dressed_as_a_fact_the_caller_owes` | `ThrowIfNegative(flightVisibilityStatuteMiles)` |
| `weather-minimums-met` | `WeatherMinimumsMetEntryPointTests.A_negative_stated_figure_is_refused` | `ArgumentOutOfRangeException.ThrowIfNegative(flightVisibilityStatuteMiles);` |
| `weather-minimums-met` | `WeatherMinimumsMetEntryPointTests.Two_operations_differing_only_in_the_stated_visibility_the_finding_does_not_print_are_different_outcomes` | `new WeatherMinimumsFinding(flightVisibilityStatuteMiles, …)` |
| `weather-minimums-met` | `WeatherMinimumsMetEntryPointTests.While_a_waiver_of_107_51_is_stated_in_force_it_declines_OutsideCurrentScope_citing_107_205_and_records_the_statement` | `NoCloudToMeasureFrom(flightVisibilityStatuteMiles, cloud, …)` |
| `weather-minimums-met` | `WeatherMinimumsMetEntryPointTests.Without_a_stated_figure_it_refuses_rather_than_assume_one` | `Demand(request.Cloud,`; `Demand(request.FlightVisibilityStatuteMiles,` |
| cross-cutting | `OperationEvaluatorTests.The_cost_recorded_in_decision_0004_is_the_cost_the_engine_actually_has` | `rate => lighting.IntensityReduced` |

  **How the list was made, and what the method cannot see.** Every backtick-quoted fragment in
  `corpus-map.overlay.json` and `cross-cutting-mutations.json` was taken, and those present in
  `src/` and `tests/` at `684cb7d` and absent after this change were kept. It is a lower bound, not
  a census: a fragment that elides with `...` is only matched up to the ellipsis, so one whose
  elision is in the *middle* is missed — the two `weather-minimums-met` rows above were found by
  hand for exactly that reason, and they are the worst of the set, because
  `flightVisibilityStatuteMiles` is `decimal?` now and those fragments no longer type-check. A
  fragment that is the mutation's *inserted* code rather than the original is also skipped, since
  it is not meant to be in the tree. Four further records were found the same way and **are**
  reworded on freshly observed mutations rather than left: `speed-within-limit`'s
  `Without_a_groundspeed_it_refuses`, whose substitution moves from the handler to the rule, and
  the three `moving-aircraft-operation` rows that quoted `Prohibited: fromAMovingAircraft`.

  **The alternative, which would have left every one of these exact.** Name the nullable
  *parameter* `statedLighting` and the demanded local `lighting`: all thirteen rule bodies stay
  byte-identical, and the diff is a signature line and a demand line per rule. It was not taken
  because it renames public parameters — a named-argument source break on top of the
  binary-breaking `decimal` → `decimal?` above — and because the demanded local reads better under
  the name the paragraph uses while the caller's unresolved input is the one that wants an
  adjective. That is a judgement about naming, and a reviewer who weighs the translation cost
  higher should have it as a follow-up; it is mechanical.
- **`docs/decisions/**` is on this engine's semantic surface** (`.github/agent-policy.json`), so the
  change carrying this record needs the semantic verdict.
- **This record is the general form of what `Rules/Protection.cs` had been saying since § 107.39(b)
  was built.** That paragraph is now a citation of this file rather than an argument made in one
  entry's remarks.
