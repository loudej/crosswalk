using Gate;

namespace Sandbox
{
    public class Startup
    {
        public static void Configuration(IAppBuilder builder)
        {
            builder.Run(HelloWorldApp.Create);
        }
    }
}