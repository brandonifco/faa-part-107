# 0006 — A composite declines in its own name, and quotes the constituent it asked

**Status:** accepted.

## Context

A **composite** here is an entry whose answer is built out of the entries its `dependsOn` names —
a conjunction, a list of excepted cases, an exception clause. Several of them have to report that a
constituent did not settle the question, and say so to a caller who asked about the composite:

| entry | file | what it reports |
|---|---|---|
| `operating-limitations` (§ 107.51 intro) | `Rules/Compliance.cs` | each constituent's verdict; a decline where one is undetermined |
| `visual-observer-conditions` (§ 107.33) | `Rules/Observers.cs` | same, over § 107.33's requirements |
| `over-human-beings` (§ 107.39) | `Rules/Overflight.cs` | same, over § 107.39's three excepted cases |
| `civil-twilight-operation` (§ 107.29(b)-(c)) | `Rules/Twilight.cs` | same, over its constituents |
| `weather-minimums-met` (§ 107.51(c)-(d)) | `Rules/Weather.cs` | a decline on `prominent-objects`, which declines always |
| `right-of-way` (§ 107.37(a)) | `Rules/Yielding.cs` | a decline on `well-clear`, which declines always |

`speed-within-limit` (§ 107.51(a), `Rules/Speed.cs`) is the nearest relative and not quite a member:
`speed-limit` **resolves**, to both printed figures, and the openness is inside the value it
resolved. Decision 0001 nonetheless gave it the decline shape below — the composite's own wording,
naming `speed-limit`'s question and citing `speed-limit`'s locator — and issue #78 cites it as the
precedent for the two entries this record is about.

