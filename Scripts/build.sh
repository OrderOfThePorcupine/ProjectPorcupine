#! /bin/sh

echo "Attempting to build $project for Windows"
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
    -batchmode \
    -nographics \
    -silent-crashes \
    -logFile $(pwd)/unity.log \
    -projectPath $(pwd) \
    -buildWindowsPlayer "$(pwd)/Build/windows/ProjectPorcupine.exe" \
    -quit

if [ $? = 0 ] ; then
  echo "Building Windows exe completed successfully."
else
  echo "Building Windows exe failed. Exited with $?."
  exit $?
fi

echo "Attempting to build $project for OS X"
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
    -batchmode \
    -nographics \
    -silent-crashes \
    -logFile $(pwd)/unity.log \
    -projectPath $(pwd) \
    -buildOSXUniversalPlayer "$(pwd)/Build/osx/ProjectPorcupine.app" \
    -quit

if [ $? = 0 ] ; then
  echo "Building Mac exe completed successfully."
else
  echo "Building Mac exe failed. Exited with $?."
  exit $?
fi

echo "Attempting to build $project for Linux"
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
    -batchmode \
    -nographics \
    -silent-crashes \
    -logFile $(pwd)/unity.log \
    -projectPath $(pwd) \
    -buildLinuxUniversalPlayer "$(pwd)/Build/linux/ProjectPorcupine.exe" \
    -quit

if [ $? = 0 ] ; then
  echo "Building Linux exe completed successfully."
else
  echo "Building Linux exe failed. Exited with $?."
  echo $LOG_FILE
  exit $?
fi

echo "Attempting to zip builds for week $BUILD_VERSION"
cd $(pwd)/Build/
echo 'Attempting to zip linux'
zip -q -r Linux-$BUILD_VERSION.zip linux/
echo 'Attempting to zip osx'
zip -q -r MacOSX-$BUILD_VERSION.zip osx/
echo 'Attempting to zip windows'
zip -q -r Windows-$BUILD_VERSION.zip windows/
cd -

echo "All builds done creating config file"

# Create the config file for Bintray
./Scripts/generate-bintray-json.sh > ./Scripts/bintray.json
