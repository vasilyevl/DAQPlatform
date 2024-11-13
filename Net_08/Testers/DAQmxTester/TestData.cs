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

using Grumpy.DAQmxDeviceServer.Configuration;
using Grumpy.DAQmxNetApi;

namespace Grumpy.DAQmxTester
{
    internal static class TestData
    {

        internal static string ChannelConfig = "{" +
        "\r\n  \"Type\": \"AnalogInput\"," +
        "\r\n  \"Alias\": \"MyInput2\"," +
        "\r\n  \"PhysicalChannel\": \"Dev1/ai1\"," +
        "\r\n  \"OperationModes\": " +
            "[" +
                "\r\n    \"OnDemand\"," +
                "\r\n    \"FiniteSamples\"," +
           //       "\r\n    \"ContinuousSamples\"" +
           "\r\n  ]," +
        "\r\n  \"Range\": " +
            "{" +
                "\r\n    \"Min\": -0.05," +
                "\r\n    \"Max\": 5.05" +
             "\r\n  }," +
        "\r\n  \"AITermination\": \"NRSE\"" +
        "\r\n}";


        internal static List<ChannelConfiguration> CreateChannels( IOTypes ioType, 
            int startingChannel, int numberOfChannels, 
            string device,  
            string aliasBase,
            IOModes[] operationModes,
            AIORange? range = null,
            AiTermination aiTermination = AiTermination.Default,
            int port = 0,
            DODrive driveType = DODrive.Any
            ) {

            List<ChannelConfiguration> channels = new List<ChannelConfiguration>();

            for (int i = startingChannel; i < startingChannel + numberOfChannels; i++) {

                ChannelConfiguration channel = new ChannelConfiguration();

                channel.SetType(ioType);
                channel.Alias = aliasBase + i.ToString();
                
                channel.OperationModes = operationModes;
                channel.Range = new AIORange(-10.0, 10.0);
                channel.AITermination = AiTermination.RSE;

                string chnl = string.Empty;
                switch (ioType) {
                    case (IOTypes.AnalogInput):
                        chnl = "ai";
                        break;
                    case (IOTypes.AnalogOutput):
                        chnl = "ao";
                        break;
                    case (IOTypes.DigitalInput):
                    case (IOTypes.DigitalOutput):
                        chnl = "port" + port.ToString() + "/line";
                        break;
                    default:
                        throw new ArgumentException("Invalid IOTypes value.");
                }
                chnl += i.ToString();
                channel.PhysicalChannel = device + "/" + chnl;
                channels.Add(channel);
            }

            return channels;

        }
    }
}
