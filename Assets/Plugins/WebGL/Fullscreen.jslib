
mergeInto(LibraryManager.library, {
  WebEnterFullscreen: function () {
    console.log("[WebEnterFullscreen] called");

    var instance = window.unityInstance || globalThis.unityInstance;

    if (instance) {
      console.log("[WebEnterFullscreen] unityInstance found, entering fullscreen");
      instance.SetFullscreen(1);
    } else {
      console.warn("[WebEnterFullscreen] unityInstance is undefined or null");
    }
  },

  WebExitFullscreen: function () {
    console.log("[WebExitFullscreen] called");

    var instance = window.unityInstance || globalThis.unityInstance;

    if (instance) {
      console.log("[WebExitFullscreen] unityInstance found, exiting fullscreen");
      instance.SetFullscreen(0);
    } else {
      console.warn("[WebExitFullscreen] unityInstance is undefined or null");
    }
  }
});