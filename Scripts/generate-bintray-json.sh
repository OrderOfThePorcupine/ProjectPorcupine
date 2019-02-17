#! /bin/bash

NOW_DATE=`date +%Y-%m-%d`

cat << EOF
{
    "package": {
        "name": "Prebuilt-Binaries",
        "repo": "ProjectPorcupine",
        "subject": "orderoftheporcupine",
        "desc": "Weekly dev-build #$BUILD_VERSION"
    },

    "version": {
        "name": "$BUILD_VERSION",
        "released": "$NOW_DATE",
        "attributes": [],
        "gpgSign": false
    },

    "files":
        [
        {"includePattern": "Build/(.*\\.zip)", "uploadPattern": "\$1"}
        ],
    "publish": true
}
EOF