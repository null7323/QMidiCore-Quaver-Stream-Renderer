using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using SharpExtension;
using SharpExtension.Collections;

namespace QQS_UI.Core
{
    /// <summary>
    /// 对音符序列进行排序. 这是.NET BCL中Array.Sort对于<see cref="UnmanagedList{Note}"/>的特化版本.
    /// </summary>
    public static unsafe class NoteSorter
    {
        // 实现: https://referencesource.microsoft.com/#q=GenericArraySortHelper<T>.IntroSort"

        /// <summary>
        /// 获取递归的深度.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static long GetRecursionDepth(long len)
        {
            long res = 0;
            while (len >= 1)
            {
                ++res;
                len /= 2;
            }
            return res;
        }
        /// <summary>
        /// 如果 a处元素 > b处元素, 进行交换
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SwapIfGreater(in Note* collection, in long a, in long b)
        {
            if (a != b)
            {
                if (!Compare(collection[a], collection[b]))
                {
                    Note n = collection[a];
                    collection[a] = collection[b];
                    collection[b] = n;
                }
            }
        }
        /// <summary>
        /// 对前后 Note 进行比较.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Compare(in Note left, in Note right)
        {
            // 如果符合以下条件 (返回true), 那么 left < right, 也就是 left 应该排在 right 的前面.
            return left.Start < right.Start || (left.Track < right.Track && left.Start == right.Start);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void InsertionSort(in Note* collection, in long low, in long high)
        {
            long i, j;
            Note t;
            for (i = low; i != high; ++i)
            {
                j = i;
                t = collection[i + 1];

                while (j >= low && Compare(t, collection[j]))
                {
                    collection[j + 1] = collection[j];
                    --j;
                }

                collection[j + 1] = t;
            }
        }
        /// <summary>
        /// 内省排序.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private static void IntroSort(in Note* collection, in long low, long high, long depth)
        {
            while (high > low)
            {
                long partitionSize = high - low + 1;
                if (partitionSize < 16)
                {
                    if (partitionSize == 1)
                    {
                        return;
                    }
                    if (partitionSize == 2)
                    {
                        SwapIfGreater(collection, low, high);
                    }
                    if (partitionSize == 3)
                    {
                        SwapIfGreater(collection, low, high - 1);
                        SwapIfGreater(collection, low, high);
                        SwapIfGreater(collection, high - 1, high);
                        return;
                    }
                    InsertionSort(collection, low, high);
                }
                if (depth == 0)
                {
                    HeapSort(collection, low, high);
                    return;
                }
                --depth;

                long p = PickPivotAndPartition(collection, low, high);
                IntroSort(collection, p + 1, high, depth);
                high = p - 1;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DownHeap(in Note* collection, long i, in long n, in long low)
        {
            Note d = collection[low + i - 1];
            long child;
            while (i <= n / 2)
            {
                child = 2 * i;
                if (child < n && Compare(collection[low + child - 1], collection[low + child]))
                {
                    child++;
                }
                if (!Compare(d, collection[low + child - 1]))
                {
                    break;
                }

                collection[low + i - 1] = collection[low + child - 1];
                i = child;
            }
            collection[low + i - 1] = d;
        }
        /// <summary>
        /// 堆排序.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void HeapSort(in Note* collection, in long low, in long high)
        {
            long n = high - low + 1;
            for (long i = n / 2; i >= 1; --i)
            {
                DownHeap(collection, i, n, low);
            }
            for (long i = n; i > 1; --i)
            {
                //Swap(lo, lo + i - 1);
                Note note = collection[low];
                collection[low] = collection[low + i - 1];
                collection[low + i - 1] = note;

                DownHeap(collection, 1, i - 1, low);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort(UnmanagedList<Note> collection)
        {
            if (collection.Count < 2)
            {
                return;
            }
            lock (collection)
            {
                IntroSort((Note*)UnsafeMemory.GetActualAddressOf(ref collection[0]), 0, collection.Count - 1, collection.Count);
            }
        }

        private static long PickPivotAndPartition(in Note* collection, in long lo, in long hi)
        {
            long mid = lo + (hi - lo) / 2;
            SwapIfGreater(collection, lo, mid);
            SwapIfGreater(collection, lo, hi);
            SwapIfGreater(collection, mid, hi);

            Note pivot = collection[mid];
            //Swap(mid, hi - 1);
            Note m = collection[mid];
            collection[mid] = collection[hi - 1];
            collection[hi - 1] = m;

            long left = lo, right = hi - 1;

            while (left < right)
            {
                while (Compare(collection[++left], pivot))
                {

                }

                while (Compare(pivot, collection[--right]))
                {

                }

                if (left >= right)
                {
                    break;
                }

                m = collection[left];
                collection[left] = collection[right];
                collection[right] = m;
            }

            m = collection[left];
            collection[left] = collection[hi - 1];
            collection[hi - 1] = m;
            return left;
        }
    }
}
