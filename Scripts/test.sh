#!/usr/bin/env bash
# -----------------------------------------------------------------------------
#  A script to build a Unity project and run unit tests in either a local or
#  Travis CI environment. Exits with code 1 if any test fails or build errors
#  occur.
#
#  Usage:
#    build_and_test.sh [OPTION]
#
#  OPTIONS:
#    --travis    Indicate that this is being run on the Travis CI server.
#                Otherwise runs as a local environment.
#    -h, --help  Display this usage message.
#
#  NOTES:
#    1) If you're running locally without a special Unity path, the script
#       attempts to guess one based on your OS. If that fails, you can export
#       'unityPath' in your environment to manually set the path.
#    2) The script will produce:
#         - A Unity log file:   unity.log
#         - A Test results XML: EditorTestResults.xml
#       After processing, those files are removed unless needed for error info.
# -----------------------------------------------------------------------------

set -o errexit
set -o nounset
set -o pipefail

# Default path for Unity if not overridden
travisUnity="/Applications/Unity/Unity.app/Contents/MacOS/Unity"
unityPath="${unityPath:-}"           # allow override by env var
RUN_AS_TRAVIS=""                     # will be set if --travis is used

# -----------------------------------------------------------------------------
# 1) Parse arguments
# -----------------------------------------------------------------------------
while [[ $# -gt 0 ]]; do
  case "$1" in
    --travis)
      # If --travis is given, override unityPath with the Travis-specific path
      unityPath="$travisUnity"
      RUN_AS_TRAVIS=1
      ;;
    -h|--help)
      echo "Usage: ${0##*/} [OPTION]"
      echo "Build Unity project and run all unit tests. Exits with 1 if anything fails."
      echo
      echo "Options:"
      echo "  --travis     Indicate Travis CI environment; overrides unityPath with Travis path."
      echo "  -h, --help   This usage message."
      echo
      echo "Environment Variables:"
      echo "  unityPath     If set, used as the path to the Unity executable for local runs."
      exit 0
      ;;
    *)
      # Possibly invoked in a scenario with unexpected extra arguments
      echo "Warning: Unknown option '$1'. Ignoring."
      echo "Try '${0##*/} --help' for usage info."
      ;;
  esac
  shift
done

# -----------------------------------------------------------------------------
# 2) If local run and unityPath is still empty, guess the path
# -----------------------------------------------------------------------------
if [[ -z "$RUN_AS_TRAVIS" ]]; then
  if [[ -z "$unityPath" ]]; then
    # Attempt to guess the environment/OS
    case "$(uname -o || true)" in
      # some systems might not support 'uname -o'; fallback is set above
      Msys)
        unityPath="C:\\Program Files\\Unity\\Editor\\Unity.exe"
        ;;
      *)
        echo "No local Unity path specified; assuming macOS default."
        unityPath="$travisUnity"
        ;;
    esac
  fi
  echo "Using Unity executable: $unityPath"
fi

# -----------------------------------------------------------------------------
# 3) A helper function that only echos if we are in Travis CI mode
# -----------------------------------------------------------------------------
travecho() {
  if [[ -n "$RUN_AS_TRAVIS" ]]; then
    echo "$@"
  fi
}

# Track whether we ended the Travis fold for tests
endTestsFold=0

# -----------------------------------------------------------------------------
# 4) Initiate Unity build + test runs
# -----------------------------------------------------------------------------
travecho 'travis_fold:start:compile'
echo "Attempting to run unit tests in Unity..."

# Kick off Unity in batch mode to run the tests
"$unityPath" \
  -batchmode \
  -runEditorTests \
  -nographics \
  -editorTestsResultFile "$(pwd)/EditorTestResults.xml" \
  -projectPath "$(pwd)" \
  -logFile unity.log

# Capture log content for Travis logs
logFile="$(pwd)/unity.log"
travecho "$(cat "$logFile")"
travecho 'travis_fold:end:compile'

# -----------------------------------------------------------------------------
# 5) Check if test results file was created
# -----------------------------------------------------------------------------
travecho 'travis_fold:start:tests'
travecho 'Show Results from Tests'

if [[ ! -f "$(pwd)/EditorTestResults.xml" ]]; then
  echo "ERROR: Results file not found: EditorTestResults.xml"
  echo "Make sure no existing Unity process was open and try again."
  travecho "travis_fold:end:tests"
  endTestsFold=1

  # Attempt to parse compilation errors from unity.log
  if [[ -f "$(pwd)/unity.log" ]]; then
    out=$(grep "CompilerOutput" unity.log || true)
    if [[ -n "$out" ]]; then
      printf '\n[BUILD FAILED] The compiler generated the following messages:\n'
      # Show lines between CompilerOutput and EndCompilerOutput
      awk '/CompilerOutput:/,/EndCompilerOutput/' < unity.log
    fi
  fi

  # Cleanup
  rm -f "$(pwd)/unity.log"
  exit 1
fi

# If we got here, we have EditorTestResults.xml
rm -f "$(pwd)/unity.log"

resultsFile="$(pwd)/EditorTestResults.xml"
travecho "$(cat "$resultsFile")"
if [[ "$endTestsFold" == 0 ]]; then
  travecho 'travis_fold:end:tests'
fi

# -----------------------------------------------------------------------------
# 6) Parse the XML results for pass/fail
# -----------------------------------------------------------------------------
result=$(sed -n 's/<test-run.*result="\([^"]*\).*/\1/p' "$resultsFile")
errorCount=$(sed -n 's/<test-run.*failed="\([^"]*\).*/\1/p' "$resultsFile")

exitStatus=0

# If any test fails
if [[ "$errorCount" != "0" ]]; then
  echo "$errorCount unit test(s) failed!"

  # Show which tests specifically failed
  echo
  echo "The following unit tests failed:"
  grep 'result="Failed"' "$resultsFile" | grep 'test-case' || true

  exitStatus=1
fi

# If we want to catch inconclusive tests too:
inconclusiveCount=$(sed -n 's/<test-run.*inconclusive="\([^"]*\).*/\1/p' "$resultsFile")
if [[ "$inconclusiveCount" != "0" ]]; then
  echo "$inconclusiveCount unit test(s) were inconclusive!"
  exitStatus=1
fi

# Clean up the results file
rm -f "$resultsFile"

# -----------------------------------------------------------------------------
# 7) Exit with final status
# -----------------------------------------------------------------------------
exit $exitStatus



