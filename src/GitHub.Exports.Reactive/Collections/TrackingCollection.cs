using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Reactive.Linq;
using GitHub.Helpers;
using ReactiveUI;

namespace GitHub.Collections
{
    public class TrackingCollection<T> : ObservableCollection<T>, ITrackingCollection<T>, IEnumerable<T> where T : ICopyable<T>
    {
        IDisposable subscription;
        IObservable<T> source;
        Func<T, T, int> comparer;

        public TrackingCollection()
        {
        }

        public TrackingCollection(IObservable<T> source)
        {
            Listen(source);
        }

        public ITrackingCollection<T> Listen(IObservable<T> obs)
        {
            source = obs
                .ObserveOn(RxApp.MainThreadScheduler)
                .Do(t =>
                {
                    var idx = IndexOf(t);
                    if (idx >= 0)
                    {
                        var old = this[idx];
                        var comparison = 0;
                        if (comparer != null)
                            comparison = comparer(t, old);
                        this[idx].CopyFrom(t);
                        if (comparer == null) return; // no sorting to be done, just replacing the element in-place
                        if (comparison < 0) // replacing element has lower sorting order, find the correct spot towards the beginning
                        {
                            var i = idx;
                            for (; i > 0 && comparer(this[i], this[i-1]) < 0; i--)
                                Swap(i, i-1);
                            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
                            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, old, i, idx));

                        }
                        else if (comparison > 0) // replacing element has higher sorting order, find the correct spot towards the end
                        {
                            var i = idx;
                            for (; i < Count - 1 && comparer(this[i], this[i+1]) > 0; i++)
                                Swap(i, i+1);
                            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
                            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, old, i, idx));
                        }
                    }
                    // the element doesn't exist yet. If there's a comparer and other items, add it in the correct spot
                    else if (comparer != null && Count > 0)
                    {
                        var i = 0;
                        for (; i < Count && comparer(t, this[i]) > 0; i++) { }
                        if (i == Count)
                            Add(t);
                        else
                            Insert(i, t);
                    }
                    else
                        Add(t);
                })
                .Publish()
                .RefCount();
            return this;
        }

        public void SetComparer(Func<T, T, int> compare)
        {
            comparer = compare;
            UpdateSort();
        }

        void UpdateSort()
        {
            if (comparer == null)
                return;

            for (var start = 0; start < Count - 1; start++)
            {
                var idxSmallest = start;
                for (var i = start + 1; i < Count; i++)
                    if (comparer(this[i], this[idxSmallest]) < 0)
                        idxSmallest = i;
                if (idxSmallest == start) continue;
                Swap(start, idxSmallest);
                OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, this[start], start, idxSmallest));
            }
        }

        void Swap(int left, int right)
        {
            var l = this[left];
            this[left] = this[right];
            this[right] = l;           
        }

        public IDisposable Subscribe()
        {
            subscription = source.Subscribe();
            return this;
        }

        public void Dispose()
        {
            subscription?.Dispose();
            GC.SuppressFinalize(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Items.GetEnumerator();
        }
    }
}
    