using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Grumpy.DaqFramework.Common
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
