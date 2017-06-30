#! /bin/sh
# copied from: http://blog.stablekernel.com/continuous-integration-for-unity-5-using-travisci

project="Project Porcupine"

echo "Attempting to build $project for Windows"
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
    -batchmode \
    -nographics \
    -silent-crashes \
    -logFile $(pwd)/unity.log \
    -projectPath $(pwd) \
    -buildWindowsPlayer "$(pwd)/Build/windows/$project.exe" \
    -quit

echo "Attempting to build $project for OS X"
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
    -batchmode \
    -nographics \
    -silent-crashes \
    -logFile $(pwd)/unity.log \
    -projectPath $(pwd) \
    -buildOSXUniversalPlayer "$(pwd)/Build/osx/$project.app" \
    -quit

echo "Attempting to build $project for Linux"
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
    -batchmode \
    -nographics \
    -silent-crashes \
    -logFile $(pwd)/unity.log \
    -projectPath $(pwd) \
    -buildLinuxUniversalPlayer "$(pwd)/Build/linux/$project.exe" \
    -quit

echo 'Logs from build'
cat $(pwd)/unity.log

echo 'Attempting to zip builds'
cd $(pwd)/Build/
zip -q -r Linux-$BUILD_VERSION.zip linux/
zip -q -r MacOSX-$BUILD_VERSION.zip osx/
zip -q -r Windows-$BUILD_VERSION.zip windows/
cd -

# create the config file for Bintray through ERB (an ruby cli-tool)
erb ./Scripts/bintray.json.erb > ./Scripts/bintray.json
