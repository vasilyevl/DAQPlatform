using System;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace SDAQFramework.Math
{
    public interface ICorrector
    {
        Int64 Id {
            get;
        }
        String Name {
            get;
        }
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
    /// - ClipRange support: inputs are clipped to the range before evaluation
    /// Derived classes must implement EvaluateCore(double) and FromJson(string).
    /// </summary>
    public abstract class CorrectorBase : ICorrector
    {
        private static long s_nextId = 0;
        private (double Min, double Max)? _clipRange;
        /// <summary>
        /// Create a corrector with optional name and optional ClipRange.
        /// If clipRange is provided it will be normalized so Min <= Max.
        /// </summary>
        protected CorrectorBase(string? name, (double Min, double Max)? clipRange = null) {
            Id = Interlocked.Increment(ref s_nextId);
            Name = string.IsNullOrWhiteSpace(name) ? $"{GetType().Name}_{Id}" : name.Trim();
            ClipRange = clipRange;
        }


        public long Id {
            get;
        }
        public string Name {
            get;
        }

        // ClipRange defines allowed input range; if not set, inputs are not clipped.
        // Tuple: (Min, Max). Min must be <= Max when set.
        public (double Min, double Max)? ClipRange {
            get {
                return _clipRange;
            }
            set {
                if (value.HasValue) {
                    var v = value.Value;
                    // normalize so Min is the smaller and Max is the larger
                    double min = System.Math.Min(v.Min, v.Max);
                    double max = System.Math.Max(v.Min, v.Max);
                    _clipRange = (min, max);
                }
                else {
                    _clipRange = null;
                }
            }
        }

        // Factory parser required by the ICorrector contract.
        public abstract ICorrector FromJson(string json);

        // Derived types implement the core single-value evaluation here.
        // CorrectorBase.Evaluate performs clipping (if ClipRange set) and then delegates.
        public virtual double Evaluate(double x) {
            double xClipped = ClipInput(x);
            return EvaluateCore(xClipped);
        }

        // Vectorized evaluation implemented once here (uses Evaluate which applies clipping).
        public virtual double[] Evaluate(double[] xs) {
            if (xs == null) {
                throw new ArgumentNullException(nameof(xs));
            }

            var results = new double[xs.Length];
            for (int i = 0; i < xs.Length; i++) {
                results[i] = Evaluate(xs[i]);
            }

            return results;
        }

        public override string ToString() => $"{Name} (Id={Id})";

        // Clipping helper: if ClipRange has value, clamp to [Min, Max].
        protected double ClipInput(double x) {
            if (ClipRange.HasValue) {
                
                (Double min, Double max) = ClipRange.Value;
                
                if (min > max) {
                    // Treat invalid range as no-op (alternative: throw)
                    return x;
                }

                if (x < min) {
                    return min;
                }

                if (x > max) {
                    return max;
                }
            }

            return x;
        }

        // Derived classes implement this to evaluate a single (already-clipped) input.
        protected abstract double EvaluateCore(double x);

        // Helpers for JSON parsing reused by derived classes
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