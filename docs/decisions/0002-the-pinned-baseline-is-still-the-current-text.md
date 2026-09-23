# 0002 — The pinned 2026-01-01 baseline is still the current text, and the map is not churned for a date

**Status:** accepted.

## Context

This engine pins 14 CFR Part 107 in `corpus/part107.xml` and cites it as of a date.
Two files describe that pin, and they do not carry the same fields. `provenance.json`'s `corpus`
block carries four, and the map package's `corpus-manifest.json` carries those four and three more:

| | in `provenance.json` | in `corpus-manifest.json` |
|---|---|---|
| `sourceId` — `cfr-14-107` | yes | yes |
| `asOf` — `2026-01-01` | yes | yes |
| `contentHash` — `80f6bc4b002df9dcc60a651fec30a2dc3590081cc3e5fd431d9885c69b7ce35e` | yes | yes |
| `hashDerivation` — `ecfr-versioner-xml` | yes | yes |
| `boundaryPolicy` — `pin-in-repo` | — | yes |
| `verification` — `committed-copy` | — | yes |
| `retrievedFrom` — `…/versioner/v1/full/2026-01-01/title-14.xml?part=107` | — | yes |

The four they share agree exactly, which is what "the record and the manifest agree on the pin"
means here. The other three are the manifest's alone, and the argument below turns on them, so it
is worth being exact about where they live.

The engine is now some months past that date, and it is about to be treated as the regulatory core
of a product. A caller is entitled to know whether "Part 107 as of 2026-01-01" is still the current
text — and entitled to the evidence rather than an assurance. The question this record answers is
therefore not "is there newer text somewhere" but the two the map actually turns on: **is the pin
authentic**, and **did anything inside the mapped extent move**.

Note what is *not* a source here. The FAA's own web guidance, its advisory circulars and its
summaries of Part 107 are commentary. The corpus is the CFR text as the eCFR versioner serves it,
which is what `hashDerivation: ecfr-versioner-xml` names and what the committed copy is a copy of.

## The check

Three questions, each answered by a command whose output is below.

**1. Has part 107 been amended since the pinned date?** The versioner's own version history for the
part, which records every amendment with its date:

```
$ curl -sS --compressed "https://www.ecfr.gov/api/versioner/v1/versions/title-14.json?part=107"

176 rows, distinct amendment dates:
  2016-12-30, 2018-03-05, 2020-05-04, 2020-06-29, 2020-07-27, 2020-08-05,
  2020-09-24, 2020-10-06, 2020-12-11, 2021-01-15, 2021-02-26, 2021-03-10,
  2021-04-21, 2021-04-30, 2021-11-10, 2022-12-09, 2025-09-03, 2025-11-03

rows with amendment_date > 2026-01-01:  0
```

The most recent amendment to part 107 is **2025-11-03**, which is before the pinned baseline. The
pin was taken after the latest amendment, not before it.

**2. Is the pin authentic — does the recorded hash reproduce?** The manifest's `retrievedFrom` URL,
fetched again:

```
$ curl -sS --compressed "https://www.ecfr.gov/api/versioner/v1/full/2026-01-01/title-14.xml?part=107" -o p107-20260101.xml
$ sha256sum p107-20260101.xml corpus/part107.xml

80f6bc4b002df9dcc60a651fec30a2dc3590081cc3e5fd431d9885c69b7ce35e  p107-20260101.xml
80f6bc4b002df9dcc60a651fec30a2dc3590081cc3e5fd431d9885c69b7ce35e  corpus/part107.xml
```

Byte for byte, the committed copy is what that URL serves. The recorded hash is the hash of those
bytes. Nothing about the pin is taken on trust.

(The endpoint refuses an uncompressed request with `406` and the message *"This endpoint requires
response compression"*, which is why `--compressed` is not optional. A fetch without it writes a
171-byte error document, and a hash of **that** would differ from the baseline for a reason that
has nothing to do with the regulation. Worth naming: this is exactly the shape of accident that
makes a corpus check report a change that never happened.)

**3. Did the text move?** The most recent text the versioner can serve. Title 14's
`up_to_date_as_of` is the latest date it will accept; a later date is refused rather than silently
served as the newest available:

```
$ curl -sS --compressed "https://www.ecfr.gov/api/versioner/v1/titles.json"
  title 14:  latest_amended_on 2026-09-15, latest_issue_date 2026-09-15, up_to_date_as_of 2026-09-21

$ curl -sS --compressed "https://www.ecfr.gov/api/versioner/v1/full/2026-09-21/title-14.xml?part=107" -o p107-current.xml
$ sha256sum p107-current.xml corpus/part107.xml

8d448ab10578dfaf1f01e75f6d435ee1a42fde54067932c8594c3b1f2ce2951d  p107-current.xml
80f6bc4b002df9dcc60a651fec30a2dc3590081cc3e5fd431d9885c69b7ce35e  corpus/part107.xml
```

