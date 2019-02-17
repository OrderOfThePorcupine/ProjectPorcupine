#! /bin/sh
# Build for bintray

./Scripts/Install/unity.sh
./Scripts/test.sh --travis

if [ $? -ne 0 ]; then
    # If the tests failed we don't run the build.
    echo "Tests failed with exit code $?!"
    exit 1
fi

./Scripts/build.sh
exit $?
