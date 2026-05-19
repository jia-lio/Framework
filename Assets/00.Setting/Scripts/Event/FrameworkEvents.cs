namespace Framework
{
    public readonly struct SceneLoadedEvent
    {
        public readonly string SceneName;
        public SceneLoadedEvent(string sceneName) => SceneName = sceneName;
    }

    public readonly struct PopupClosedEvent
    {
        public readonly PopupView Popup;
        public PopupClosedEvent(PopupView popup) => Popup = popup;
    }

    public readonly struct GamePausedEvent { }

    public readonly struct GameResumedEvent { }
}
