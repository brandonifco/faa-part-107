# 0010 — The development guide decision 0009 cites is no longer published in this repository

**Status:** accepted when the owner merges the pull request that closes #144. That merge is the
ruling, on documentation only: this record rules on no map entry, and changes no rule or outcome.

## Context

Decision 0009 cites `docs/rules-api-development-guide.md` (§4.3, Phase A §13.1) for the question it
answers, and says twice more what "the guide" accepts or keeps for the Phase A spike. That file was
product design for a separate, private service that consumes this engine. It is outside this
engine's scope and does not belong in this public repository, so #144 removes it from the tree.

A numbered decision record is frozen: it is superseded by a new record and never rewritten. So
0009 is not edited, and its citation now names a document that this repository does not hold.

## Decision

Where decision 0009 cites `docs/rules-api-development-guide.md`, or says what "the guide" accepts or
keeps, the document is no longer published here. This record is the pointer. Those statements about
the guide are no longer checkable from this repository.

0009's ruling does not rest on them. Its reasons are the ones it argues itself, from this engine's
distribution and the two precedents it names, and they stand as written: the engine is published to
nuget.org as one exact, versioned package, and a consumer pins it by hash. Nothing in this record
changes that.

## Consequences

- No engine behaviour, map entry, overlay file or test changes.
- A reader of 0009 who looks for the cited guide will not find it in the tree. Earlier commits
  still hold it; removing it from the tree does not unpublish history.
