using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using PhialeTech.PhialeGrid.Wpf.Surface.Presenters;

namespace PhialeTech.PhialeGrid.Wpf.Surface.Pools
{
    /// <summary>
    /// Zarządza pool'em kontenerów dla komórek i headerów.
    /// Recykluje kontenery zamiast tworzyć nowe, co drastycznie poprawia performance.
    /// </summary>
    public sealed class GridContainerPool : IDisposable
    {
        private readonly Dictionary<string, Stack<FrameworkElement>> _pools = 
            new Dictionary<string, Stack<FrameworkElement>>();

        private readonly Dictionary<string, Func<FrameworkElement>> _factories = 
            new Dictionary<string, Func<FrameworkElement>>();

        private const int MaxPoolSize = 500;

        /// <summary>
        /// Rejestruje factory dla danego typu kontenera.
        /// </summary>
        public void RegisterFactory(string containerType, Func<FrameworkElement> factory)
        {
            if (containerType == null)
                throw new ArgumentNullException(nameof(containerType));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            _factories[containerType] = factory;
            _pools[containerType] = new Stack<FrameworkElement>();
        }

        /// <summary>
        /// Pobiera kontener z pool'u lub tworzy nowy.
        /// </summary>
        public FrameworkElement AcquireContainer(string containerType)
        {
            if (containerType == null)
                throw new ArgumentNullException(nameof(containerType));

            if (!_pools.TryGetValue(containerType, out var pool))
            {
                pool = new Stack<FrameworkElement>();
                _pools[containerType] = pool;
            }

            if (pool.Count > 0)
            {
                return pool.Pop();
            }

            if (!_factories.TryGetValue(containerType, out var factory))
                throw new InvalidOperationException($"Brak factory dla typu '{containerType}'");

            return factory();
        }

        /// <summary>
        /// Zwraca kontener do pool'u.
        /// </summary>
        public void ReleaseContainer(string containerType, FrameworkElement container)
        {
            if (containerType == null || container == null)
                return;

            if (!_pools.TryGetValue(containerType, out var pool))
            {
                pool = new Stack<FrameworkElement>();
                _pools[containerType] = pool;
            }

            // Czyszczę kontener
            if (container is GridMasterDetailPresenter masterDetailPresenter)
            {
                masterDetailPresenter.OverlayData = null;
            }
            else if (container is ContentControl contentControl)
            {
                contentControl.Content = null;
            }

            // Nie dodaję do pool'u jeśli już zbyt wiele
            if (pool.Count < MaxPoolSize)
            {
                pool.Push(container);
            }
        }

        /// <summary>
        /// Czyści cały pool.
        /// </summary>
        public void Clear()
        {
            foreach (var kvp in _pools.Values)
            {
                kvp.Clear();
            }
        }

        /// <summary>
        /// Zwraca statystyki pool'u.
        /// </summary>
        public Dictionary<string, int> GetStatistics()
        {
            var stats = new Dictionary<string, int>();
            foreach (var kvp in _pools)
            {
                stats[kvp.Key] = kvp.Value.Count;
            }
            return stats;
        }

        public void Dispose()
        {
            Clear();
            _pools.Clear();
            _factories.Clear();
        }
    }
}
