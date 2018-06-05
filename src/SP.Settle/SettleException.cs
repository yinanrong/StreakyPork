using System;

namespace Sp.Settle
{
    public class SettleException : Exception
    {
        public SettleException(string msg) : base(msg)
        {
        }

        public SettleException(string msg, Exception innerException) : base(msg, innerException)
        {
        }
    }
}