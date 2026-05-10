using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using NUnit.Framework;
using PhialeTech.PhialeGrid.Wpf.Controls;

namespace PhialeGrid.Wpf.Tests.Virtualization
{
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class GridVirtualizedCollectionViewTests
    {
        [Test]
        public void GetItemAt_UsesUnderlyingListDirectly()
        {
            var source = new TrackingList(new object[] { "A", "B", "C" });
            var view = new GridVirtualizedCollectionView(source);

            var value = view.GetItemAt(1);

            Assert.That(value, Is.EqualTo("B"));
            Assert.That(source.IndexerReadCount, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void SourceCollectionChanged_IsForwardedToView()
        {
            var source = new ObservableCollection<object>(new object[] { "A", "B" });
            var view = new GridVirtualizedCollectionView(source);
            NotifyCollectionChangedEventArgs receivedArgs = null;

            ((INotifyCollectionChanged)view).CollectionChanged += (sender, args) => receivedArgs = args;
            source.Add("C");

            Assert.That(receivedArgs, Is.Not.Null);
            Assert.That(receivedArgs.Action, Is.EqualTo(NotifyCollectionChangedAction.Add));
            Assert.That(view.Count, Is.EqualTo(3));
        }

        private sealed class TrackingList : IList
        {
            private readonly List<object> _items;

            public TrackingList(IEnumerable<object> items)
            {
                _items = new List<object>(items);
            }

            public int IndexerReadCount { get; private set; }

            public int Add(object value) => throw new System.NotSupportedException();

            public void Clear() => throw new System.NotSupportedException();

            public bool Contains(object value) => _items.Contains(value);

            public int IndexOf(object value) => _items.IndexOf(value);

            public void Insert(int index, object value) => throw new System.NotSupportedException();

            public bool IsFixedSize => true;

            public bool IsReadOnly => true;

            public void Remove(object value) => throw new System.NotSupportedException();

            public void RemoveAt(int index) => throw new System.NotSupportedException();

            public object this[int index]
            {
                get
                {
                    IndexerReadCount++;
                    return _items[index];
                }
                set => throw new System.NotSupportedException();
            }

            public void CopyTo(System.Array array, int index) => _items.ToArray().CopyTo(array, index);

            public int Count => _items.Count;

            public bool IsSynchronized => false;

            public object SyncRoot => this;

            public IEnumerator GetEnumerator() => _items.GetEnumerator();
        }
    }
}

