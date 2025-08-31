/*
Copyright (c) 2024 vasilyevl (Grumpy). Permission is hereby granted, 
free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"),to deal in the Software 
without restriction, including without limitation the rights to use, copy, 
modify, merge, publish, distribute, sublicense, and/or sell copies of the 
Software, and to permit persons to whom the Software is furnished to do so, 
subject to the following conditions:

The above copyright notice and this permission notice shall be included 
in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,FITNESS FOR A 
PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE 
OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grumpy.SDAQFramework.MathUtilities
{
    /// <summary>
    /// Represents the result of statistical accumulation, including mean, min, max, and standard deviations.
    /// </summary>
    public class StatResult
    {
        private double _accumulatedSquaredValue;
        private double _populationStandardDeviation;
        private double _sampleStandardDeviation;

        /// <summary>
        /// Initializes a new instance of the <see cref="StatResult"/> class.
        /// </summary>
        /// <param name="accumulatedValue">The sum of all values added.</param>
        /// <param name="accumulatedSquaredValue">The sum of the squares of all values added.</param>
        /// <param name="count">The number of values accumulated.</param>
        /// <param name="min">The minimum value encountered.</param>
        /// <param name="max">The maximum value encountered.</param>
        public StatResult(double accumulatedValue,
            double accumulatedSquaredValue,
            int count, double min, double max)
        {
            AccumulatedValue = accumulatedValue;
            _accumulatedSquaredValue = accumulatedSquaredValue;

            Count = count;
            MinValue = min;
            MaxValue = max;

            _populationStandardDeviation = System.Math.Sqrt(
                (accumulatedSquaredValue / count) -
                ((accumulatedValue * accumulatedValue) / (count * count)));

            _sampleStandardDeviation = System.Math.Sqrt(
                ((accumulatedSquaredValue -
                (accumulatedValue * accumulatedValue / count))) / (count - 1));
        }

        /// <summary>
        /// Gets the sum of all values added.
        /// </summary>
        public double AccumulatedValue { get; private set; }

        /// <summary>
        /// Gets the population standard deviation of the accumulated values.
        /// </summary>
        public double PopulationStdev => _populationStandardDeviation;

        /// <summary>
        /// Gets the sample standard deviation of the accumulated values.
        /// </summary>
        public double SampleSedev => _sampleStandardDeviation;

        /// <summary>
        /// Gets the minimum value encountered.
        /// </summary>
        public double MinValue { get; private set; }

        /// <summary>
        /// Gets the maximum value encountered.
        /// </summary>
        public double MaxValue { get; private set; }

        /// <summary>
        /// Gets the number of values accumulated.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Gets the mean (average) of the accumulated values.
        /// </summary>
        public double Mean => Count > 0 ? AccumulatedValue / Count : 0;

        /// <summary>
        /// Returns a string representation of the statistical result, with formatted values.
        /// </summary>
        /// <returns>A string containing the count, accumulated value, min, max, mean, and standard deviations.</returns>
        public override string ToString()
        {
            string Format(double value)
            {
                if (System.Math.Abs(value) > 1000 ||
                    (System.Math.Abs(value) < 1.0 && value != 0))
                    return value.ToString("0.000E+00");
                else
                    return value.ToString("0.000");
            }

            return $"Mean: {Format(Mean)}\n" +
                   $"Min: {Format(MinValue)}, Max: {Format(MaxValue)}\n" +
                   $"Sample Stdev: {Format(SampleSedev)}\n" +
            $"Count: {Count}\n" +
                   $"Accumulated Value: {Format(AccumulatedValue)}\n" +
                   $"Population Stdev: {Format(PopulationStdev)}\n";
                   
        }
    }

    /// <summary>
    /// Accumulates statistical data (sum, min, max, count, standard deviations) for a sequence of values.
    /// </summary>
    public class StatAccumulator
    {
        private double _accumulatedValue;
        private double _accumulatedSquaredValue;
        private int _count;
        private double _min;
        private double _max;
        private int _samplesToSkip;
        private object _lock;
        private bool _isRunning;
        private int _maxCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="StatAccumulator"/> class.
        /// </summary>
        /// <param name="samplesToSkip">The number of initial samples to skip before accumulating statistics.</param>
        /// <param name="autoStart">Whether to start accumulating immediately.</param>
        /// <param name="maxCount">The maximum number of samples to accumulate. Use -1 for unlimited.</param>
        public StatAccumulator(int samplesToSkip = 0, bool autoStart = true, int maxCount = -1)
        {
            _lock = new object();
            Reset();
            _maxCount = maxCount;
            _samplesToSkip = samplesToSkip;
            if (autoStart) {
                this.Start();
            }

            _maxCount = maxCount;
        }

        /// <summary>
        /// Resets the accumulator to its initial state.
        /// </summary>
        /// <param name="start">If true, the accumulator will be set to running after reset.</param>
        public void Reset(bool start = false)
        {
            lock (_lock) {
                _accumulatedValue = 0;
                _accumulatedSquaredValue = 0;
                _count = 0;
                _min = double.MaxValue;
                _max = double.MinValue;
                _isRunning = start;
            }
        }

        /// <summary>
        /// Adds a value to the accumulator.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public void AddValue(double value)
        {
            try {
                lock (_lock) {
                    if (_isRunning) {

                        if (_isRunning &&
                            (_maxCount <= 0 || (_count - _samplesToSkip) >= _maxCount)) {

                            if ((_samplesToSkip <= 0 || _count >= _samplesToSkip)) {

                                _accumulatedValue += value;
                                _accumulatedSquaredValue += value * value;
                                _min = System.Math.Min(_min, value);
                                _max = System.Math.Max(_max, value);
                            }
                            _count++;
                        }
                    }
                }
            }
            catch (Exception ex) {
                Console.WriteLine($"Add Value Exception: {ex}");
            }
        }

        /// <summary>
        /// Starts or restarts the accumulator, resetting all statistics.
        /// </summary>
        public void Start()
        {
            lock (_lock) {
                Reset(true);
            }
        }

        /// <summary>
        /// Stops accumulating values.
        /// </summary>
        public void Stop()
        {
            lock (_lock) {
                _isRunning = false;
            }
        }

        /// <summary>
        /// Resumes accumulating values without resetting statistics.
        /// </summary>
        public void Resume()
        {
            lock (_lock) {
                _isRunning = true;
            }
        }

        /// <summary>
        /// Gets the current statistical result.
        /// </summary>
        public StatResult Result
        {
            get {
                lock (_lock) {
                    return new StatResult(_accumulatedValue,
                                      _accumulatedSquaredValue,
                                      _count - _samplesToSkip,
                                      _min, _max);
                }
            }
        }
    }
}
