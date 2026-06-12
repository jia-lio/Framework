using System.Diagnostics;

namespace Framework
{
    /// <summary>
    /// 전역 커스텀 로그. DI 없이 아무 곳에서나 호출 가능.
    /// 릴리스 빌드에서는 [Conditional]에 의해 호출 코드 자체가 제거된다 (인자 평가 포함).
    /// 이 클래스는 가드 없이 항상 컴파일되어야 호출부가 릴리스에서도 컴파일된다.
    /// </summary>
    public static class DebugLog
    {
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void Log(string message)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnityEngine.Debug.Log($"{DebugLogCapture.CUSTOM_PREFIX} {message}");
#endif
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogWarning(string message)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnityEngine.Debug.LogWarning($"{DebugLogCapture.CUSTOM_PREFIX} {message}");
#endif
        }

        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void LogError(string message)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnityEngine.Debug.LogError($"{DebugLogCapture.CUSTOM_PREFIX} {message}");
#endif
        }
    }
}
