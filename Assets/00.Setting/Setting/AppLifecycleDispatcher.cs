using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace Framework
{
    /// <summary>
    /// Unity 앱 라이프사이클 콜백을 받아 IAppLifecycle 핸들러에 fan-out.
    /// DontDestroyOnLoad 싱글톤 GameObject (RootScope에서 생성·등록).
    ///
    /// 플랫폼 매트릭스:
    /// - 모바일: OnApplicationPause(true)가 실제 저장점. OnApplicationQuit은 비신뢰(백그라운드 kill 시 미발생).
    /// - 데스크톱: OnApplicationQuit이 종료점. 에디터 Stop도 OnApplicationQuit 발화.
    /// - Android 멀티윈도우/분할화면: pause 없이 focus(false)만 오는 경우가 있어 모바일 한정 보조 트리거.
    ///
    /// _suspended 가드로 중복 발화(pause→quit, focus→pause 등)를 1회로 합친다.
    /// </summary>
    public class AppLifecycleDispatcher : MonoBehaviour
    {
        [Inject] private IEnumerable<IAppLifecycle> _handlers;

        private bool _suspended;

        private void OnApplicationPause(bool pause)
        {
            if (pause) Suspend();
            else Resume();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            // 데스크톱 alt-tab 디스크 churn 방지 — pause가 안 오는 모바일에서만 보조 트리거.
            if (!Application.isMobilePlatform) return;
            if (!hasFocus) Suspend();
            else Resume();
        }

        private void OnApplicationQuit() => Suspend();   // 데스크톱 종료 (모바일은 비신뢰)

        private void Suspend()
        {
            if (_suspended) return;
            _suspended = true;
            Dispatch(true);
        }

        private void Resume()
        {
            if (!_suspended) return;
            _suspended = false;
            Dispatch(false);
        }

        private void Dispatch(bool suspend)
        {
            if (_handlers == null) return;   // 주입 전 콜백 방어
            foreach (var handler in _handlers)
            {
                try
                {
                    if (suspend) handler.OnSuspend();
                    else handler.OnResume();
                }
                catch (Exception e)
                {
                    // 한 핸들러 throw가 나머지를 막지 않도록 격리
                    Debug.LogError($"[AppLifecycle] {(suspend ? "OnSuspend" : "OnResume")} failed: {e}");
                }
            }
        }
    }
}
