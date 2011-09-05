using System;

namespace Sandbox
{
    delegate void Workaround(
        Gate.AppDelegate a1,
        Gate.ResultDelegate a2,
        Gate.BodyDelegate a3,
        Func<int,int,int> a4,
        Action<int> a5);
}
