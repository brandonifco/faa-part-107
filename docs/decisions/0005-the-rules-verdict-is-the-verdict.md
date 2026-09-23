# 0005 — The rule's verdict is the verdict, and an obtainable authorization does not soften it

**Status:** accepted.

## Context

`OperationEvaluator` (issue #51) turns each entry's resolved finding into one product-facing
`RequirementState` by reading one property of that finding. Two of the rules record, beside their
verdict, that what would have met the requirement is an instrument somebody can issue:

| entry | verdict property | the other field |
|---|---|---|
| `airspace-authorized` (§ 107.41) | `AirspaceFinding.MayOperate` | `AuthorizationRequired` |
| `restricted-area-permitted` (§ 107.45) | `AreaPermissionFinding.Permitted` | `PermissionRequired` |

The first version of the evaluator read both fields for these two entries:

```csharp
AirspaceFinding finding => finding.MayOperate ? Satisfied : Obtainable(finding.AuthorizationRequired),
private static RequirementState Obtainable(bool required) => required ? ActionRequired : Violated;
```

It was reviewed and failed, and the finding was correct. `Airspace.Authorized` computes
`mayOperate = !required || (authorization.Held && authorization.ObtainedBeforeTheOperation)`, so
`!MayOperate` **entails** `required`. `ProhibitedAndRestrictedAreas.Permitted` has the identical
shape. The `Violated` branch of `Obtainable` was therefore unreachable for both entries: **there
was no input on which this evaluator would say that § 107.41 or § 107.45 was violated.**

The separating case is the plainest one the product has:

```csharp
new OperationFacts { Airspace = AirspaceClass.ClassB, AtcAuthorization = AtcAuthorization.None(caller), … }
```

The rule answers `MayOperate: false` — § 107.41 prohibits the operation. The evaluator reported
`ActionRequired` and filed it under `Outstanding`, documented as *"Everything the caller still owes
this engine before it could say more."* The caller owed nothing. The engine had answered, and the
answer was no. A product rendering `InState(Violated)` as its blocking problems showed nothing at
all for a Class B flight whose operator had stated they hold no authorization.

## Why this was a substantive defect and not a presentational one

**The field read was not the field claimed.** The evaluator's own remarks justified the split as
*"that distinction is the rule's own field, not your inference."* It is not.
`AirspaceFinding.AuthorizationRequired` is documented as *"True when the airspace is one the
section names, so prior ATC authorization is what lifts its prohibition"* — a structural fact about
which airspaces § 107.41 reaches, true of every Class B operation whether or not it complies.
`AreaPermissionFinding.PermissionRequired` likewise: *"true in a prohibited or a restricted area."*
Neither field says anything about whether the thing is still obtainable, or about how far from
compliance the operation is. Reading "obtainable, so not yet a breach" out of "the section reaches
this airspace" was the orchestrator's inference, presented as the rule's.

**It contradicted the issue that commissioned it.** Issue #51 defines the state as *"the rule names
something obtainable that the caller **has not stated**"*, and requires that *"none of which may be
folded into another… Collapsing any pair is the defect this issue exists to prevent."* Here the
caller **had** stated it: `AtcAuthorization.None(caller)` is an affirmative statement of
non-possession, made by a named person, and not stating it at all already has a state of its own,
`FactRequired`.

**It discarded a distinction a handler had taken trouble to keep.** `AtcAuthorization.NotPrior` is
documented as recorded rather than collapsed into `None` *"because the two are not the same fact"* —
the engine keeping § 107.41's word "prior". Both reached `ActionRequired` identically.

## Decision

**Every arm of the evaluator's verdict table reads the rule's own verdict property, in the
direction that property's own documentation states, and nothing else.**

```csharp
AirspaceFinding finding => Met(finding.MayOperate),
AreaPermissionFinding finding => Met(finding.Permitted),
```

`Met` is the same helper the other nine arms use. `Obtainable` is deleted. § 107.41 and § 107.45
now report `Violated` on exactly the inputs on which their own rules say the section is not met,
and `Satisfied` on the rest.

**The obtainability is not lost; it was never the evaluator's to summarise.**
`EvaluatedRequirement.Finding` carries the rule's finding whole, so a product that wants to render
*"you may still be able to obtain a prior ATC authorization"* reads
`AirspaceFinding.AuthorizationRequired` and `AirspaceFinding.Authorization` — including the
`None` / `NotPrior` distinction — from the answer it already has. What changes is that the engine
no longer speaks for the rule about what that field means.

**`RequirementState.ActionRequired` stays in the enumeration, and no rule produces it today.** That
is stated plainly on the member and here, rather than being left for a reader to discover:

- Issue #51 §2 requires the state to be distinct from `Violated` and from the rest; deleting it
  would fold "action or authorization required" into "violated" at the type level, which is the
  collapse the issue exists to prevent, and would make adding it later a breaking change.
- **What would produce it** is a rule that says so itself: a finding with a verdict of its own
  distinguishing *not met* from *not yet obtained* — the way `AreaPermissionFinding` distinguishes
  `Permitted` from `PermissionRequired`, but with the second field being about the state of the
  requirement rather than about which areas the section reaches. No rule in this engine draws that
  today. When one does, it lands in this state and nothing about `Violated` changes.
- An unproduced `ActionRequired` loses a caller nothing, because no rule models the shape it
  describes. An unreachable `Violated` lost a caller the answer "no" on two sections. The two are
  not the same defect wearing different hats, and the asymmetry is the whole reason this record
  exists.

**`UnresolvedInteraction` is kept on the same footing**, for the same reason: the kernel has five
`UnresolvedReason`s, no rule here produces the fifth, and folding it into another would be a
collapse.

## What was rejected

**Split `None` from `NotPrior`: `Violated` for a stated non-possession, `ActionRequired` for a
held-but-not-prior authorization.** This is tempting, because it would keep both states alive and
would honour the distinction `AtcAuthorization` preserves. It was rejected because the rule has
already combined those two fields into one verdict — `mayOperate = !required || (Held && Prior)` —
and re-separating them here to say that one of them is *closer to compliance* is a judgement about
what § 107.41 requires, made by the orchestrator. It is the same class of judgement as the one that
failed review, arrived at by a more careful route. Whether a non-prior authorization can be
regularised is also a fact about time and about the operation this engine has no input for: for an
operation already flown it cannot be obtained at all.

**Report `ActionRequired` whenever the rule records the thing as required.** The behaviour that
failed review.

**Delete `ActionRequired`.** Fails issue #51 §2, and makes the state un-addable without a breaking
change.

**Escalate under `AGENTS.md` §6.** Nothing here is ambiguous in the corpus or in the map. The
rules state their verdicts; the defect was that the orchestrator was not reading them.

## Consequences

- **§ 107.41 and § 107.45 can now report `Violated`, and do**, on the case the product was built
  for. `Each_verdict_is_read_from_its_own_rule_in_its_own_direction` carries the rows —
  Class B with `AtcAuthorization.None`, Class B with `AtcAuthorization.NotPrior`, a prohibited area
  with `PermissionStatement.NotGranted` — and
  `A_stated_absence_of_an_authorization_is_an_answer_and_not_a_question` pins that none of them is
  `ActionRequired` and that the obtainability is still on the finding.
- **`Outstanding` means what it says again.** It holds `ActionRequired`, `HumanAssertionRequired`
  and `FactRequired` — things the caller owes this engine — and a stated non-possession is no
  longer among them.
- **Every arm of the verdict table now has the same shape**: one property, read forward. That is
  the property a reviewer can check by reading each finding type's own documentation, which is how
  the defect above was caught.
- **This record is the pair of `docs/decisions/0004`.** That one refuses to score an assertion
  because the polarity is not recorded anywhere; this one refuses to soften a verdict that *is*
  recorded. Both say the same thing from opposite sides: the orchestrator reports what the rules
  say, and where they say nothing it says nothing rather than something kinder.
- **`docs/decisions/**` is on this engine's semantic surface** (`.github/agent-policy.json`), so
  the change carrying this record needs the semantic verdict.
