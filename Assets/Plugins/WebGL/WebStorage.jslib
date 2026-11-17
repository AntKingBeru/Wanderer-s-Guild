// Copies the browser's in-memory file system (Unity's /idbfs mount) to IndexedDB. Calls made while a copy is running are
// coalesced into one more copy afterwards, so every write is saved and two copies never overlap (that fails).
mergeInto(LibraryManager.library, {
  WG_SyncFileSystem: function () {
    if (typeof FS === 'undefined' || !FS.syncfs) return;

    var state = Module.wgStorageSync || (Module.wgStorageSync = { running: false, again: false });
    if (state.running) { state.again = true; return; }

    var run = function () {
      state.running = true;
      state.again = false;
      FS.syncfs(false, function (error) {
        if (error) console.warn('[WebStorage] Saving to browser storage failed: ' + error);
        state.running = false;
        if (state.again) run();
      });
    };
    run();
  }
});