using System.Runtime.CompilerServices;

namespace Neo.SmartContract
{
    [AsyncMethodBuilder(typeof(ContractTaskMethodBuilder))]
    class ContractTask
    {
        private readonly ContractTaskAwaiter awaiter;

        public static ContractTask CompletedTask { get; }

        static ContractTask()
        {
            CompletedTask = new ContractTask();
            CompletedTask.GetAwaiter().SetResult();
        }

        public ContractTask()
        {
            awaiter = CreateAwaiter();
        }

        protected virtual ContractTaskAwaiter CreateAwaiter() => new();

        public virtual ContractTaskAwaiter GetAwaiter() => awaiter;

        public virtual object GetResult() => null;
    }

    [AsyncMethodBuilder(typeof(ContractTaskMethodBuilder<>))]
    class ContractTask<T>
    {
        private readonly ContractTaskAwaiter<T> awaiter;
        public static ContractTask<T> CompletedTask { get; }
        static ContractTask()
        {
            CompletedTask = new ContractTask<T>();
            CompletedTask.GetAwaiter().SetResult();
        }
        public ContractTask()
        {
            awaiter = CreateAwaiter();
        }
        protected virtual ContractTaskAwaiter<T> CreateAwaiter() => new();

        public virtual ContractTaskAwaiter<T> GetAwaiter() => awaiter;

        public virtual object GetResult() => GetAwaiter().GetResult();
    }
}
