#!/usr/bin/env python3
"""check-package.py -- the FaaPart107 package is what docs/decisions/0009 says it is.

    tools/check-package.py artifacts/FaaPart107.0.1.0-dev.nupkg
    tools/check-package.py artifacts/FaaPart107.0.1.0.nupkg --version 0.1.0 --commit "$GITHUB_SHA"

Engine-owned, not emitted by rules-factory. `dotnet pack` decides what goes into the package from
MSBuild properties spread over four files, one of them generated; this reads the package that
came out and fails, naming the part, on anything 0009 does not allow. It runs on every pull request
(package.yml) so packaging cannot break unseen, and in publish.yml between the pack and the push,
so nothing reaches nuget.org that it has not examined.

What it holds:

  * the id is FaaPart107, and the version is --version when one is given (publish.yml passes the
    tag's), so a tag cannot publish a version it does not name;
  * the package holds exactly FaaPart107.dll and FaaPart107.xml for each target framework
    Directory.Build.props declares, and nothing else but the OPC parts NuGet writes;
  * each framework's dependency group is exactly RulesKernel, at the version provenance.json
    records as `kernel.version`. The map package and RulesKernel.Analyzers are build-only and must
    not flow to a consumer;
  * the nuspec names this repository and a 40-hex commit, equal to --commit when one is given;
  * every packed DLL embeds this tree's provenance.json byte for byte, as the managed resource
    FaaPart107.provenance.json: a 4-byte little-endian length followed by exactly those bytes.

Standard library only. Exit 0 when every check held, 1 when one failed, 2 on bad usage. A check
that examined nothing fails: a package with no DLL in it proves nothing about provenance.
"""
import argparse
import json
import re
import struct
import sys
import xml.etree.ElementTree as ET
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
PACKAGE_ID = "FaaPart107"
REPOSITORY = "https://github.com/brandonifco/faa-part-107"
KERNEL_ID = "RulesKernel"
BUILD_ONLY = ("RulesFactory.Maps.", "RulesKernel.Analyzers")
OPC_PARTS = re.compile(r"^(_rels/\.rels|\[Content_Types\]\.xml|package/services/metadata/core-properties/[0-9a-f]+\.psmdcp)$")


def target_frameworks() -> list[str]:
    text = (ROOT / "Directory.Build.props").read_text(encoding="utf-8")
    match = re.search(r"<TargetFrameworks>([^<]+)</TargetFrameworks>", text)
    if not match:
        sys.exit("error: Directory.Build.props declares no <TargetFrameworks>")
    return [tfm.strip() for tfm in match.group(1).split(";") if tfm.strip()]


