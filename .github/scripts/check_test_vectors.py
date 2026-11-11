#!/usr/bin/env python3
"""Check that a vendored test vector blob agrees with Robot Raconteur Core.

Every vector in the vendored blob must exist in the Core blob with the same type
and values. Vectors that only Core has are listed, so the copy can be refreshed,
but they do not fail the check.

    check_test_vectors.py <vendored test_vectors.bin> <Core test/data directory>

The blob is parsed with Core's own test_vectors_tool.py, so the format checks
match Core.
"""

import os
import sys


def main(argv):
    if len(argv) != 3:
        sys.stderr.write(__doc__)
        return 2
    vendored_path, core_dir = argv[1], argv[2]
    sys.path.insert(0, core_dir)
    import test_vectors_tool  # pylint: disable=import-outside-toplevel

    def load(path):
        with open(path, "rb") as f:
            return dict((v["name"], v) for v in test_vectors_tool.unpack(f.read()))

    vendored = load(vendored_path)
    core = load(os.path.join(core_dir, "test_vectors.bin"))

    errors = []
    for name in sorted(vendored):
        if name not in core:
            errors.append("%s is not in Core" % name)
        elif vendored[name]["type"] != core[name]["type"] or vendored[name]["values"] != core[name]["values"]:
            errors.append("%s differs from Core" % name)

    for name in sorted(set(core) - set(vendored)):
        print("Core has %s, which is not in the vendored copy" % name)
    for error in errors:
        print("ERROR: " + error)
    print("%d vendored vectors checked against Core, %d errors" % (len(vendored), len(errors)))
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
