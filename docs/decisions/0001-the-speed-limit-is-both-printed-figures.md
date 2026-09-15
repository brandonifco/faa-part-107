# 0001 — The speed limit is both printed figures, and the waiver is the caller's statement

**Status:** accepted.

## Context

§ 107.51(a): *"The groundspeed of the small unmanned aircraft may not exceed 87 knots (100 miles
per hour)."* The map (`RulesFactory.Maps.FaaPart107` 2.0.0) splits it in two, like every figure
in § 107.51:

- `speed-limit`, `kind: value`, `clarity: ambiguous`, `fate: unresolved`, `RequiresInterpretation`.
  Its question: the two figures are not equal, 87 knots being about 100.12 miles per hour, and
  the corpus does not say which governs.
- `speed-within-limit`, `kind: operation`, `clarity: clear`, `dependsOn: [speed-limit]`. Its note
  asks for the cases at, just below and just above 87 knots, and between 100 mph and 87 knots.

Both carry `suspendedBy: [waivable-regulations]`, the `scope: out` entry for § 107.205, which
lists § 107.51 among the regulations a certificate of waiver may authorize deviation from
(rules-factory decision 0021).

The correspondence table (rules-factory `docs/corpus-map.md`) decides the rows. Row 5 makes an
operation whose value dependency is unimplemented answer `MissingRulesData`, so
`speed-within-limit` can only resolve once `speed-limit` is `implemented`. Row 6 makes an
`implemented` entry with `fate: unresolved` mean *built, and declines the stated case*.

## Decision

**`speed-limit` resolves to both figures, as printed, and chooses neither.** Its
`GroundspeedLimit` value carries 87 knots and 100 miles per hour and cites `§ 107.51(a)`. That
is what the corpus states, and the backlog item asks for exactly that: *"every figure and unit"*.
The case that turns on the question is a request for the limit **as one figure in one unit**
(`SpeedLimitRequest.AsOneFigureIn`): 87 knots if the knots figure governs, about 86.9 knots if
the other does. That request declines `RequiresInterpretation`, citing `§ 107.51(a)`.

**`speed-within-limit` resolves wherever both readings agree, and declines where they do not.**
A groundspeed exceeding neither figure is within the limit, and one exceeding both is beyond it,
whichever figure governs. Both resolve, citing `§ 107.51(a)`. A groundspeed above 100 mph and at
or below 87 knots exceeds one figure and not the other. That is `speed-limit`'s question reached
through `dependsOn`, so the decline is `RequiresInterpretation` and cites `speed-limit`'s locator.
That includes exactly 87 knots, and 86.9 knots. The engine does not pick a reading. The conservative
reading, lowest figure wins, would be a decision the map has not recorded.

**The units are compared exactly.** Part 107 does not define the knot or the mile per hour. The
engine uses the international definitions, 1 knot = 1.852 km/h and 1 mph = 1.609344 km/h, which
are exact decimals. The comparison is `decimal` arithmetic with no rounding. The map's question
reads the units the same way (*"87 knots is 100.12 miles per hour"*). If a corpus defining them
were ever admitted, this would be a `definedElsewhere` question rather than an engine constant.

**Whether a waiver is in force is the caller's statement, demanded and never defaulted.** Both
requests carry a `WaiverStatement`: the regulation (`§ 107.51`), whether a certificate of waiver
authorizing deviation from it is in force, who says so, and, optionally, the certificate. A
request without one throws `ArgumentException`, because a missing input is the caller's error
and not a gap in the corpus. A statement that a waiver is in force makes the entry answer
`OutsideCurrentScope`, citing `§ 107.205` (`waivable-regulations`), with the statement and its
author in `Attempted`. A statement that none is in force evaluates the rule normally and travels
with the result (`GroundspeedLimit.Waiver`, `GroundspeedFinding.Waiver`). A statement about
another regulation is refused.

## What was rejected

**Leave `speed-limit` declining everything.** Row 6 allows it, but it throws away the two figures
the corpus does state, and `speed-within-limit` could then never resolve anything, even 50 knots.

**Let the knots figure govern because it comes first, or let the parenthetical govern because it
is lower.** Either is a reading. The map records the question as `unresolved`, and an engine that
picks silently is what `ambiguity.fate` exists to prevent.

**Default the waiver to "none held".** That convicts a holder. Defaulting to "held" excuses
everyone (decision 0021).

**Model the waiver as `kind: assertion` through `RuleRequest.Assert`.** The map has no assertion
entry for it, and 0021 rules it is not one. It is an input declared on each suspended entry's
request, as #93 provides for inputs.

## Consequences

**Two entries share one locator, so a citation cannot say which of them declined.** Both cite
`§ 107.51(a)`. A mutation that made `speed-within-limit`'s between-figures decline cite its own
locator instead of `speed-limit`'s left every test green, because the two `SourceLocator`s are
equal. `Attempted` names the entry whose question it is, and the test asserts that. The kernel's
`UnresolvedResult` has no field for the entry id.

**The kernel's `UnresolvedResult` has no place for a caller's statement.** The waiver statement
on a decline is recorded as text in `Attempted`. A resolved result records it as a typed field.
