using System;
using System.Collections.Generic;
using System.Reflection;

namespace Crosswalk
{
    public class WebAppDomainManager : AppDomainManager
    {
        CrosswalkModule.AppHandlerInfo _info;
        readonly IDictionary<string, CrosswalkModule.BindHandlerDelegate> _binders = new Dictionary<string, CrosswalkModule.BindHandlerDelegate>();
        readonly IDictionary<string, CrosswalkModule.ExecuteHandlerDelegate> _handlers = new Dictionary<string, CrosswalkModule.ExecuteHandlerDelegate>();

        public override void InitializeNewDomain(AppDomainSetup appDomainInfo)
        {
            _info.BindHandler = BindHandler;
            CrosswalkModule.BindAppHandlerInfo(ref _info);
        }

        public void BindHandler(
            ref CrosswalkModule.BindHandlerContext context,
            out CrosswalkModule.ExecuteHandlerDelegate executeHandler)
        {
            lock (_handlers)
            {
                if (!_handlers.TryGetValue(context.Name, out executeHandler))
                {
                    CrosswalkModule.BindHandlerDelegate binder;
                    lock (_binders)
                    {
                        if (!_binders.TryGetValue(context.ManagedType, out binder))
                        {
                            var parts = context.ManagedType.Split(new[] { "," }, 2, StringSplitOptions.None);

                            var assembly = Assembly.Load(parts[1]);
                            var type = assembly.GetType(parts[0]);
                            var instance = (IBinder)Activator.CreateInstance(type);
                            binder = instance.Bind;
                            _binders.Add(context.ManagedType, binder);
                        }
                    }
                    binder(ref context, out executeHandler);
                    _handlers.Add(context.Name, executeHandler);
                }
            }
        }
    }
}
