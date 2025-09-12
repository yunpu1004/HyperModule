namespace HyperModule
{
    public interface IProvide<R>
    {
        R Provide();
    }

    public interface IProvide<T, R>
    {
        R Provide(T data);
    }

    public interface IProvide<T1, T2, R>
    {
        R Provide(T1 data1, T2 data2);
    }

    public interface IProvide<T1, T2, T3, R>
    {
        R Provide(T1 data1, T2 data2, T3 data3);
    }

    public interface IProvide<T1, T2, T3, T4, R>
    {
        R Provide(T1 data1, T2 data2, T3 data3, T4 data4);
    }
}