def local(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def check(package: Path, version: str | None, commit: str | None) -> list[str]:
    problems: list[str] = []
    provenance_bytes = (ROOT / "provenance.json").read_bytes()
    kernel_version = (json.loads(provenance_bytes).get("kernel") or {}).get("version")
    if not kernel_version:
        problems.append("provenance.json records no kernel.version to hold the dependency to")
    frameworks = target_frameworks()

    with zipfile.ZipFile(package) as archive:
        names = archive.namelist()
        nuspecs = [name for name in names if "/" not in name and name.endswith(".nuspec")]
        if nuspecs != [f"{PACKAGE_ID}.nuspec"]:
            return [f"expected exactly one {PACKAGE_ID}.nuspec at the root, found {nuspecs}"]
        nuspec = ET.fromstring(archive.read(nuspecs[0]))
        dlls = {name: archive.read(name) for name in names if name.endswith(".dll")}

    metadata = next((child for child in nuspec if local(child.tag) == "metadata"), None)
    if metadata is None:
        return ["the nuspec has no <metadata>"]
    fields = {local(child.tag): child for child in metadata}

    # Identity.
    found_id = (fields.get("id").text or "") if "id" in fields else ""
    if found_id != PACKAGE_ID:
        problems.append(f"id is {found_id!r}, expected {PACKAGE_ID!r}")
    found_version = (fields.get("version").text or "") if "version" in fields else ""
    if version is not None and found_version != version:
        problems.append(f"version is {found_version!r}, but {version!r} was asked for")
    expected_name = f"{PACKAGE_ID}.{found_version}.nupkg"
    if package.name != expected_name:
        problems.append(f"file is named {package.name!r}, expected {expected_name!r}")

    # Contents: exactly the assembly and its documentation per framework, and the OPC parts.
    expected = {f"lib/{tfm}/{PACKAGE_ID}.{ext}" for tfm in frameworks for ext in ("dll", "xml")}
    actual = {name for name in names if not OPC_PARTS.match(name) and name != f"{PACKAGE_ID}.nuspec"}
    for name in sorted(expected - actual):
        problems.append(f"missing {name}")
    for name in sorted(actual - expected):
        problems.append(f"unexpected {name}: 0009 packs the assembly and its documentation, nothing else")

    # Dependencies: RulesKernel at the recorded kernel version, per framework, and nothing else.
    dependencies = fields.get("dependencies")
    groups = {} if dependencies is None else {
        group.get("targetFramework", ""): [(d.get("id"), d.get("version")) for d in group]
        for group in dependencies if local(group.tag) == "group"
    }
    for tfm in frameworks:
        matching = [deps for key, deps in groups.items() if key.lower().lstrip(".") in (tfm, f"netcoreapp{tfm[3:]}", f"net{tfm[3:]}")]
        if len(matching) != 1:
            problems.append(f"expected one dependency group for {tfm}, found {len(matching)} among {sorted(groups)}")
            continue
        deps = matching[0]
        for dep_id, dep_version in deps:
            if dep_id and dep_id.startswith(BUILD_ONLY):
                problems.append(f"{tfm}: depends on {dep_id}, which is build-only and must not flow to a consumer")
        kernel = [dep_version for dep_id, dep_version in deps if dep_id == KERNEL_ID]
        if kernel != [kernel_version]:
            problems.append(f"{tfm}: RulesKernel dependency is {kernel}, but provenance.json records kernel {kernel_version!r}")
        others = sorted(dep_id or "" for dep_id, _ in deps if dep_id != KERNEL_ID)
        if others:
            problems.append(f"{tfm}: unexpected dependencies {others}; 0009 allows RulesKernel only")

    # Where the bytes came from.
    repository = fields.get("repository")
    if repository is None:
        problems.append("the nuspec has no <repository>; PublishRepositoryUrl must name the source")
    else:
        if repository.get("url") != REPOSITORY:
            problems.append(f"repository url is {repository.get('url')!r}, expected {REPOSITORY!r}")
        found_commit = repository.get("commit") or ""
        if not re.fullmatch(r"[0-9a-f]{40}", found_commit):
            problems.append(f"repository commit is {found_commit!r}, not a 40-hex commit")
        elif commit is not None and found_commit != commit:
            problems.append(f"repository commit is {found_commit}, but the package was asked to be built from {commit}")

    # The embedded record is this tree's provenance.json, byte for byte.
    if not dlls:
        problems.append("the package holds no DLL, so nothing about its provenance was examined")
    resource = struct.pack("<I", len(provenance_bytes)) + provenance_bytes
    for name, image in sorted(dlls.items()):
        if image.count(resource) != 1:
            problems.append(f"{name} does not embed this tree's provenance.json byte for byte")

    return problems


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__.split("\n\n", 1)[0])
    parser.add_argument("package", type=Path)
    parser.add_argument("--version", help="the version the package must carry (publish.yml: the tag's)")
    parser.add_argument("--commit", help="the commit the nuspec must name (publish.yml: GITHUB_SHA)")
    args = parser.parse_args()
    if not args.package.is_file():
        print(f"error: {args.package} is not a file", file=sys.stderr)
        return 2
    problems = check(args.package, args.version, args.commit)
    for problem in problems:
        print(f"FAIL {problem}")
    if problems:
        print(f"check-package: FAIL ({len(problems)} problem(s)) in {args.package.name}")
        return 1
    print(f"check-package: ok   {args.package.name}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
