#if UNITY_EDITOR || DEVELOPMENT_BUILD
namespace Framework
{
    public interface IDebugPanel
    {
        string Title { get; }
        void Draw();
    }
}
#endif
