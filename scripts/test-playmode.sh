#!/usr/bin/env bash
# Run WARPAZ TCG PlayMode tests in batchmode.
# Requires WARPAZ_UNITY_EDITOR to point at the Unity 6000.3.20f1 editor binary.
set -u

if [[ -z "${WARPAZ_UNITY_EDITOR:-}" ]]; then
  echo "WARPAZ_UNITY_EDITOR is not set. Example:" >&2
  echo "  export WARPAZ_UNITY_EDITOR=/path/to/Unity/Hub/Editor/6000.3.20f1/Editor/Unity" >&2
  exit 1
fi

if [[ ! -x "${WARPAZ_UNITY_EDITOR}" ]]; then
  echo "WARPAZ_UNITY_EDITOR is not executable: ${WARPAZ_UNITY_EDITOR}" >&2
  exit 1
fi

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT="${ROOT}/TestResults"
mkdir -p "${OUT}"

RESULTS="${OUT}/playmode.xml"
LOG="${OUT}/playmode.log"

set +e
"${WARPAZ_UNITY_EDITOR}" \
  -batchmode \
  -nographics \
  -quit \
  -projectPath "${ROOT}" \
  -runTests \
  -testPlatform PlayMode \
  -testResults "${RESULTS}" \
  -logFile "${LOG}"
UNITY_EXIT=$?
set -e

if [[ ! -f "${RESULTS}" ]]; then
  echo "PlayMode results file was not written: ${RESULTS}" >&2
  echo "Unity exit code: ${UNITY_EXIT}. See ${LOG}" >&2
  exit 1
fi

python3 - "${RESULTS}" "${UNITY_EXIT}" <<'PY'
import sys
import xml.etree.ElementTree as ET

path, unity_exit = sys.argv[1], int(sys.argv[2])
root = ET.parse(path).getroot()
failed = int(root.attrib.get("failed", "0") or "0")
inconclusive = int(root.attrib.get("inconclusive", "0") or "0")
total = int(root.attrib.get("total", "0") or "0")
passed = int(root.attrib.get("passed", "0") or "0")
result = root.attrib.get("result", "")
print(f"PlayMode: {passed}/{total} passed, failed={failed}, inconclusive={inconclusive}, result={result}, unity_exit={unity_exit}")
if unity_exit != 0 or failed > 0 or inconclusive > 0 or result.lower() == "failed":
    sys.exit(1)
sys.exit(0)
PY
