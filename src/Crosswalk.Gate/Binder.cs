using System;
using Gate;

namespace Crosswalk.Gate
{
    public class Binder : IBinder
    {
        public void Bind(
            ref CrosswalkModule.BindHandlerContext binding, 
            out CrosswalkModule.ExecuteHandlerDelegate executeHandler)
        {
            var app = AppBuilder.BuildConfiguration(binding.ScriptProcessor);
            var handler = new Handler(app);
            executeHandler = handler.Execute;
        }

    }
}
