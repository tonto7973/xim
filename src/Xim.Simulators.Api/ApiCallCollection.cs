using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Xim.Simulators.Api
{
    internal sealed class ApiCallCollection : IReadOnlyCollection<ApiCall>
    {
        private readonly ConcurrentQueue<ApiCall> _apiCalls = new ConcurrentQueue<ApiCall>();

        public int Count => _apiCalls.Count;

        internal ApiCallCollection() { }

        internal void Add(ApiCall call)
            => _apiCalls.Enqueue(call);

        public IEnumerator<ApiCall> GetEnumerator()
            => _apiCalls.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => _apiCalls.GetEnumerator();
    }
}