The first four had converged on one shape without it being written down. The last two, which merged
together (#75, #76), each had half of it, and neither half was wrong on any input reachable that day
— which is why both passed review:

|  | `right-of-way` | `weather-minimums-met` |
|---|---|---|
| asked the declining constituent at runtime | **yes**, `WellClear(waiver)` | **no** — never called `Prominence.Objects` |
| whose decline was emitted | the constituent's, **verbatim** | its **own**, naming the constituent |

Both halves cost something real.

**A `dependsOn` edge nothing traverses is decorative.** `weather-minimums-met` hard-coded the fact
that `prominent-objects` is open — *"the map entry 'prominent-objects' holds open which objects are
'prominent'"* — from a constant. If `prominent-objects` were ever settled, by a new map version or
an owner's ruling, this entry would go on declining with a message asserting something false, and
**no test in this repository would go red.** The engine would be stating a fact about the corpus
that the corpus no longer supports.

**A decline handed back verbatim carries no trace of what was asked.** `right-of-way` returned
`well-clear`'s `UnresolvedResult` unchanged, and its test pinned that they were *equal*: same
reason, same `Attempted`, same locator. A caller who asked whether § 107.37(a) prohibited a pass
over an aircraft was told that part 107 does not define "well clear", with nothing saying that
question had been reached from anywhere. `docs/decisions/0001` had already recorded the opposite
shape for `speed-within-limit` → `speed-limit`, for exactly this situation.

Issue #78 asked for the two to be harmonised and for the result to be written down. The #83 review
of `operating-limitations` added a second question to it, which this record answers below: what a
composite may quote from its constituent's account without that counting as propagation.

## Decision

**A composite asks its constituent, and what the constituent answers on the request in hand is what
decides.** No composite records in advance which of its constituents can resolve. An entry whose
question the map later settles is followed from the call, and no test of the composite has to
change for that to happen.

**Where the constituent's openness decides the outcome, the composite emits its own
`UnresolvedResult`**, with these four parts:

- **Reason** — the constituent's, as it gave it on this request. Not a constant.
- **Locator** — the constituent's. The question that blocks the answer is the constituent's
  question, and a citation should lead to where that question is.
- **`Attempted`** — the composite's own, and this is what makes the decline the composite's: it
  names the entry the caller asked about, the situation it was asked about, and the constituent
  whose question blocks it, by id and by citation.
- **The constituent's own account, quoted inside that `Attempted`.**

**Handing back the constituent's `UnresolvedResult` is forbidden.** Not its object, and not a
reconstruction equal to it. A composite's decline must be distinguishable from its constituent's on
the bytes, because a product layer above this engine has to tell "we could not answer what you
asked" from "we could not answer something three levels below what you asked", and because a
`right-of-way` decline that is byte-identical to a `well-clear` decline is a decline that has
forgotten the question.

**Quoting the constituent's account is not propagation, and is wanted.** The distinction is
between *being* the constituent's result and *containing* it. `UnresolvedResult.Attempted` is a
string, the composite writes it, and embedding the constituent's `Attempted` in it — introduced by
words of the composite's own, after the composite has named itself and named the constituent —
keeps the chain visible without making the two results interchangeable. A caller who receives

> decide whether the map entry `weather-minimums-met` is met on a stated flight visibility of 10
> statute miles: … and the map entry `prominent-objects` [§ 107.51(c)] did not answer which objects
> are "prominent" …; what `prominent-objects` recorded: decide which objects the map entry
> `prominent-objects` counts: part 107 does not define "prominent" …

can see both that the question it asked was `weather-minimums-met`'s and that the openness
originates in `prominent-objects`. That is what the two merged composites lost in opposite ways.

**The entry a composite cites and names is the one its own `dependsOn` names, never the one further
down.** This is settled by the map and not by preference. `operating-limitations.dependsOn` names
`weather-minimums-met` and does **not** name `prominent-objects`; for `operating-limitations` to
cite § 107.51(c) would be the entry asserting a relationship the map does not give it, and it would
go stale the moment its constituent's own dependencies changed. "The originating entry" is also not
well defined past depth two: each level picks a first-in-`dependsOn` blocker, so "originating"
means "first-of-first-of-first", an artifact of traversal order rather than a citation. The
quotation above is how depth is carried; the citation stays at depth one.

**Where the map gives the constituent no verdict type, a constituent that resolves is an ordinary
case, not an impossibility.** `prominent-objects` and `well-clear` are `Resolution<object>`: there
is no verdict in them for a composite to read. A ruling that settled either question would have to
give the entry a verdict type first, and this engine would be re-produced for it. So the composite records
the case as unsettled with that entry's account beside it — the shape `over-human-beings` already
gives such a constituent (`NoVerdictToRead`) — rather than guessing a verdict out of an `object` or
throwing. `right-of-way` threw `UnreachableException` on that branch; **that is removed**, because
once the constituent's result is what decides, an entry that resolves is a case the composite met
and not a broken invariant.

## What was rejected

**Leave the two as they were, since neither is wrong on a reachable input.** This is true and is
the reason both passed review. It is also the argument for never fixing a latent defect, and the
`weather-minimums-met` half had the property that the defect would arrive *silently*: a map change
would make the engine assert something false with every test green. A decline that cannot be
falsified by the thing it describes is not evidence of anything.

**Make the composite propagate, uniformly.** It is one line, it is obviously consistent, and it
loses the caller's question. It also does not survive depth: `operating-limitations` above
`weather-minimums-met` above `prominent-objects` would answer a question about § 107.51's
introductory text with a paragraph about the word "prominent", three levels down.

**Have the composite cite the originating entry rather than its immediate constituent** — for
`operating-limitations`, § 107.51(c). Rejected on the map: the edge does not exist, and
"originating" is not well defined past depth two (above).

**Re-derive the constituent's verdict in the composite.** Every one of these composites reads a
verdict the constituent resolved, or records that it resolved none; none recomputes one. A
composite that re-derived would be a second implementation of its constituent's rule, and the
figures would stop moving when the constituent's moved.

**Settle here what reason a composite gives when a constituent resolved a value it cannot read a
verdict from.** That is issue #92's question, across all the composites that have the fallback, and
answering it in passing here would pre-empt a decision that has its own issue. This record says
only that such a case is *ordinary* — reported, not thrown — and leaves the reason to #92.

## Consequences

- **`Rules/Weather.cs` now asks `Prominence.Objects` wherever § 107.51(c) is reached**, and both of
  its declines — the one for a measured cloud and `NoCloudToMeasureFrom` — are built from what that
  entry answered. The dependency is asked in the decline builders and not in `MinimumsMet`, because
  the one finding this entry resolves is settled by § 107.51(d) alone and § 107.51(c) is never
  reached there. Which cases resolve did not move.
  `The_decline_follows_prominent_objects_own_answer_and_is_not_that_answer_handed_back` pins it,
  and goes red when `Prominence.Objects` resolves.
- **`Rules/Yielding.cs` builds `right-of-way`'s own decline**, naming `right-of-way` and
  `well-clear`, carrying `well-clear`'s reason, citing `well-clear`'s § 107.37(a), and quoting its
  account. The seven of sixteen cells the two enumerations decide are untouched: the change is
  confined to the nine that reach the exception.
  `An_object_107_37_a_names_passed_over_under_or_ahead_declines_with_this_entrys_own_decline_on_well_clears_question`
  replaces the assertion that the two results were equal with the assertion that they differ.
- **A shared locator still cannot tell two entries apart, and `Attempted` is what does.**
  `right-of-way` and `well-clear` both cite § 107.37(a); `weather-minimums-met`'s decline cites
  § 107.51(c), which `prominent-objects` and `visibility-minimum` share. Decision 0001 recorded the
  same blind spot for speed. Every test here asserts the naming on `Attempted`, not on the
  citation, because a mutation that swapped one of those locators for the other would otherwise
  stay green.
- **`operating-limitations` carries its constituent's account forward, and so does
  `preflight-actions`.** `LimitationOutcome.Account` holds `weather-minimums-met`'s full `Attempted`
  on a decline as much as on a resolved finding — `Compliance.Outcome` fills it from
  `unresolved.Attempted` — but until **issue #103** (PR #114) that account never reached the caller:
  `Compliance.Undetermined` built its own `Attempted` from each undetermined constituent's
  `Entry.Id` and `Entry.Locator.Citation` and nothing else, and the array of outcomes was local and
  discarded. So a caller received "`weather-minimums-met` [§ 107.51(c)-(d)] did not resolve" and the
  fact that the openness originates in `prominent-objects` was gone — two hops of loss at depth
  four. It now quotes `limitation.Account` for every undetermined constituent, after this entry has
  named itself and named that constituent, and `Preflight.Undetermined` does the same for every
  unanswered obligation: the gap there was identical and pinned in neither direction, and the owner
  widened #103 to it. Issue #78 was scoped to the two composites that disagreed and explicitly not
  to the four that already had the shape, which is why this record named the gap rather than closing
  it; what it settled is that it *is* a gap, and what closing it must not do (become propagation).
  `The_decline_is_this_entrys_own_and_not_the_constituents_handed_back` now pins the carrying rather
  than the loss — `Assert.DoesNotContain("prominent-objects", mine.Attempted)` has become
  `Assert.Contains("prominent-objects", mine.Attempted)` beside
  `Assert.Contains(constituent.Attempted, mine.Attempted)`, with
  `Assert.NotEqual(constituent.Attempted, mine.Attempted)` unchanged next to them, because containing
  the constituent's account is not being its result.
- **The quotation is one hop, taken by each composite, and transitive only by composition.** This is
  #103's criterion 5, decided there and recorded here because it governs every composite and not
  only the two that carry it today. A composite quotes the account of the entries its own
  `dependsOn` names and reads nothing below them. Depth arrives anyway, because the constituent
  built its account the same way: `operating-limitations` quoting `weather-minimums-met` once
  carries `prominent-objects`' own words to a caller at depth four without ever reading that entry.
  Walking the graph and quoting transitively is refused for the same reason this record refuses the
  deeper *citation* — the edge is not in the map, and "originating" is not well defined past depth
  two — so one hop is both the least and the most a composite may quote: it is the only hop the map
  authorises. Nothing is truncated, deliberately. What bounds the length is the map — one quotation
  per constituent that did not resolve, each bounded in turn by its own `dependsOn` — and a cutoff
  chosen in a rule would drop the depth the quotation exists to carry, silently.
- **Three composites still do not quote, and each is its own issue.** `over-human-beings`,
  `visual-observer-conditions` and `civil-twilight-operation` build their declines from ids,
  citations and reasons and discard the constituent's account — the first two hold one on their
  outcome records, and `civil-twilight-operation` has no field for it at all. #103's sweep put each
  of the three to its own entry point on a declining request and confirmed at runtime that the
  constituent's `Attempted` does not appear in the composite's. None was changed on that branch,
  because fixing three entries the issue did not name would have made a one-issue change
  unreviewable; the bullet above is the shape each of them is owed.
- **Nothing about either entry's regulatory reading moved.** No case that resolved before declines
  now, and no case that declined before resolves. What changed is what a decline says and where its
  facts come from.
- **Verbatim propagation survives on unreachable arms, and is a defect this record does not reach.**
  `Weather.MinimumsMet` and `Speed.Within` both pass `Resolution<T>.FromUnresolved` as the
  `onUnresolved` arm of a `Match` over a value constituent (`visibility-minimum`, `cloud-clearance`,
  `speed-limit`), which hands that constituent's `UnresolvedResult` straight back. Those arms are
  dead, but not because the waiver gate is a constituent's only way to decline: `Clouds.Clearance`
  and `Speed.Limit` each have a second, behind an optional parameter — `asOneDistance: true` and
  `asOneFigureIn: not null`, both `RequiresInterpretation` — and only `visibility-minimum` declines
  on the gate alone. They are dead because each composite runs its own waiver gate first and then
  calls its constituent at the default (`Clouds.Clearance(waiver)`, `Speed.Limit(waiver)`), never
  asking the question that second decline answers. They are still the forbidden shape, waiting for a
  call site that asks it or for a constituent that gains a third way to decline, and they are outside
  issue #78's acceptance criteria. Closing that is **issue #104**; recorded here so that the next
  agent to touch either file does not have to rediscover it.
- **`docs/decisions/**` is on this engine's semantic surface** (`.github/agent-policy.json`), so the
  change carrying this record needs the semantic verdict.
