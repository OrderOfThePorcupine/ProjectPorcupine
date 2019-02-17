#! /bin/sh
# Run unit tests
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
