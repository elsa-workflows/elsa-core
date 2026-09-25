#!/usr/bin/env python3
"""Accept a negative consumer build only for the known missing SecretsDbContext API."""

import re
import sys
from pathlib import Path


ERROR = re.compile(r"\berror(?:\s+([A-Z]+\d+))?\s*:", re.IGNORECASE)


def is_expected_break(log: str) -> bool:
    found_expected = False
    for line in log.splitlines():
        for match in ERROR.finditer(line):
            if (match.group(1) or "").upper() != "CS0246" or "SecretsDbContext" not in line[match.end():]:
                return False
            found_expected = True
    return found_expected


if __name__ == "__main__":
    if len(sys.argv) != 2 or not is_expected_break(Path(sys.argv[1]).read_text(encoding="utf-8")):
        raise SystemExit(1)
