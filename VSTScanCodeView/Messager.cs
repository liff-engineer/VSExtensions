using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSTScanCodeView
{
    public class Messager
    {
        public void Send(object message)
        {
            MessageReceived?.Invoke(this, message);
        }

        public event EventHandler<object> MessageReceived;
    }
}
