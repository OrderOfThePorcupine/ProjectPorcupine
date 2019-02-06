#! /bin/sh

PROJECT_PATH=$(pwd)/$UNITY_PROJECT_PATH
UNITY_BUILD_DIR=$(pwd)/Build
LOG_FILE=$UNITY_BUILD_DIR/unity-win.log
EXPORT_PATH=$(pwd)/$PROJECT_NAME-v"$TRAVIS_TAG"-b"$TRAVIS_BUILD_NUMBER".unitypackage
RELEASE_DIRECTORY=./release


ERROR_CODE=0

echo "Creating package at=$EXPORT_PATH"
mkdir $UNITY_BUILD_DIR
/Applications/Unity/Unity.app/Contents/MacOS/Unity \
  -batchmode \
  -nographics \
  -silent-crashes \
  -logFile \
  -projectPath "$PROJECT_PATH" \
  -exportPackage "Assets" "$EXPORT_PATH" \
  -quit \
  | tee "$LOG_FILE"
  
if [ $? = 0 ] ; then
	echo "Created package successfully."
	ERROR_CODE=0
	
	echo "Packaging unity package into release..."
	#Preprare release unity package by packing into ZIP
	RELEASE_ZIP_FILE=$RELEASE_DIRECTORY/$PROJECT_NAME-v$TRAVIS_TAG.zip

	mkdir -p $RELEASE_DIRECTORY

	echo "Preparing release for version: $TRAVIS_TAG"
	cp "$EXPORT_PATH" "$RELEASE_DIRECTORY/"`basename "$EXPORT_PATH"`
	cp "./README.md" "$RELEASE_DIRECTORY"
	cp "./LICENSE" "$RELEASE_DIRECTORY"

	echo "Files in release directory:"
	ls $RELEASE_DIRECTORY

	zip -6 -r $RELEASE_ZIP_FILE $RELEASE_DIRECTORY

	echo "Release zip package ready. Zipinfo:"
	zipinfo $RELEASE_ZIP_FILE
	
else
	echo "Creating package failed. Exited with $?."
	ls
	ERROR_CODE=1
fi

#echo 'Build logs:'
#cat $LOG_FILE

echo "Export finished with code $ERROR_CODE"
exit $ERROR_CODE
