# The Rules API phase — development guide

**Status:** the guiding document for the next phase of work. It is not a decision record and it
does not amend [`AGENTS.md`](../AGENTS.md); where the two disagree, the contract wins (§2 there).
The decisions this guide leaves open are listed in §13. Each is made as a record in
`docs/decisions/` (here) or in the new repository's own decision log, never by an implementer in
code.

---

## 1. What this phase is

This engine already answers the question a commercial product sells:

```text
OperationFacts  →  OperationEvaluator.Evaluate()  →  OperationEvaluation
```

`OperationEvaluation` holds one `EvaluatedRequirement` per map entry: forty-seven of them, in the
map's order. Each one has its `RequirementState`, its citations, an explanation, a finding, a
decline reason and decline citation, the assertion it owes and who may make it, and the input it is
missing. The evaluation also carries the engine's identity (`EvaluatedBy`, from
`Ruleset.Identity`) and its recorded provenance (`ProvenanceJson()`). None of this is reduced to
one boolean.

**This phase puts HTTP in front of that, in a separate repository, without changing what it
says.** The deliverable is a host, working name `brandonifco/rules-api`, with FAA Part 107 as its
first installed engine. Its module boundary must let a second engine plug in without rewriting the
host.

The one sentence the architecture has to keep true:

> **The platform knows that engines exist. It does not know that FAA regulations exist.**

## 2. Principles that decide most questions

1. **`faa-part-107` stays a pure, deterministic regulatory engine.** Nothing is added to it: no
   ASP.NET, no JSON contracts, no clock, no persistence and no customer identifiers. The only change
   this phase may ask of it is packaging (§4.3), and that goes through the ordinary rails: an issue,
   a worktree and a pull request.
2. **Absence is not falsity, at every layer.** A property the caller left out reaches
   `OperationFacts` as `null`. The API never defaults a boolean to `false`, a number to `0`, a
   waiver to "none held", an assertion to "holds", or `AsOf` to today. The engine reports
   `FactRequired`, and the API passes that on.
3. **All twelve states survive the wire.** No state is merged with another or renamed into one.
   None is summarised into `compliant`, `legal`, `approved` or `missionApproved`, because the
   engine cannot establish those and the API does not pretend to.
4. **HTTP success is not regulatory success.** `Violated` is data, returned with `200 OK`.
5. **The public contract is not the engine's C# types.** A hand-written, versioned DTO layer sits
   between them. An engine refactor then cannot silently change the REST contract, and a REST
   change cannot reach into the engine.
6. **Product facts and regulatory facts never mix.** The evaluation id, account, API key, external
   reference, request time, request hash and billing data live in the envelope and the store. None
   of them enters `OperationFacts`, and its own documentation says why.
7. **Don't generalise from one example.** FAA shows what the module interface needs to be. A
   deliberately different second engine (§10) then proves it. Nothing moves upstream into
   RulesKernel or rules-factory until two real engines independently need the same shape.

## 3. Architecture

### 3.1 Repository layout

```text
brandonifco/rules-api
  src/
    RulesApi/                      the host: Program.cs, middleware, OpenAPI, infrastructure
    RulesApi.Contracts/            engine-neutral wire types: EvaluationEnvelope<T>,
                                   EngineDescriptor, SourceCitationDto, problem-details shapes
    RulesApi.Runtime/              IRuleEngineModule, EngineRegistry, EvaluationContext,
                                   AddRuleEngine<T>()
    RulesApi.FaaPart107/           the FAA adapter
      FaaPart107Module.cs
      FaaPart107Endpoints.cs
      Contracts/                   EvaluateOperationRequest, EvaluateOperationResponse,
                                   RequirementResult, finding DTOs
      Mapping/                     OperationFactsMapper, OperationEvaluationMapper,
                                   RequirementStateMapper, FindingMapper
  tests/
    RulesApi.Tests/                host and runtime behaviour, tested with a fake module
    RulesApi.FaaPart107.Tests/     adapter mapping and end-to-end HTTP tests
```

### 3.2 Dependency direction

Arrows read "depends on".

