namespace Framework
{
    /// <summary>
    /// 앱 라이프사이클 콜백을 받는 서비스. AppLifecycleDispatcher가 순회 호출한다.
    /// IInitializable과 대칭 패턴.
    /// </summary>
    public interface IAppLifecycle
    {
        /// <summary>앱이 백그라운드로 가거나(모바일 pause=true) 종료될 때(데스크톱). 즉시 영속화.</summary>
        void OnSuspend();

        /// <summary>앱이 포그라운드로 복귀할 때.</summary>
        void OnResume();
    }
}
