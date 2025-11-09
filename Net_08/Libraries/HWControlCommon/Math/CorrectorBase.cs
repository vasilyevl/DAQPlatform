using Newtonsoft.Json.Linq;

namespace SDAQFramework.Math
{
    public interface ICorrector
    {
        Int64 Id { get; }
        String Name { get; }
        abstract ICorrector FromJson(String json);
        Double Evaluate(Double x);
        Double[] Evaluate(Double[] xs);
        String ToString();
    }

    /// <summary>
    /// Base class for correctors. Implements common ICorrector members:
    /// - Id / Name assignment (thread-safe counter)
    /// - default ToString()
    /// - vectorized Evaluate(double[]) implementation that calls Evaluate(double)
    /// Derived classes must implement Evaluate(double) and static FromJson(string).
    /// </summary>
    public abstract class CorrectorBase : ICorrector
    {
        private static long s_nextId = 0;

        public CorrectorBase(string? name) {

            Id = Interlocked.Increment(ref s_nextId);
            Name = string.IsNullOrWhiteSpace(name) ? $"{GetType().Name}_{Id}" : name.Trim();
        }

        public long Id {
            get;
        }
        public string Name {
            get;
        }

        public abstract ICorrector FromJson(string json);


        // Each derived type must provide a static ICorrector FromJson(string)
        // (static abstract members cannot be implemented here).

        // Single-value evaluation implemented by derived classes.
        public abstract double Evaluate(double x);

        // Vectorized evaluation implemented once here.
        public virtual double[] Evaluate(double[] xs) {
            if (xs == null)
                throw new ArgumentNullException(nameof(xs));
            var results = new double[xs.Length];
            for (int i = 0; i < xs.Length; i++)
                results[i] = Evaluate(xs[i]);
            return results;
        }

        public override string ToString() => $"{Name} (Id={Id})";


        protected static bool ParseBoolToken(JToken t) {
            if (t == null) {
                return false;
            }

            if (t.Type == JTokenType.Boolean) {
                return t.Value<bool>();
            }

            if (t.Type == JTokenType.String && bool.TryParse(t.Value<string>(), out var b)) {
                return b;
            }

            if ((t.Type == JTokenType.Integer || t.Type == JTokenType.Float) && t.Value<int>() != 0) {
                return true;
            }

            return false;
        }

        protected static double ParseNumberToken(JToken t) {
            if (t.Type == JTokenType.Integer || t.Type == JTokenType.Float) {
                return t.Value<double>();
            }

            if (t.Type == JTokenType.String && double.TryParse(t.Value<string>(), out var v)) {
                return v;
            }

            throw new FormatException("Numeric value expected.");
        }

    }



}