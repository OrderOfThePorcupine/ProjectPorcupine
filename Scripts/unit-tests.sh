#! /bin/sh
# Run unit tests
./Scripts/test.sh --travis

if [ $? -ne 0 ]; then
    # If the tests failed we don't run the build.
    echo "Tests failed with exit code $?!"
    exit 1
fi

exit $?
