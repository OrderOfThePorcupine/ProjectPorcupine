#! /bin/sh

# adapated from: http://blog.stablekernel.com/continuous-integration-for-unity-5-using-travisci
BASE_URL=http://netstorage.unity3d.com/unity
HASH=d3101c3b8468
VERSION=5.6.3f1

download() {
    file=$1
    url="$BASE_URL/$HASH/$package"

    echo "Downloading from $url: "
    curl -o `basename "$package"` "$url"
}

install() {
    package=$1
    download "$package"

    echo "Installing "`basename "$package"`
    sudo installer -dumplog -package `basename "$package"` -target /
}

# See $BASE_URL/$HASH/unity-$VERSION-$PLATFORM.ini for complete list
# of available packages, where PLATFORM is `osx` or `win`

echo 'travis_fold:start:install-unity'
echo 'Installing Unity.pkg'
install "MacEditorInstaller/Unity-$VERSION.pkg"

# These packages are only necessary to build the binary for these platforms
# and at the moment the build should only be done through the cronjob
if [ "$TRAVIS_EVENT_TYPE" == "cron" ]; then
    install "MacEditorTargetInstaller/UnitySetup-Windows-Support-for-Editor-$VERSION.pkg"
    install "MacEditorTargetInstaller/UnitySetup-Mac-Support-for-Editor-$VERSION.pkg"
    install "MacEditorTargetInstaller/UnitySetup-Linux-Support-for-Editor-$VERSION.pkg"
fi
echo 'travis_fold:end:install-unity'
