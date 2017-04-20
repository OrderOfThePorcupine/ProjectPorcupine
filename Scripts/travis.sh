#! /bin/sh
# This script is responsible for running the script that matches
# the environment variable travis selects.

if [ "$JOB" == "unit-test" ]; then
    ./Scripts/Install/unity.sh
    ./Scripts/test.sh --travis

    # Only build binary stuff through the cron-job
    if [ "$TRAVIS_EVENT_TYPE" == "cron" ]; then
        ./Scripts/build.sh
    fi
    exit $?
fi

if [ "$JOB" == "stylecop" ]; then
    if [ "$TRAVIS_EVENT_TYPE" == "cron" ]; then
      exit 0
    fi
    ./Scripts/Install/mono.sh
    ./Scripts/Install/stylecop.sh
    ./Scripts/check-style.sh
fi
