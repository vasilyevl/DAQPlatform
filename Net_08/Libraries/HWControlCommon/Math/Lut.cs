using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace SDAQFramework.Math
{
    /// <summary>
    /// Look-up table corrector: stores (x,y) samples and provides
    /// evaluation by linear interpolation (and linear extrapolation).
    /// </summary>
    public class Lut : CorrectorBase
    {
        private readonly double[] _xs;
        private readonly double[] _ys;

        public int Count => _xs.Length;
        /// <summary>
        /// When true (default) the LUT will linearly extrapolate outside
        /// the first/last sample segment. When false out-of-range inputs
        /// are clipped to the endpoint y values.
        /// </summary>
        public bool AllowExtrapolation { get; } = true;


        public Lut(string? name, IEnumerable<(double x, double y)> samples, bool allowExtrapolation) : base(name) {

            AllowExtrapolation = allowExtrapolation;

            if (samples == null) {

                throw new ArgumentNullException(nameof(samples));
            }

            var dict = new SortedDictionary<double, double>();
            foreach (var (x, y) in samples) {

                dict[x] = y;
            }

            if (dict.Count == 0) {

                throw new ArgumentException(
                    "At least one sample is required.", 
                    nameof(samples));
            }

            _xs = dict.Keys.ToArray();
            _ys = dict.Values.ToArray();
        }

        public Lut(string? name, double[] xs, double[] ys, bool allowExtrapolation) : base(name) {

            AllowExtrapolation = allowExtrapolation;

            if (xs == null) {
                throw new ArgumentNullException(nameof(xs));
            }

            if (ys == null) {
                throw new ArgumentNullException(nameof(ys));
            }

            if (xs.Length != ys.Length) {
                throw new ArgumentException("xs and ys must have same length.");
            }

            if (xs.Length == 0) {
                throw new ArgumentException("At least one sample is required.");
            }

            var dict = new SortedDictionary<double, double>();
            for (int i = 0; i < xs.Length; i++) {
                dict[xs[i]] = ys[i];
            }

            _xs = dict.Keys.ToArray();
            _ys = dict.Values.ToArray();
        }

        override public ICorrector FromJson(string json) {
            if (string.IsNullOrWhiteSpace(json)) {
                throw new ArgumentException(
                    "JSON must be provided.",
                    nameof(json));
            }

            var root = JToken.Parse(json);
            string? name = null;
            IEnumerable<(double x, double y)> samples = null!;
            bool allowExtrapolation = true; // default

            if (root.Type == JTokenType.Object) {
                var obj = (JObject)root;

                if (obj.TryGetValue("name", StringComparison.OrdinalIgnoreCase, out var nameToken)
                    && nameToken.Type == JTokenType.String) {
                    name = nameToken.Value<string>();
                }

                // optional boolean control for extrapolation ("allowExtrapolation" or "extrapolate")
                if (obj.TryGetValue("allowExtrapolation", StringComparison.OrdinalIgnoreCase, out var allowToken) ||
                    obj.TryGetValue("extrapolate", StringComparison.OrdinalIgnoreCase, out allowToken)) {
                    allowExtrapolation = ParseBoolToken(allowToken);
                }

                if (obj.TryGetValue("points", StringComparison.OrdinalIgnoreCase, out var pointsToken)
                    && pointsToken.Type == JTokenType.Array) {
                    samples = ParsePointsArray((JArray) pointsToken);
                }
                else if (obj.TryGetValue("xs", StringComparison.OrdinalIgnoreCase, out var xsToken)
                      && obj.TryGetValue("ys", StringComparison.OrdinalIgnoreCase, out var ysToken)
                      && xsToken.Type == JTokenType.Array && ysToken.Type == JTokenType.Array) {
                    var xs = ((JArray)xsToken).Select(t => ParseNumberToken(t)).ToArray();
                    var ys = ((JArray)ysToken).Select(t => ParseNumberToken(t)).ToArray();

                    if (xs.Length != ys.Length) {
                        throw new FormatException(
                            "xs and ys arrays must have equal length.");
                    }

                    samples = xs.Zip(ys, (x, y) => (x, y)).ToArray();
                }
                else {
                    throw new FormatException(
                        "Object JSON must contain 'points' array or parallel 'xs' and 'ys' arrays.");
                }
            }
            else if (root.Type == JTokenType.Array) {
                // array root -> samples only, keep default allowExtrapolation
                samples = ParsePointsArray((JArray) root);
            }
            else {
                throw new FormatException(
                    "Expected JSON array or object for LUT.");
            }

            return new Lut(name, samples, allowExtrapolation);
        }

     
        private static IEnumerable<(double x, double y)> ParsePointsArray(JArray arr) {
            var list = new List<(double x, double y)>(arr.Count);

            foreach (var item in arr) {
                if (item.Type == JTokenType.Array) {
                    var a = (JArray)item;
                    if (a.Count < 2) {
                        throw new FormatException(
                            "Point array must have at least " +
                            "two elements [x,y].");
                    }

                    double x = ParseNumberToken(a[0]);
                    double y = ParseNumberToken(a[1]);
                    list.Add((x, y));
                }
                else if (item.Type == JTokenType.Object) {
                    var o = (JObject)item;

                    if (!o.TryGetValue("x", StringComparison.OrdinalIgnoreCase, out var xt) ||
                        !o.TryGetValue("y", StringComparison.OrdinalIgnoreCase, out var yt)) {
                        throw new FormatException(
                            "Point object must contain 'x' and 'y' properties.");
                    }

                    double x = ParseNumberToken(xt);
                    double y = ParseNumberToken(yt);
                    list.Add((x, y));
                }
                else {
                    throw new FormatException(
                        "Point must be an array [x,y] or object {x:..,y:..}.");
                }
            }

            return list;
        }

  

        public override double Evaluate(double x) {
            if (_xs.Length == 1) {
                return _ys[0];
            }

            int idx = Array.BinarySearch(_xs, x);
            if (idx >= 0) {
                return _ys[idx];
            }

            idx = ~idx;
            if (idx == 0) {
                if (!AllowExtrapolation) {
                    return _ys[0];
                }

                return LinearInterp(_xs[0], _ys[0], _xs[1], _ys[1], x);
            }

            if (idx >= _xs.Length) {
                if (!AllowExtrapolation) {
                    return _ys[_ys.Length - 1];
                }

                int n = _xs.Length;
                return LinearInterp(_xs[n - 2], _ys[n - 2], _xs[n - 1], _ys[n - 1], x);
            }

            return LinearInterp(_xs[idx - 1], _ys[idx - 1], _xs[idx], _ys[idx], x);
        }

        private static double LinearInterp(double x0, double y0, double x1, double y1, double x) {
            if (x1 == x0) {
                return y0;
            }

            double t = (x - x0) / (x1 - x0);
            return y0 + t * (y1 - y0);
        }

        // Evaluate(double[]) is inherited from CorrectorBase.

        public override string ToString() => $"{Name} (Id={Id}, Count={Count})";
    }
}