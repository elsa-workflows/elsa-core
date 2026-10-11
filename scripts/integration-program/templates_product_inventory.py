"""Current Templates build/data disposition, separate from immutable donor evidence."""

import json
from pathlib import Path
import re
import xml.etree.ElementTree as ET

from apps_consumer_inventory import regular_file, require, tracked_product

INVENTORY = "scripts/solution/solution-groups.json"


def reject_templates_source_graph(paths: set[str]) -> None:
    require(not any(path.startswith("templates/") for path in paths),
            "Independent Templates projects must not enter the root source graph")


def current_templates_projects(root: Path) -> list[Path]:
    configuration = json.loads(regular_file(root, INVENTORY).read_text())["independentProducts"]["templates"]
    require(configuration["solution"] == "templates/Elsa.Templates.slnx"
            and configuration["filter"] == "Elsa.Templates.slnf", "Templates must retain its independent product solution/filter")
    projects, data = configuration["projects"], configuration["buildData"]
    all_projects = projects + data
    require(len(all_projects) == len(set(all_projects)), "Templates build/data dispositions must be unique and disjoint")
    require(all(path.startswith("templates/") and path.endswith(".csproj") for path in all_projects),
            "Templates dispositions must name Templates project manifests")
    tracked = {path for path in tracked_product(root, "templates") if path.endswith(".csproj")}
    require(set(all_projects) == tracked, "Every tracked Templates project needs an explicit build/data disposition")
    product = regular_file(root, configuration["solution"])
    declared = []
    for project in ET.parse(product).iter("Project"):
        relative = project.attrib["Path"].replace("\\", "/")
        require(not Path(relative).is_absolute() and ".." not in Path(relative).parts, "Unsafe Templates solution project path")
        declared.append("templates/" + relative)
    require(len(declared) == len(set(declared)) and set(declared) == set(projects),
            "Templates product solution must match registered build projects only")
    filtered = json.loads(regular_file(root, configuration["filter"]).read_text())["solution"]
    require(filtered["path"] == configuration["solution"], "Templates filter must select its product solution")
    selected = filtered["projects"]
    require(len(selected) == len(set(selected)) and set(selected) == {path.removeprefix("templates/") for path in projects},
            "Templates filter must match registered build projects relative to the product solution")
    root_projects = {path.replace("\\", "/") for path in re.findall(
        r'"([^"\n]+\.csproj)"', regular_file(root, "Elsa.sln").read_text(encoding="utf-8-sig"))}
    reject_templates_source_graph(root_projects)
    for path in all_projects:
        regular_file(root, path)
    for path in projects:
        for reference in ET.parse(root / path).iter("ProjectReference"):
            target = ((root / path).parent / reference.attrib["Include"].replace("\\", "/")).resolve()
            require(target in {root / name for name in projects}, f"Project reference crosses Templates boundary: {path}")
    return [root / path for path in projects]
