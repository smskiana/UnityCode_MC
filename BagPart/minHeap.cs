using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bags
{
    [Serializable]
    public class SerializableMinHeap<T> where T : IComparable<T>
    {
        [SerializeField]private List<T> heap = new();
        public int Count => heap.Count;

        public void Add(T item)
        {
            heap.Add(item);
            HeapifyUp(heap.Count - 1);
        }

        public bool Contain(T item)
        {
            return heap.Contains(item);
        }

        public T Pop()
        {
            if (heap.Count == 0) throw new InvalidOperationException("Heap is empty");

            T min = heap[0];
            heap[0] = heap[heap.Count - 1];
            heap.RemoveAt(heap.Count - 1);
            if (heap.Count > 0)
                HeapifyDown(0);

            return min;
        }

        public T Peek()
        {
            if (heap.Count == 0) throw new InvalidOperationException("Heap is empty");
            return heap[0];
        }

        // 删除指定元素
        public bool Remove(T item)
        {
            int index = heap.IndexOf(item);
            if (index == -1)
                return false; // 不存在该元素

            int last = heap.Count - 1;

            // 如果删除的是最后一个，直接移除
            if (index == last)
            {
                heap.RemoveAt(last);
                return true;
            }

            // 用最后一个值覆盖
            heap[index] = heap[last];
            heap.RemoveAt(last);

            // 恢复堆结构
            HeapifyDown(index);
            HeapifyUp(index);

            return true;
        }

        private void HeapifyUp(int index)
        {
            while (index > 0)
            {
                int parent = (index - 1) / 2;
                if (heap[index].CompareTo(heap[parent]) >= 0) break;
                Swap(index, parent);
                index = parent;
            }
        }

        private void HeapifyDown(int index)
        {
            int last = heap.Count - 1;

            while (true)
            {
                int left = index * 2 + 1;
                int right = index * 2 + 2;
                int smallest = index;

                if (left <= last && heap[left].CompareTo(heap[smallest]) < 0)
                    smallest = left;

                if (right <= last && heap[right].CompareTo(heap[smallest]) < 0)
                    smallest = right;

                if (smallest == index) break;

                Swap(index, smallest);
                index = smallest;
            }
        }

        private void Swap(int i, int j)
        {
            T tmp = heap[i];
            heap[i] = heap[j];
            heap[j] = tmp;
        }

        // 用于在从 Inspector 修改数据后重建堆
        public void RebuildHeap()
        {
            for (int i = heap.Count / 2 - 1; i >= 0; i--)
            {
                HeapifyDown(i);
            }
        }

        public void Clear()
        {
            heap.Clear();
        }
    }
}