```text
  RulesApi  (executable; Program.cs is the composition root)
     │                              │
     │                              │  registration only
     ▼                              ▼
  RulesApi.Runtime  ◄───────  RulesApi.FaaPart107      (future: RulesApi.<Engine>)
     │                              │
     ▼                              ▼
  RulesApi.Contracts          FaaPart107  (this engine, consumed as a package)
                                    │
                                    ▼
                              RulesKernel
```

The boundary has three parts:

- **`RulesApi.Runtime` and `RulesApi.Contracts` never reference FAA Part 107, RulesKernel, or any
  engine adapter.** They are the engine-neutral platform, and a later engine reuses them unchanged.
- **The executable `RulesApi` project is the composition root.** It may reference adapter
  assemblies, and it does so solely to register them: `services.AddRuleEngine<FaaPart107Module>()`
  in `Program.cs`, and one more line of the same kind per engine (§10). It does not reference
  `FaaPart107` or `RulesKernel` itself; those reach it only through an adapter.
- **Everything in `RulesApi` outside the composition root stays engine-neutral.** Middleware,
  infrastructure, OpenAPI setup, the error envelope and every other platform type use no adapter
  type, no engine type and no kernel type.

This must be enforced by tests, not just drawn. The architecture tests in `RulesApi.Tests` check
the boundary as it actually is:

1. the `RulesApi.Runtime` and `RulesApi.Contracts` assemblies reference no `FaaPart107`,
   `RulesKernel` or `RulesApi.<Engine>` adapter assembly;
2. the `RulesApi` assembly references no `FaaPart107` or `RulesKernel` assembly directly;
3. within the `RulesApi` assembly, only the composition root (`Program`, the type the top-level
   statements in `Program.cs` compile to) depends on an adapter type. Any other type in that
   assembly that depends on an adapter, engine or kernel type fails the test, and the failure
   names it.

Adapters stay out of each other too: no `RulesApi.<Engine>` assembly references another.

### 3.3 What is generic and what is FAA-specific

| Platform (reused by every engine) | FAA Part 107 adapter |
|---|---|
| authentication, API keys, accounts, organisations | `EvaluateOperationRequest` and its DTOs |
| evaluation ids, request ids, request logging | `OperationFactsMapper` (groundspeed, airspace, waivers, assertions…) |
| audit history, persistence, replay | `OperationEvaluationMapper`, `RequirementState` mapping |
| rate limiting, usage metering, billing | finding DTOs, one per finding type |
| engine registry and discovery, API versioning | FAA domain validation → `422` |
| provenance transport | normalising the FAA provenance |
| error envelope (RFC 9457 problem details) | the FAA OpenAPI schema |
| webhooks, OpenTelemetry, health checks | the FAA evaluation event payload |

## 4. The engine boundary

### 4.1 What the adapter calls

The adapter uses only the public API this engine already has. It calls nothing generated or
internal:

| Adapter needs | Engine member |
|---|---|
| evaluate | `OperationEvaluator.Evaluate(OperationFacts)` |
| facts | the `init` properties of `OperationFacts`, plus `.Stating(WaiverStatement)` and `.Asserting(Assertion)` |
| waiver statements | `WaiverStatement.Held(regulation, statedBy, certificate?)` and `WaiverStatement.NoneHeld(regulation, statedBy)` |
| assertions | `new Assertion(MapEntry entry, bool holds, string assertedBy)`, with the entry looked up by id |
| results | `OperationEvaluation.Requirements`, `.Outstanding`, `.Unanswered` and `.Count(state)` |
| identity | `OperationEvaluation.EvaluatedBy`, a `ReplayCompatibilityIdentity`: ruleset `faa-part-107` v1, replay schema v1, and the source baselines |
| provenance | `OperationEvaluation.ProvenanceJson()`, which is `provenance.json` byte for byte |
| state of a decline from a direct entry-point call | `RequirementStates.For(UnresolvedReason)` |

### 4.2 Facts the adapter must not invent

- **`AsOf` is the operation's own date.** It is read by `night-waiver-termination`
  (§ 107.29(d)), and it is *not* the request time. The server clock never fills it in. If the
  caller omits `asOf`, it stays null and that entry reports `FactRequired`. The request time
  belongs in the envelope (§7).
