﻿#region License

/*
 Copyright 2014 - 2015 Nikita Bernthaler
 ProducerConsumer.cs is part of SFXLibrary.

 SFXLibrary is free software: you can redistribute it and/or modify
 it under the terms of the GNU General Public License as published by
 the Free Software Foundation, either version 3 of the License, or
 (at your option) any later version.

 SFXLibrary is distributed in the hope that it will be useful,
 but WITHOUT ANY WARRANTY; without even the implied warranty of
 MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 GNU General Public License for more details.

 You should have received a copy of the GNU General Public License
 along with SFXLibrary. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion License

namespace SFXLibrary
{
    #region

    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    #endregion

    public abstract class ProducerConsumer<T>
    {
        private const int CheckInterval = 6000;
        private readonly int _minConsumers;

        private readonly Dictionary<CancellationTokenSource, Task> _pool =
            new Dictionary<CancellationTokenSource, Task>();

        private readonly int _producersPerConsumer;
        private readonly BlockingCollection<T> _queue = new BlockingCollection<T>();
        private int _lastCheck;
        private int _requestedStarting;
        private int _requestedStopping;
        private int _started;

        protected ProducerConsumer(int minConsumers = 1, int producersPerConsumer = 5)
        {
            _minConsumers = minConsumers;
            _producersPerConsumer = producersPerConsumer;
            ManageConsumers();
        }

        public void AddItem(T item)
        {
            if (_queue != null)
            {
                _queue.Add(item);
                ManageConsumers();
            }
        }

        public void CompleteLogging()
        {
            if (_queue != null)
            {
                _queue.CompleteAdding();
            }
        }

        private void StartConsumers(int count)
        {
            _requestedStarting += count;
            for (var i = 0; count > i; i++)
            {
                var token = new CancellationTokenSource();
                _pool.Add(token, Task.Factory.StartNew(() => Consume(token), token.Token));
            }
        }

        private void StopConsumers(int count)
        {
            _requestedStopping += count;
            var i = 0;
            foreach (var consumer in _pool.ToList())
            {
                if (i >= count)
                    break;
                consumer.Key.Cancel();
                i++;
            }
        }

        private void ManageConsumers()
        {
            if (_queue.IsAddingCompleted && _pool.Count == 0)
                return;

            var consumers = _started + _requestedStarting - _requestedStopping;

            if (_queue.IsAddingCompleted)
            {
                StopConsumers(consumers);
            }
            else
            {
                if (_minConsumers > consumers)
                {
                    StartConsumers(_minConsumers - consumers);
                }
                else
                {
                    var consumersToRun = Convert.ToInt32(Math.Ceiling((double) _queue.Count/_producersPerConsumer));
                    consumersToRun = consumersToRun < _minConsumers ? _minConsumers : consumersToRun;
                    if (consumersToRun > consumers)
                    {
                        StartConsumers(consumersToRun - consumers);
                    }
                    else if (consumersToRun < consumers)
                    {
                        if (CheckInterval + _lastCheck <= Environment.TickCount)
                        {
                            StopConsumers(consumers - consumersToRun);
                            _lastCheck = Environment.TickCount;
                        }
                    }
                }
            }
        }

        private void Consume(CancellationTokenSource token)
        {
            _started++;
            _requestedStarting--;
            foreach (var item in _queue.GetConsumingEnumerable())
            {
                ProcessItem(item);
                if (token.IsCancellationRequested)
                {
                    _pool.Remove(token);
                    _requestedStopping--;
                    _started--;
                    break;
                }
                ManageConsumers();
            }
        }

        protected abstract void ProcessItem(T item);
    }
}