namespace HyperModule
{
    public interface IService
    {
        void Execute();
    }

    public interface IService<T>
    {
        void Execute(T data);
    }

    public interface IService<T1, T2>
    {
        void Execute(T1 data1, T2 data2);
    }

    public interface IService<T1, T2, T3>
    {
        void Execute(T1 data1, T2 data2, T3 data3);
    }

    public interface IService<T1, T2, T3, T4>
    {
        void Execute(T1 data1, T2 data2, T3 data3, T4 data4);
    }
}