- **Waivers are stated per regulation.** A regulation with no statement is not "none held";
  decision 0008 and rules-factory decision 0021 explain why. Each waiver DTO maps to one
  `WaiverStatement` and is added through `Stating`, filed under the regulation the statement
  itself names.
- **`statedBy` and `assertedBy` are required text.** The engine rejects empty ones. The API
  returns `422` rather than filling in a placeholder such as `"api"` or the caller's account name.
  Who is answerable is a regulatory fact, not a product one.
- **Assertions name map entries.** If an `entryId` is not a map entry, or not a
  `kind: assertion` entry, the API returns `422`. It never drops the assertion silently.

### 4.3 How the host consumes the engine

`src/FaaPart107/FaaPart107.csproj` is not packaged or published today. The host must pin an exact,
hashed build of the engine, so that the provenance it reports belongs to the code it actually ran.

The recommendation is to publish `FaaPart107` as a versioned NuGet package (GitHub Packages or a
private feed), built by CI from a tagged commit. `rules-api` then consumes it with a lock file in
locked mode, the same discipline this repository already applies to `RulesKernel` and the map.

Packaging is a change to this repository, so it is an issue under `AGENTS.md` §4, not something
done from the API repository. A git submodule or project reference is acceptable for the Phase A
spike only. It must be replaced before anything is persisted as an audit record.

The host targets `net10.0`. The engine multi-targets `net8.0;net10.0`.

## 5. The request contract

### 5.1 Endpoint

```http
POST /v1/engines/faa-part-107/evaluations
```

The product is an **operation evaluation**, not remote access to individual handlers.
Per-entry endpoints (`/entries/{entryId}/resolutions`) may come later for integrators, but they are
out of scope for this phase.

### 5.2 Shape

The request is a stable DTO, not a serialised `OperationFacts`. Product fields and engine facts are
separated at the top level:

```json
{
  "externalReference": "mission-2026-000184",
  "facts": {
    "groundspeed": { "value": 42, "unit": "knots" },
    "altitudeAboveGroundLevelFeet": 300,
    "flightVisibilityStatuteMiles": 5,
    "airspace": "G",
    "aircraftPower": "powered",
    "fromAMovingAircraft": false,
    "fromMovingLandOrWaterBorneVehicle": false,
    "transportingAnotherPersonsPropertyForCompensationOrHire": false,
    "asOf": "2026-09-25"
  },
  "waivers": [
    { "regulation": "§ 107.51", "status": "noneHeld", "statedBy": "the remote pilot in command" }
  ],
  "assertions": [
    { "entryId": "sufficient-available-power", "holds": true, "assertedBy": "the remote pilot in command" }
  ]
}
```

Rules for the DTO layer:

- **Every fact property is nullable and has no default.** Deserialisation must tell "absent" apart
  from every value. `System.Text.Json` with nullable value types does this, and a test proves it
  for every property.
- **There is one DTO property per `OperationFacts` property.** Each is named for the wire in
  camelCase. Each structured statement gets its own DTO: `StructureStatement`, `CloudStatement`,
  `LightingStatement`, `AtcAuthorization`, `PermissionStatement`, `SubpartDOperation`,
  `WaiverCertificate`, `AircraftEngagement` and the rest. A **coverage test** reflects over
  `OperationFacts` and fails, naming the property, if any public fact property has no DTO
  counterpart. A new engine fact therefore cannot quietly become unreachable over HTTP.
- **Enums travel as strings**, through an explicit, tested table in each direction. They are never
  sent as integers, and caller text is never passed to `Enum.Parse`.
- **Values with units carry their unit.** `groundspeed` is `{ value, unit }` with
  `unit ∈ { knots, milesPerHour }`, because the engine's `Groundspeed` keeps the unit and decision
  0001 depends on it. The adapter never converts between units.
- A waiver's `status` is `held` or `noneHeld`, and a held waiver may carry a `certificate`.

### 5.3 Validation

