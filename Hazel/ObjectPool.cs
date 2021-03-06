﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;

namespace Hazel
{
    /// <summary>
    ///     A fairly simple object pool for items that will be created a lot.
    /// </summary>
    /// <typeparam name="T">The type that is pooled.</typeparam>
    /// <threadsafety static="true" instance="true"/>
    public sealed class ObjectPool<T> where T : IRecyclable
    {
        private int numberCreated;
        public int NumberCreated { get { return numberCreated; } }
        public int NumberInUse { get { return this.inuse.Count; } }

        public int Size { get { return this.pool.Count; } }

        /// <summary>
        ///     Our pool of objects
        /// </summary>
        ConcurrentBag<T> pool = new ConcurrentBag<T>();

        private ConcurrentDictionary<T, bool> inuse = new ConcurrentDictionary<T, bool>();

        /// <summary>
        ///     The generator for creating new objects.
        /// </summary>
        /// <returns></returns>
        Func<T> objectFactory;

        /// <summary>
        ///     Internal constructor for our ObjectPool.
        /// </summary>
        internal ObjectPool(Func<T> objectFactory)
        {
            this.objectFactory = objectFactory;
        }

        /// <summary>
        ///     Returns a pooled object of type T, if none are available another is created.
        /// </summary>
        /// <returns>An instance of T.</returns>
        internal T GetObject()
        {
            T item;
            if (!pool.TryTake(out item))
            {
                Interlocked.Increment(ref numberCreated);
                item = objectFactory.Invoke();
            }

            if (!inuse.TryAdd(item, true))
            {
                throw new Exception("Duplicate pull");
            }

            return item;
        }

        /// <summary>
        ///     Returns an object to the pool.
        /// </summary>
        /// <param name="item">The item to return.</param>
        internal void PutObject(T item)
        {
            if (inuse.TryRemove(item, out bool b))
            {
                pool.Add(item);
            }
            else
            {
                throw new Exception("Duplicate add");
            }
        }
    }
}
