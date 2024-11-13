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

using DAQFramework.Common.Configuration;

using Grumpy.DAQmxNetApi;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Grumpy.DAQmxDeviceServer.Configuration
{
    public class DAQTiming: ConfigurationBase, IEquatable<DAQTiming>
    {
        private string? _clockSource;
        private double _clockRate;
        private ActiveEdge _edge;
        private SamplingMode _samplingMode;
        private int _sampleasPerChannel;
        private string? _triggerSource;
        private string? _referenceTriggerSource;
        private ActiveEdge _triggerActiveEdge;
        private ActiveEdge _referenceTriggerActiveEdge;
        
        public DAQTiming():base() {

            _clockSource = string.Empty;
            _clockRate = 1000.0;
            _edge = ActiveEdge.Rising;
            _samplingMode = SamplingMode.ContineousSamples;
            _sampleasPerChannel = 1000;
            _triggerSource = null;
            _referenceTriggerSource = null;
            _triggerActiveEdge = ActiveEdge.Rising;
            _referenceTriggerActiveEdge = ActiveEdge.Rising;
        }

        public DAQTiming(string clockSource, 
                         double clockRate, 
                         ActiveEdge edge, 
                         SamplingMode samplingMode, 
                         int samplesPerChannel, 
                         string? triggerSource = null, 
                         string? referenceTriggerSource = null, 
                         ActiveEdge triggerActiveEdge = 
                                                ActiveEdge.Rising, 
                         ActiveEdge referenceTriggerActiveEdge = 
                                                ActiveEdge.Rising) : this() {

            _clockSource = clockSource;
            _clockRate = clockRate;
            _edge = edge;
            _samplingMode = samplingMode;
            _sampleasPerChannel = samplesPerChannel;
            _triggerSource = triggerSource;
            _referenceTriggerSource = referenceTriggerSource;
            _triggerActiveEdge = triggerActiveEdge;
            _referenceTriggerActiveEdge = referenceTriggerActiveEdge;
        }

        [JsonProperty]
        public string? ClockSource {
            get => _clockSource;
            set => _clockSource = value;
        }

        [JsonProperty]
        public double ClockRate {
            get => _clockRate;
            set => _clockRate = value;
        }

        [JsonProperty]
        public ActiveEdge Edge {
            get => _edge;
            set => _edge = value;
        }

        [JsonProperty]
        public SamplingMode SamplingMode {
            get => _samplingMode;
            set => _samplingMode = value;
        }

        [JsonProperty]
        public int SamplesPerChannel {
            get => _sampleasPerChannel; 
            set => _sampleasPerChannel = value;
        }

        [JsonProperty]
        public string? TriggerSource {
            get => _triggerSource;
            set => _triggerSource = value;
        }

        [JsonProperty]
        public string? ReferenceTriggerSource { 
            get => _referenceTriggerSource; 
            set => _referenceTriggerSource = value; 
        }

        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public ActiveEdge TriggerActiveEdge {
            get => _triggerActiveEdge;
            set => _triggerActiveEdge = value;
        }

        [JsonProperty]
        public ActiveEdge ReferenceTriggerActiveEdge {
            get => _referenceTriggerActiveEdge;
            set => _referenceTriggerActiveEdge = value;
        }

        public bool Equals(DAQTiming? other) =>
                    other is not null 
                && ((ClockSource is null && other.ClockSource is null) ||
                  String.Equals(ClockSource, other.ClockSource, 
                                StringComparison.OrdinalIgnoreCase))
                && Math.Abs(ClockRate - other.ClockRate) < Epsilon
                && Edge == other.Edge
                && SamplingMode == other.SamplingMode
                && SamplesPerChannel == other.SamplesPerChannel
                && ((TriggerSource is null && other.TriggerSource is null) ||
                    String.Equals(TriggerSource, other.TriggerSource,
                                StringComparison.OrdinalIgnoreCase))
                && ((ReferenceTriggerSource is null 
                     && other.ReferenceTriggerSource is null) ||
                     String.Equals(ReferenceTriggerSource,
                                other.ReferenceTriggerSource,
                                StringComparison.OrdinalIgnoreCase))
                && TriggerActiveEdge == other.TriggerActiveEdge
                && ReferenceTriggerActiveEdge == other.ReferenceTriggerActiveEdge;

        public override bool Equals(object? obj) =>
            obj is ChannelConfiguration other && Equals(other);

        public override int GetHashCode() => 
            ClockSource?.GetHashCode() ?? 0 ^
            ClockRate.GetHashCode() ^
            Edge.GetHashCode() ^
            SamplingMode.GetHashCode() ^
            SamplesPerChannel.GetHashCode() ^
            TriggerSource?.GetHashCode() ?? 0 ^
            ReferenceTriggerSource?.GetHashCode() ?? 0 ^
            TriggerActiveEdge.GetHashCode() ^
            ReferenceTriggerActiveEdge.GetHashCode();
    }
}