| Situation | Response |
|---|---|
| the body is not JSON, or a property has the wrong JSON type | `400` |
| an unknown enum string or unit, an unknown or non-assertion `entryId`, an empty `statedBy`/`assertedBy`, or two waivers for one regulation | `422`, naming the JSON pointer |
| the engine throws `ArgumentException` for a malformed value, such as a negative altitude | `422`, naming the input the exception names |
| a fact is omitted | **not an error**: `200`, and that entry reports `factRequired` |

By design, `OperationEvaluator.Evaluate` abandons the whole evaluation when it meets a malformed
value, and its documentation says why. The API does the same: it never returns a partial result
alongside a `422`.

## 6. The response contract

### 6.1 States on the wire

All twelve states are carried one-to-one, as stable camelCase strings:

| `RequirementState` | wire | group |
|---|---|---|
| `Satisfied` | `satisfied` | answered |
| `Violated` | `violated` | answered |
| `Informational` | `informational` | answered |
| `HumanAssertionRecorded` | `humanAssertionRecorded` | answered |
| `ActionRequired` | `actionRequired` | outstanding |
| `HumanAssertionRequired` | `humanAssertionRequired` | outstanding |
| `FactRequired` | `factRequired` | outstanding |
| `RequiresInterpretation` | `requiresInterpretation` | unanswered |
| `OutsideCurrentScope` | `outsideCurrentScope` | unanswered |
| `NotBuilt` | `notBuilt` | unanswered |
| `MissingRulesData` | `missingRulesData` | unanswered |
| `UnresolvedInteraction` | `unresolvedInteraction` | unanswered |

No rule produces `actionRequired` or `unresolvedInteraction` today (see the documentation on
`RequirementState`, and decision 0005). They are in the contract anyway, so that a rule which starts
producing them does not break the API. A test enumerates `RequirementState` and fails on any member
without a wire value. The mapping must stay total and one-to-one, or the build goes red.

`informational` must never be rendered or counted as satisfied. `humanAssertionRecorded` is not
scored either way (decision 0004). The API states both in the schema descriptions, not only in
prose documentation.

### 6.2 One requirement

Every field of `EvaluatedRequirement` is carried. None is dropped for brevity.

```json
{
  "entryId": "speed-within-limit",
  "state": "satisfied",
  "status": "implemented",
  "citations": [ { "source": "cfr-14-107", "locator": "§ 107.51(a)" } ],
  "explanation": "…the engine's own words…",
  "finding": { "type": "groundspeedFinding", "withinLimit": true, "authority": { "source": "cfr-14-107", "locator": "§ 107.51(a)" } },
  "reason": null,
  "declineCites": null,
  "missingInput": null,
  "assertionOwed": null,
  "assertionCites": null,
  "assertedBy": []
}
```

- **`declineCites` is not the same as `citations`.** A decline often cites where the deciding rule
  lives: § 107.205 for a suspended entry, or `speed-limit` for a groundspeed between 87 knots and
  100 mph. It is carried and rendered separately.
- **`finding` needs real work.** `EvaluatedRequirement.Finding` is typed `object?`. It holds a
  rule's own finding, a printed figure, or an `Assertion`. The adapter maps each concrete finding
  type to its own DTO, with a `type` discriminator (an OpenAPI `oneOf`). A test resolves every entry
  on facts that make it resolve, and fails by name on any finding type that has no DTO. The engine
  object is never serialised directly, because that would make the REST contract "whatever the C#
  currently serialises".

### 6.3 The evaluation

Counts and hashes below are illustrative.

```json
{
  "evaluationId": "01J…",
  "externalReference": "mission-2026-000184",
  "evaluatedAt": "2026-09-25T11:37:22Z",
  "engine": {
    "id": "faa-part-107",
    "apiVersion": "v1",
    "ruleset": { "id": "faa-part-107", "version": 1 },
    "replaySchemaVersion": 1,
    "corpus": { "sourceId": "cfr-14-107", "asOf": "2026-01-01", "contentHash": "80f6…" },
    "map": { "packageId": "RulesFactory.Maps.FaaPart107", "version": "4.0.0" },
    "kernel": { "packageId": "RulesKernel", "version": "0.3.0" },
    "provenanceSha256": "…"
  },
  "result": {
    "requirements": [ /* 47, in map order */ ],
    "summary": { "satisfied": 14, "violated": 0, "factRequired": 9, "…": 0 },
    "outstanding": [ "airspace-authorized", "…" ],
    "unanswered": [ "night-operation", "…" ]
  }
}
```

