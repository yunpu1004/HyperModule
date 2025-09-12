namespace HyperModule
{
    public interface IExecute
    {
        void Execute();
    }

    public interface IExecute<T>
    {
        void Execute(T data);
    }

    public interface IExecute<T1, T2>
    {
        void Execute(T1 data1, T2 data2);
    }

    public interface IExecute<T1, T2, T3>
    {
        void Execute(T1 data1, T2 data2, T3 data3);
    }

    public interface IExecute<T1, T2, T3, T4>
    {
        void Execute(T1 data1, T2 data2, T3 data3, T4 data4);
    }
}