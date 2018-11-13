namespace Solstice
{
    internal class InterpolatedValue
    {
        private readonly float baseValue;
        private readonly float factor;

        public InterpolatedValue(float baseValue, float factor)
        {
            this.baseValue = baseValue;
            this.factor = factor;
        }

        public float Calculate(float strength)
        {
            return baseValue + strength * factor;
        }
    }
}