"""Reviewed identity of the unpublished 3.10.1 candidate that publish-consolidated-3.10.1-candidate.yml may promote.

This is a new pin. The 3.10.0 pin (integration-program/consolidated_candidate_input.py and its envelope) is
a different identity and stays untouched; the 3.10.0 candidate must never be promoted as 3.10.1.
"""

VERSION = "3.10.1"
RUN_ID = 43236516383
RUN_ATTEMPT = 1
ARTIFACT_ID = 13955429317
SOURCE_COMMIT = "1a95b12fa70254831114ea517948604fb62d695b"
ARCHIVE_SHA256 = "9c975053b3113b62eec664fcb498a776e7dedcfbbc578a1df391650541955c19"
MANIFEST_SHA256 = "c20fc5eee0d579055c1fd045f93cf7ef3b24468ed066c3dc124c9d1192e77d6e"
PRODUCER_WORKFLOW = ".github/workflows/prepare-consolidated-3.10.1-release-candidate.yml"
ARTIFACT_NAME = f"consolidated-candidate-{SOURCE_COMMIT}-{RUN_ID}-{RUN_ATTEMPT}"
RELEASE_LINE = "release/3.10.0"

# The staged 3.10.0 candidate: never promotable here.
FORBIDDEN_RUNS = {40347931480}
FORBIDDEN_ARTIFACTS = {12664519771}
FORBIDDEN_SOURCES = {"5cff67228e550b353b3483eb3b37dfdb73a41c74"}

FEEDZ_PACKAGE_COUNT = 231
NUGET_EXCLUDE_IDS = "Elsa.Platform.Integration"
NUGET_PACKAGE_COUNT = 230
