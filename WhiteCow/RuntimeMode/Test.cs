using System;
using WhiteCow.Log;

namespace WhiteCow.RuntimeMode
{
    public class Test:Interface.IRuntimeMode
    {
        public Test()
        {
        }

        public void Dispose()
        {
         
        }

        public void StartToMooh()
        {
            Logger.Instance.LogInfo("test mode started");
        }
    }
}
