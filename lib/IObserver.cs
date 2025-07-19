namespace ShapeDrawer.lib
{
    public interface IObserver
    {
        void Update(string eventType, object data);
    }
}