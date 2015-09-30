using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using GitHub.Helpers;

namespace GitHub.Collections
{
    public interface ITrackingCollection<T> : IDisposable, IList<T>, IList, IReadOnlyList<T> where T : ICopyable<T>
    {
        ITrackingCollection<T> Listen(IObservable<T> obs);
        IDisposable Subscribe();
        void SetComparer(Func<T, T, int> comparer);
        event NotifyCollectionChangedEventHandler CollectionChanged;
    }
}