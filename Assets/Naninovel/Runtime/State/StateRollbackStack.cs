// Copyright 2017-2019 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A limited-size dropout stack containing <see cref="GameStateMap"/> objects.
    /// </summary>
    public class StateRollbackStack
    {
        [System.Serializable]
        private class SerializedStack
        {
            public List<GameStateMap> List = default;

            public SerializedStack (List<GameStateMap> list)
            {
                List = list;
            }
        }

        /// <summary>
        /// The stack will dropout elements when the capacity is exceeded.
        /// </summary>
        public readonly int Capacity;
        /// <summary>
        /// Number of elements in stack.
        /// </summary>
        public int Count => rollbackList.Count;

        private readonly LinkedList<GameStateMap> rollbackList = new LinkedList<GameStateMap>();

        public StateRollbackStack (int capacity)
        {
            Capacity = capacity;
        }

        public void Push (GameStateMap item)
        {
            rollbackList.AddFirst(item);

            if (rollbackList.Count > Capacity)
                rollbackList.RemoveLast();
        }

        public GameStateMap Peek () => rollbackList.First?.Value;

        public GameStateMap Pop ()
        {
            if (Count == 0) return null;

            var item = rollbackList.First.Value;
            rollbackList.RemoveFirst();
            return item;
        }

        public GameStateMap Pop (PlaybackSpot playbackSpot)
        {
            var spotFound = false;
            var node = rollbackList.First;
            while (node != null)
            {
                if (node.Value.PlaybackSpot == playbackSpot) { spotFound = true; break; }
                node = node.Next;
            }
            if (!spotFound) return null;

            while (rollbackList.First != node)
                Pop();

            return Pop();
        }

        public void Clear () => rollbackList.Clear();

        public void OverrideFromJson (string json)
        {
            Clear();

            if (string.IsNullOrEmpty(json)) return;

            var serializedStack = JsonUtility.FromJson<SerializedStack>(json);
            if (serializedStack?.List is null || serializedStack.List.Count == 0) return;

            foreach (var item in serializedStack.List)
                Push(item);
        }

        public string ToJson (int maxSize)
        {
            var rangeCount = Mathf.Min(maxSize, rollbackList.Count);
            if (rangeCount == 0) return null;

            var list = rollbackList.Reverse().ToList().GetRange(rollbackList.Count - rangeCount, rangeCount);
            var serializedStack = new SerializedStack(list);
            return JsonUtility.ToJson(serializedStack);
        }
    }
}
