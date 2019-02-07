#! /bin/sh
# This script is responsible for running the script that matches
# the environment variable travis selects.

if [ "$JOB" == "unit-test" ]; then
    ./Scripts/Install/unity.sh

    ./Scripts/test.sh --travis
    if [ $? -ne 0 ]; then
        # If the tests failed we don't run the build.
        echo "Tests failed with exit code $?!"
        exit 1
    fi

    # Only build binary stuff through the cron-job
    if [ "$TRAVIS_EVENT_TYPE" == "cron" ]; then
        ./Scripts/build.sh
    fi
    exit $?
fi

if [ "$JOB" == "stylecop" ]; then
    ./Scripts/Install/mono.sh
    ./Scripts/Install/stylecop.sh
    ./Scripts/check-style.sh
fi