- `summary` gives a **count for each state, with all twelve keys always present**, taken from
  `OperationEvaluation.Count`. It is a tally, not a verdict, and it has no "total compliant" field.
- `outstanding` and `unanswered` are the engine's own groupings, listed as entry ids.
- There is **no** `legal`, `compliant`, `approved` or equivalent field. A product policy layer
  might one day make an aggregate statement, over a ruleset complete enough to support one, but
  that would be its own recorded decision. It is not part of this phase.
- The engine fields are read from the engine itself (`EvaluatedBy` and `ProvenanceJson()`). They
  are never typed into the host's configuration.

### 6.4 HTTP status

| Situation | HTTP |
|---|---|
| the evaluation completed, whatever the states | `200` |
| malformed JSON, or wrong JSON types | `400` |
| an impossible or invalid domain value | `422` |
| an unknown engine or API version | `404` |
| authentication or authorisation failure (Phase B) | `401` / `403` |
| rate limit (Phase B) | `429` |
| unexpected platform failure | `500` |

Every non-2xx response has an RFC 9457 problem-details body, with a stable `type` URI and the
request id.

## 7. The envelope: product facts stay outside the engine

```csharp
public sealed record EvaluationEnvelope<T>
{
    public required string EvaluationId { get; init; }
    public string? ExternalReference { get; init; }
    public required DateTimeOffset EvaluatedAt { get; init; }
    public required EngineDescriptor Engine { get; init; }
    public required T Result { get; init; }
}
```

The clock lives here, and only here. "The API executed this engine at 11:37:22 UTC" is a product
fact. The engine has no clock, and it must never be handed one through `AsOf` (§4.2).

Determinism is measured on `result`, not on the envelope. Sending the same request twice gives
byte-identical serialised `result` and `engine` sections; only `evaluationId` and `evaluatedAt`
differ.

## 8. The platform module interface

The lifecycle is generic, but the payloads stay typed. There is no
`Dictionary<string, object> Inputs` at the HTTP boundary, for any engine, ever. It would bring back
exactly the ambiguity this engine was built to remove.

```csharp
public interface IRuleEngineModule
{
    string Id { get; }            // "faa-part-107"
    string DisplayName { get; }   // "FAA Part 107"
    string ApiVersion { get; }    // "v1"
    EngineDescriptor Describe();
    void MapEndpoints(IEndpointRouteBuilder endpoints);   // under /v1/engines/{Id}
}

services.AddRuleEngine<FaaPart107Module>();
```

Start with this and let FAA push back on it. Expect it to change once, when the second engine
arrives. Don't add members for hypothetical engines before then.

### Endpoints for this phase

```text
GET  /v1/engines                                  the registry: id, name and apiVersion per module
GET  /v1/engines/faa-part-107                     the EngineDescriptor
POST /v1/engines/faa-part-107/evaluations         the product
GET  /v1/engines/faa-part-107/provenance          normalised provenance plus the raw provenance.json
GET  /health                                      liveness and readiness
```

The provenance endpoint returns two things. The first is a normalised summary: engine, ruleset,
factory version and commit, map package and version, kernel, and corpus source, as-of date and
hash. The second is the raw `provenance.json` bytes with their SHA-256. An auditor can then check
the summary against the record instead of trusting it.

## 9. Phase A: the vertical slice (MVP)

Keep it **brutally small**: no accounts, no payments, no database, no frontend and no Kubernetes.

```text
curl → POST /v1/engines/faa-part-107/evaluations → request DTO → OperationFactsMapper
     → OperationEvaluator.Evaluate() → OperationEvaluationMapper → EvaluationEnvelope → JSON
```

### Acceptance

