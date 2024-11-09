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

namespace Grumpy.DAQFramework.Common
{
    public interface IIntegrator<T>
    {
        T Accumulator { get; }
        int Counter { get; }
        bool CanIntegrate { get; }

        void Reset(bool start = false);
        int Add(T value);
        T CurrentAverage { get; }
    }

    public class DoubleIntegrator:IIntegrator<double>
    {
        private double _accumulator;
        private int _counter;
        private bool _canIntergrate;
        public DoubleIntegrator()
        {
            Reset();
        }

        public double Accumulator {
            get {
                double r;
                Thread.MemoryBarrier();
                r = _accumulator;
                Thread.MemoryBarrier();
                return r;
            }

            private set {
                Thread.MemoryBarrier();
                _accumulator = value;
                Thread.MemoryBarrier();
            }
        }

        public int Counter {
            get {
                int r;
                Thread.MemoryBarrier();
                r = _counter;
                Thread.MemoryBarrier();
                return r;
            }

            private set {
                Thread.MemoryBarrier();
                _counter = value;
                Thread.MemoryBarrier();
            }
        }

        public bool CanIntegrate {
            get {
                bool r;
                Thread.MemoryBarrier();
                r = _canIntergrate;
                Thread.MemoryBarrier();
                return r;
            }
            set {
                Thread.MemoryBarrier();
                _canIntergrate = value;
                Thread.MemoryBarrier();
            }
        }
        public void Reset(bool enable = false)
        {
            Accumulator = 0.0;
            Counter = 0;
            CanIntegrate = enable;
        }

        public int Add(double value)
        {
            if (_canIntergrate) {
                Accumulator += value;
                Counter++;
            }
                return Counter;
        }

        public double CurrentAverage => (Counter == 0) ? double.NaN : Accumulator / Counter;
    }

    public class DoubleArrayIntegrator : IIntegrator<double[]>
    {
        private DoubleIntegrator[] _integrators;
        public DoubleArrayIntegrator( int nuberOfChannels)
        {
            _integrators = new DoubleIntegrator[nuberOfChannels];
            
            for(int i = 0; i < nuberOfChannels; i++) {

                _integrators[i] = new DoubleIntegrator();   
            }
            Reset();
        }

        public int Counter => _integrators[0].Counter;

        public double[] Accumulator {
            get {
                double[] r = new double[_integrators.Length];
                 
                for (int i = 0; i <= _integrators.Length; i++) {
               
                    r[i] = _integrators[i].Accumulator;
                }
                return r;         
            }
        }

        public void Reset(bool enable = false)
        {
            for (int i = 0; i < _integrators.Length; i++) {
             
                _integrators[i].Reset();
            }
        }

        public int Add( double[] newSet)
        {
            for (int i = 0; i < _integrators.Length; i++) {
                
                _integrators[i].Add(newSet[i]);
            }

            return _integrators[0].Counter;
        }

        public bool CanIntegrate {
            get {
                foreach (var integrator in _integrators) {
                
                    if (!integrator.CanIntegrate) {
                    
                        return false;
                    }
                }
                return true;
            }
            set {
                foreach (var integrator in _integrators) {
                    
                    integrator.CanIntegrate = value;
                }
            }
            
        }

        public double[] CurrentAverage {
            get {

                 double[] currentAverage = new double[_integrators.Length];
                
                for (int i = 0; i < _integrators.Length; i++) {
                    
                    currentAverage[i] = _integrators[i].CurrentAverage;
                }

                return currentAverage;
            }
        }
    }
}
