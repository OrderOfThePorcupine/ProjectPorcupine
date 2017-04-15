#! /bin/sh

# adapted from: https://github.com/JonathanPorta/ci-build

# This link should be changed if the unity version of this project is updated
echo 'Downloading Unity-5.4.0f3: '
curl -o Unity.pkg http://download.unity3d.com/download_unity/4d2f809fd6f3/MacEditorInstaller/Unity-5.5.3f1.pkg

echo 'travis_fold:start:install-unity'
echo 'Installing Unity.pkg'
sudo installer -dumplog -package Unity.pkg -target /
echo 'travis_fold:end:install-unity'
