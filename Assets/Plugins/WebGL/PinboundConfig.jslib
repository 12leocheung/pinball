mergeInto(LibraryManager.library, {
  // The Firebase Realtime Database URL, set in index.html as
  //   window.PINBOUND_FIREBASE_URL = "https://your-app-default-rtdb.firebaseio.com";
  // Read once at boot; an empty string keeps the game on the on-device boards.
  Pinbound_FirebaseUrl: function () {
    var url = '';
    if (typeof window !== 'undefined' && window.PINBOUND_FIREBASE_URL) {
      url = String(window.PINBOUND_FIREBASE_URL);
    }
    var size = lengthBytesUTF8(url) + 1;
    var buffer = _malloc(size);
    stringToUTF8(url, buffer, size);
    return buffer;
  }
});
