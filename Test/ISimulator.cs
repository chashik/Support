using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Test
{
    public interface ISimulator
    {
        void Start(CancellationToken token);

        List<Task> Pool { get; }
    }
}
