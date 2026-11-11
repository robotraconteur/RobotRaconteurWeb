# Test vectors

`test_vectors.bin` holds the numeric arrays that the service tests send and check. It is a copy of
`test/data/test_vectors.bin` from [Robot Raconteur Core](https://github.com/robotraconteur/robotraconteur), so this
implementation tests with the same data as Core and the other bindings. The format, naming convention, and the
`test_vectors_tool.py` used to inspect or extend the blob are documented in Core's
`test/data/TEST_VECTORS_FORMAT.md`.

The tests read vectors by name with `TestVectors.Get<T>("name")`. The blob is embedded in the test assembly as the
resource `test_vectors.bin`.

Do not edit the copy here. To add or change vectors, change Core's blob first, then copy it over this file. CI runs
`.github/scripts/check_test_vectors.py` to check that every vector in this copy matches Core.
