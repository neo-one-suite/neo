using Neo.Network.P2P.Payloads;
using System;

namespace Neo.SmartContract
{
    public class LogEventArgs : EventArgs
    {
        public IVerifiable ScriptContainer { get; }
        public UInt160 ScriptHash { get; }
        public string Message { get; }
        public int Position { get; }

        public LogEventArgs(IVerifiable container, UInt160 script_hash, string message, int position)
        {
            this.ScriptContainer = container;
            this.ScriptHash = script_hash;
            this.Message = message;
            this.Position = position;
        }
    }
}
