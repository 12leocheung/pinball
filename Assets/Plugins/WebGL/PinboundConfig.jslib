mergeInto(LibraryManager.library, {
  // Leaderboard backend config, set in index.html. All three are optional: with
  // none set the game keeps every score on the device.
  //
  //   window.PINBOUND_REDIS_URL   = "https://xxx.upstash.io";
  //   window.PINBOUND_REDIS_TOKEN = "AX...";
  //   window.PINBOUND_FIREBASE_URL = "https://your-app-default-rtdb.firebaseio.com";
  function pinboundString(name) {
    var value = '';
    if (typeof window !== 'undefined' && window[name]) {
      value = String(window[name]);
    }
    var size = lengthBytesUTF8(value) + 1;
    var buffer = _malloc(size);
    stringToUTF8(value, buffer, size);
    return buffer;
  }

  Pinbound_RedisUrl: function () { return pinboundString('PINBOUND_REDIS_URL'); },
  Pinbound_RedisToken: function () { return pinboundString('PINBOUND_REDIS_TOKEN'); },
  Pinbound_FirebaseUrl: function () { return pinboundString('PINBOUND_FIREBASE_URL'); }
});