> Given a JSON Part 107 operation request, the API converts it without inventing omitted facts,
> runs the existing `OperationEvaluator`, and returns all 47 mapped outcomes in map order without
> collapsing their states. It identifies the exact engine, map and corpus that produced the result,
> and exposes the engine's recorded provenance.

This is proven by end-to-end tests through `WebApplicationFactory`. Each test is built from a case
the engine's own tests or decisions already establish, so the expected answer comes from the engine
and not from the API author:

| Case | Built from | Expected |
|---|---|---|
| empty `facts` | `OperationFacts.Nothing` | `200`; 47 requirements; every entry that demands an input reports `factRequired`; none reports `satisfied` |
| a groundspeed exceeding neither figure (at or below 100 mph), `noneHeld` for § 107.51 | `SpeedWithinLimitEntryPointTests` | `speed-within-limit` reports `satisfied` |
| a groundspeed above 87 knots (so exceeding both figures), `noneHeld` | same | `violated` |
| a groundspeed above 100 mph and at or below 87 knots | decision 0001 | `requiresInterpretation`, with `declineCites` pointing to `speed-limit` § 107.51(a) |
| a groundspeed given, with no waiver statement for § 107.51 | decision 0008 | `factRequired`, with `missingInput` naming the waiver |
| the waiver for § 107.51 stated as `held` | README, § 107.205 | `outsideCurrentScope`, with `declineCites` § 107.205 |
| `sufficient-available-power` with `aircraftPower: powered` and no assertion | `OperationEvaluatorTests` | `humanAssertionRequired`, with `assertionOwed`, `assertionCites` and `assertedBy` present |
| the same, asserted | decision 0004 | `humanAssertionRecorded`, with the finding carrying the assertion |
| `night-operation`, whatever the facts | `definedElsewhere` | `missingRulesData` |
| `waiver-policy`, whatever the facts | `scope: out` | `outsideCurrentScope` |
| a negative altitude | the `Evaluate` contract | `422`, with no partial result |
| an unknown `entryId` in `assertions` | §4.2 | `422` |
| `asOf` omitted | §4.2 | `night-waiver-termination` reports `factRequired`, and the server date appears nowhere in `result` |
| the same request sent twice | `DeterminismTests` | byte-identical `result` and `engine` |
| `GET …/provenance` | `ProvenanceJson()` | the raw bytes equal the engine's embedded `provenance.json`, and the hash matches |

The structural tests named above are also part of Phase A: `OperationFacts` coverage,
`RequirementState` totality, finding-type coverage, the dependency test, and "absent is not false"
for every nullable property.

If the expected state for a case is in doubt, read it from this engine's tests or decisions.
**Never decide it from your own knowledge of Part 107** (`AGENTS.md` §2). The API's job is to carry
what the engine says. A test that encodes the author's reading of the regulation instead amounts to
a second rules engine that nobody has reviewed.

Phase A also ships the OpenAPI document generated from the DTOs, checked in and diffed in CI, so
that any contract change shows up as a visible diff.

## 10. The second engine is the architecture test

Before any real second engine, add a **tiny fake one**, `ExampleShipping`, with completely
different inputs (a shipment, a destination, a quantity) and three or four trivial rules.

Adding it may touch only:

```text
src/RulesApi.ExampleShipping/   Module, Endpoints, Contracts/, Mapping/
tests/RulesApi.ExampleShipping.Tests/
one registration line: services.AddRuleEngine<ExampleShippingModule>();
```

If it needs edits anywhere else, the platform is not generic yet. That includes `Program.cs` beyond
the one line, the envelope, middleware, authentication, logging, persistence, billing, request
tracking and the OpenAPI infrastructure. Fix the platform, then try again. Keep the fake engine in
the repository permanently, as the regression test for genericity.

## 11. Phases after the slice

Each phase starts only when the previous phase's exit criterion holds.

