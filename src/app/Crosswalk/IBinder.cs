namespace Crosswalk
{
    public interface IBinder
    {
        void Bind(
            ref CrosswalkModule.BindHandlerContext binding, 
            out CrosswalkModule.ExecuteHandlerDelegate executeHandler);
    }
}
