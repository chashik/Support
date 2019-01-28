using System.Threading;

namespace Test
{
    public interface ISimulator
    {
        void Start(CancellationToken token);
    }
}