| Phase | Adds | Exit criterion |
|---|---|---|
| **A** | REST API, FAA adapter, OpenAPI, provenance endpoint, integration tests (§9), `ExampleShipping` (§10) | every §9 test is green in CI, and the fake engine was added without host edits |
| **B** | API keys, accounts, PostgreSQL evaluation history, request ids, audit trail, rate limiting | every `200` is stored as in §12 and retrievable by id, and replaying a stored request reproduces its stored `result` byte for byte |
| **C** | a simple web app: operation wizard, grouped results, saved evaluations, PDF/report export | the UI renders all twelve states distinctly and never shows an aggregate verdict |
| **D** | usage metering, billing, webhooks, customer admin, published API docs | billing reads from the evaluation store, and nothing billing-related enters the engine or the result |
| **E** | a second commercial engine (EAR export) | it is added the same way §10 added the fake one |

Hold off on sophisticated UI until the API boundary has survived FAA. The API is the reusable
product.

**UI guidance for Phase C.** Use a wizard: aircraft and operation, then location and airspace,
people and structures, visibility and clouds, waivers and authorisations, then evaluate. Group the
results by the engine's own groups (answered, outstanding, unanswered), with each state on its own
line. Each requirement expands to show its citations, the stated facts it read, the finding, the
decline citation, and the engine, ruleset and corpus that produced it, with a link to the
provenance.

## 12. Persistence and replay (Phase B)

Store each evaluation so it can be proven and replayed:

```text
Evaluation
  Id, AccountId, EngineId, ApiContractVersion, ExternalReference, RequestedAt
  RequestJson, RequestSha256
  EngineRulesetVersion, ReplaySchemaVersion, CorpusIdentity, MapPackageIdentity,
  KernelIdentity, FactoryIdentity, ProvenanceSha256
  ResponseJson, ResponseSha256
```

This later makes **"re-evaluate this historical request under the new corpus"** possible. Run the
stored request against a newer engine package and compare requirement by requirement: unchanged,
changed, newly answerable, or a new entry. Two rules make that trustworthy:

- Store the **request DTO**, not the mapped `OperationFacts`, so that a replay goes through the
  mapper shipped with the new engine version.
- Label a replay against a different ruleset or replay-schema version as such. Never present it as
  the original evaluation.

Whether one host runs several engine versions side by side is a Phase B design decision (§13), not
a Phase A requirement.

## 13. Open decisions

Record each of these before the phase that needs it. Don't let an implementer settle them in code.

1. **How the engine is distributed** (Phase A): a NuGet package from this repository (recommended,
   §4.3) or a submodule; which feed; and how the host checks the package's embedded provenance
   against its lock-file hash.
2. **The evaluation id format** (Phase A): ULID or UUIDv7.
3. **How fine-grained finding DTOs are** (Phase A): one DTO per engine finding type (recommended),
   or a smaller set of shared shapes.
4. **How a waiver's `regulation` is spelled** (Phase A): exactly the engine's form (`§ 107.51`), or
   a normalised form (`107.51`) mapped explicitly. Whichever is chosen, the mapping is a table, not
   string manipulation.
5. **Several engine versions in one host** (Phase B): selected by URL or header, or one deployment
   per version.
6. **The authentication model** (Phase B): API keys first, OAuth later.

## 14. What this phase does not do

- It does not change the rules, the map, the overlay, the corpus or anything generated here.
- It does not add an aggregate verdict.
- It does not add string-keyed generic inputs.
- It does not move `OperationEvaluator` into RulesKernel or rules-factory. `OperationEvaluator` is
  hand-written orchestration, and every future engine will probably want something like it: one
  result per mapped requirement, outstanding and unanswered entries kept, citations and identity
  carried. Once FAA and a second real engine have each produced one, compare them. What they share
  is the evidence for what belongs upstream. Not before.

## 15. How this repository takes part

During this phase, work in `faa-part-107` stays under `AGENTS.md`: every change is an issue, a
worktree, a branch and a pull request, gated by `./scripts/validate.sh full`. Few changes are
expected here:

- packaging and publishing the engine (§4.3, §13.1);
- closing a gap in the public surface that the adapter finds, for example a finding type or fact
  type that is not public enough to map. File it as an issue describing what the adapter needs. It
  is an engine API change, reviewed as one, and not something to work around in the adapter.

If the adapter finds something that looks like the engine answering wrongly, it is not fixed in the
adapter. It is reported here as an issue against the entry and handled under §5 and §6 of
`AGENTS.md`, like any other.