The hashes differ. That is where a check that stopped at the hash would report a changed
regulation, and it would be wrong. The whole diff is ten lines:

```
$ diff p107-current.xml corpus/part107.xml | grep -c '^[<>]'
10

$ diff p107-current.xml corpus/part107.xml
11c11
< <DIV6 N="A" TYPE="SUBPART" VOLUME="2" hierarchy_metadata="{…/title-14/part-107/subpart-A…}">
---
> <DIV6 N="A" TYPE="SUBPART" hierarchy_metadata="{…/title-14/part-107/subpart-A…}">
  … and the same single change on the DIV6 elements for subparts B, C, D and E.
```

Five elements, one attribute. The eCFR now emits `VOLUME="2"` on each `<DIV6 TYPE="SUBPART">`; the
2026-01-01 serialization did not. It is the CFR volume the subpart is printed in — a fact about the
publication, carried in the document's structural metadata, not regulatory text and not inside any
entry's `evidence`.

Two further facts make that characterisation mechanical rather than a judgement about what the
attribute looks like.

**The attribute is not new to this document, and its value has not changed.** The pinned corpus
already carries it, on the part element the subparts sit inside:

```
$ grep -o '<DIV5[^>]*>' corpus/part107.xml | head -1
<DIV5 N="107" TYPE="PART" VOLUME="2" hierarchy_metadata="{…/title-14/part-107…}">
```

So `VOLUME="2"` is markup the baseline itself already establishes the map ignores. What the
publisher did was propagate the same value down to the child subpart elements.

**The change lands on a date with no amendment behind it.** Probing the versioner date by date
finds a single boundary, and it is nowhere near an amendment:

```
$ for d in 2026-05-24 2026-05-25 2026-05-26 2026-05-27 2026-06-15; do
    curl -sS --compressed ".../full/$d/title-14.xml?part=107" | sha256sum; done
2026-05-24 -> 80f6bc4b002df9dc…      2026-05-26 -> 8d448ab10578dfaf…
2026-05-25 -> 80f6bc4b002df9dc…      2026-05-27 -> 8d448ab10578dfaf…
                                     2026-06-15 -> 8d448ab10578dfaf…
```

Every date to 2026-05-25 serves the old serialization; every date from 2026-05-26 serves the new
one. The versioner reports **no amendment to part 107 anywhere in that window** — the latest is
still 2025-11-03. A content difference that appears on a date on which no amendment exists cannot
be a change in the regulation. That is a proof, not an impression.

Removing that one attribute settles it:

```
$ sed 's/\(<DIV6 [^>]*TYPE="SUBPART"\) VOLUME="2"/\1/' p107-current.xml > p107-current-novol.xml
$ diff p107-current-novol.xml corpus/part107.xml && echo IDENTICAL
IDENTICAL
```

**The current authoritative text of Part 107 and this engine's pinned corpus are the same
document.** Every difference between them is accounted for, by name, and none of it is regulation.

## Decision

**The baseline stays at 2026-01-01, and nothing is re-pinned, re-mapped or re-versioned.**

Not because re-pinning would be expensive, but because there is nothing to record. The corpus has
not moved inside the mapped extent — or outside it. Re-pinning would produce a new content hash, a
new `asOf`, a new map package version and a re-produce of every generated file in this engine, all
of it asserting a change to a document that is character-for-character what it already was, save
five attributes the publisher added to its own markup. Downstream, every one of those version
numbers is a claim that something happened. Spending them on a serialization change devalues them
for the time something does happen, which is the only time they are worth anything.

**The manifest's own terms already settle it, before any reading of `asOf` is needed.** This is
the short argument, and it is the one to rely on:

- `retrievedFrom` names a **dated** URL, `…/full/2026-01-01/…`, and that exact URL still serves the
  pinned bytes today. The re-fetch in check 2 above is not a courtesy; it is the manifest's own
  identifier of the corpus, resolving to the same document it always did.
- The 2026-09-21 bytes come from a **different URL, which the manifest does not name.** They are
  evidence about the regulation — useful, and the reason for check 3 — but they are not the thing
  the pin points at.
- `boundaryPolicy: pin-in-repo` and `verification: committed-copy` make the committed copy the
  boundary, and `scripts/engine-gate.py`'s posture step applies the derivation to
  `corpus/part107.xml`'s own bytes, never to a live fetch.
