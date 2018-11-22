using System.Collections.Generic;
using UnityEngine;

namespace Solstice
{
    internal class Interpolator
    {
        private IDictionary<float, float> values = new SortedDictionary<float, float>();

        public void Clear()
        {
            this.values.Clear();
        }

        public float GetValue(float position)
        {
            float leftPosition = float.NaN;
            float leftValue = 0;

            foreach (KeyValuePair<float, float> entry in values)
            {
                if (entry.Key < position)
                {
                    leftPosition = entry.Key;
                    leftValue = entry.Value;
                    continue;
                }

                if (float.IsNaN(leftPosition))
                {
                    return entry.Value;
                }

                return Mathf.Lerp(leftValue, entry.Value, (position - leftPosition) / (entry.Key - leftPosition));
            }

            return leftValue;
        }

        public void Set(float key, float value)
        {
            this.values[key] = value;
        }
    }
}