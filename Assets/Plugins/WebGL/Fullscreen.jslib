
// 풀스크린 진입/해제.
//
// Module["SetFullscreen"] 은 Unity 가 심어 두는 함수라 jslib 안에서 바로 쓸 수 있다.
// window.unityInstance 는 템플릿의 index.html 이 직접 대입해 줘야만 존재하는데,
// 내장 PWA 템플릿은 그 대입을 하지 않는다. 그래서 Module 을 먼저 쓰고
// unityInstance 는 폴백으로만 남긴다.
//
// SetFullscreen 은 내부에서 JSEvents.canPerformEventHandlerRequests 를 잠시 열어
// Unity 게임 루프(프레임 안)에서 부른 요청도 통과시킨다. 다만 브라우저의
// transient activation 은 여전히 필요하므로 반드시 버튼 클릭 흐름에서 불러야 한다.

mergeInto(LibraryManager.library, {

  $SetUnityFullscreen: function (on) {
    if (typeof Module !== "undefined" && Module.SetFullscreen) {
      Module.SetFullscreen(on);
      return;
    }

    var instance = (typeof window !== "undefined" && window.unityInstance) || globalThis.unityInstance;
    if (instance && instance.SetFullscreen) {
      instance.SetFullscreen(on);
      return;
    }

    console.warn("[Fullscreen] SetFullscreen 을 찾지 못했다");
  },

  $IsInFullscreen: function () {
    return !!(document.fullscreenElement || document.webkitFullscreenElement ||
              document.mozFullScreenElement || document.msFullscreenElement);
  },

  WebEnterFullscreen__deps: ['$SetUnityFullscreen', '$IsInFullscreen'],
  WebEnterFullscreen: function () {
    // 이미 풀스크린인데 또 요청하면 브라우저가 "종료하려면 …" 안내 토스트를 처음부터 다시 띄운다.
    // 그동안 그 영역의 터치가 먹히지 않으므로, 상태가 바뀔 때만 요청한다.
    if (IsInFullscreen()) return;

    SetUnityFullscreen(1);
  },

  WebExitFullscreen__deps: ['$SetUnityFullscreen', '$IsInFullscreen'],
  WebExitFullscreen: function () {
    if (!IsInFullscreen()) return;

    SetUnityFullscreen(0);
  }
});