- `hashDerivation: ecfr-versioner-xml` names the **function** used to digest those bytes — it
  resolves in `scripts/factory/intake.py` to a plain sha256 over them. It is not a promise that the
  digest tracks whatever the publisher currently serves at some other URL.

So nothing here is redefined and no tension has to be resolved: the pin is the committed copy, the
manifest's dated URL still reproduces it, and the gate checks exactly that.

**And `asOf: 2026-01-01` is a statement about the regulation, not about the fetch.** This is the
longer argument, and it is why the short one is not a technicality. The date says *this engine
implements the Part 107 that was in force on 2026-01-01*, and that statement is still true today,
because no amendment has occurred since 2025-11-03. It does not say *these bytes are the latest
bytes the eCFR will serve*, which would make the baseline stale the moment a publisher touched its
own XML generator. A corpus pin whose date has to move for reasons unrelated to the law is a pin
that means nothing.

**A hash difference is the start of a corpus check, not the end of one.** The `verification:
committed-copy` posture makes the gate compare the committed bytes with the recorded hash, which is
what proves the copy has not been tampered with — and that check is unaffected here, because
nothing in this repository changed. But a *re-check against the live source*, of the kind this
record performs, has to characterise every difference before it can conclude anything. Had this
record stopped at "the hashes differ", it would have opened a map revision for `VOLUME="2"`.

**What would change this answer, and what the criterion actually is.**

The criterion is about **element identity and text**, not about which elements the locator grammar
can address. That distinction is the one this check turned on and it is easy to get wrong, so it is
worth stating why. The grammar does address subparts: `subpart-d-categories` is the one entry of
the forty-seven whose locator is not a section designation, and its citation is exactly
`"subpart D"` — the very kind of element that gained the attribute. A criterion phrased as "a hunk
touching any element the grammar addresses" would therefore have fired on this diff and sent a
markup change to the temporal-change process. What the grammar cannot address is an **attribute**;
what it resolves on is the element's identity (`N="D" TYPE="SUBPART"`) and the text inside it, and
neither moved.

So: re-open the question when a diff shows any of

- an `amendment_date`, `date` or `issue_date` after the baseline in the versioner's history for
  part 107 — the record checked all three, and on this corpus they agree on every row;
- a change to text: anything inside a `<P>`, a `<HEAD>`, a section heading, or any other prose the
  map quotes as `evidence`;
- a change to an element's **identity** — its `N`, its `TYPE`, or its presence: a section, subpart
  or paragraph added to, removed from, or renumbered within the part;
- a change to an attribute that any entry's locator or the adapter actually reads. `VOLUME` is not
  one; a criterion that lists attributes by name would go stale, so the test is whether anything in
  the map or the `ecfr-xml` adapter reads it.

and treat as **not** a corpus change a difference that is confined to markup nothing reads,
**provided** it is shown to be so rather than assumed.

**The trigger for re-running this check is broader than an amendment.** The date probing above
shows the publisher re-serializes its own output at date boundaries with no amendment behind them.
A future re-serialization of the **2026-01-01** rendition itself would break the
reproduce-from-`retrievedFrom` check in step 2 — the pin would still be the committed copy, and the
committed-copy posture would still pass, but the manifest's dated URL would no longer return those
bytes, and this record's step 2 would no longer reproduce. That is not a change in the law and it
would not move the baseline, but it is a thing a reader of this record should expect to find rather
than be alarmed by. Watching only `amendment_date` would miss it.

The answer to a genuine corpus change is not this record — it is the temporal-change process: a new
mapping pass against the new text, independently checked, a new published map version, and a
re-produce of this engine from it, on its own issue. The `faa-part-107-temporal` trial in
rules-factory is the worked example of that process, and it is also the standing evidence that this
engine's dates-inside-a-rule are ordinary operations over a date the caller supplies, not corpus
versioning.

## Consequences

- This engine continues to cite `cfr-14-107` as of `2026-01-01`, and that citation is accurate.
- `corpus/part107.xml`, `provenance.json`, `corpus-map.overlay.json` and the map package version are
  untouched by this record. It adds a document and changes no behaviour.
- The re-check is reproducible: the four commands above are the whole of it, and a reader who runs
  them gets the outputs above, or has found something this record did not.
- This is a point-in-time result. It is evidence that the baseline was current on the date of this
  record, not a guarantee that it stays current. The check is cheap and its inputs are public, so
  re-running it is the maintenance this pin needs — watching the versioner's history for part 107,
  and not only its `amendment_date` column, for the reason given under "What would change this
  answer".
