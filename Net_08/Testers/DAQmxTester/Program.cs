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


using Grumpy.Common.BaseObjects;
using Grumpy.DAQmxDeviceServer.Configuration;
using Grumpy.DAQmxNetApi;

namespace Grumpy.DAQmxTester
{
    internal class Program
    {
        static protected DAQmxTestHelper _helper = new DAQmxTestHelper();
        static ChannelConfiguration ConfigureNewAiChannel() {

            ChannelConfiguration channel = new ChannelConfiguration();

            channel.SetType(IOTypes.AnalogInput);
            channel.Alias = "MyInput";
            channel.PhysicalChannel = $"{_helper.Config.DeviceName}{_helper.Config.SingleAiChannel}";
            channel.OperationModes = [ IOModes.OnDemand ,
                                       IOModes.FiniteSamples ,
                                       IOModes.ContinuousSamples ];
            channel.Range = new AIORange(-10.0, 10.0);
            channel.AITermination = AiTermination.RSE;

            return channel;
        }

        static ChannelConfiguration? DeserializeAiChannel(string source) {

            if (ConfigurationExtensions.DeserializeFromString(

                source, out ChannelConfiguration? channel2, out string error)) {

                Console.WriteLine($"Deserialized object: " +
                $"ChannelConfigurationBase");

                string serialized = channel2?.SerializeToString(out error)
                    ?? "SERIALISATION FAILED.";

                Console.WriteLine($"Deserialized object as string: " +
                    $"\n {serialized}");

                return channel2;
            }
            else {

                Console.WriteLine($"Failed to deserialize object. " +
                    $"Error: \n{error}");
                return null;
            }
        }


        static string? Serialize<T>(T o) where T : ConfigurationBase {

            var serialized = o.SerializeToString(out string? error);

            if (serialized != null) {
                Console.WriteLine($"Serialized object to " +
                        $"string: \n {serialized}");
            }
            else {
                Console.WriteLine($"Failed to serialize " +
                    $"object. Error: \n{error}");
            }

            return serialized;
        }

        static void Test1() {

            Console.WriteLine("\n----------- Test 1 -----------\n");
            Console.WriteLine("Hello, DAQmx!\n\n");

            ChannelConfiguration channel = ConfigureNewAiChannel();

            Console.WriteLine($"channel " +
                $"{(ConfigurationExtensions.IsJsonObject(channel) ? "is" : "isn't")}" +
                $" a Json object");

            string? serialized = Serialize(channel);

            DeserializeAiChannel(TestData.ChannelConfig);


            ChannelConfiguration? channel3 =
                                        channel.Clone(out string? err);

            if (channel3 is null) {
                Console.WriteLine($"Failed to clone object. Error: \n{err}");
            }
            else {

                string? serialized3 = channel3.SerializeToString(out string? error3);
                if (serialized3 != null) {
                    Console.WriteLine($"Cloned object as string: " +
                        $"\n{serialized3}");
                }
                else {
                    Console.WriteLine($"Failed to serialize object. Error: " +
                        $"\n{error3}");
                }

            }

            ChannelConfiguration channel4 = new ChannelConfiguration();
            string? error4 = string.Empty;

            if (channel4?.CopyFrom(channel3!, out error4) ?? false) {

                Console.WriteLine($"Copied object from channel3 to channel4.");

                string? serialized4 = channel4.SerializeToString(out error4);
                if (serialized4 != null) {
                    Console.WriteLine($"Copied object as string: " +
                            $"\n{serialized4}");
                }
                else {
                    Console.WriteLine($"Failed to serialize object. Error: " +
                            $"\n{error4}");
                }

            }
            else {
                Console.WriteLine($"Failed to copy object. Error: " +
                            $"\n{error4!}");
            }
        }

        static void Test2(out List<ChannelConfiguration> channels) {

            channels = new List<ChannelConfiguration>();

            channels.AddRange(TestData.CreateChannels(
                                IOTypes.AnalogInput,
                                0, 6,
                                _helper.Config.DeviceName,
                                "AnalogInput",
                                [IOModes.OnDemand, IOModes.FiniteSamples],
                                new AIORange(-10.0, 10.0),
                                AiTermination.RSE));

            channels.AddRange(TestData.CreateChannels(
                                IOTypes.AnalogOutput,
                                0, 2,
                                _helper.Config.DeviceName,
                                "AnalogOutput",
                                [IOModes.OnDemand, IOModes.FiniteSamples],
                                new AIORange(-10.0, 10.0)));
        }

        static void Main(string[] args) {

            DAQmxAOTestClass testClass = new DAQmxAOTestClass();

            Console.WriteLine("Running Test1AOSingleSample...");
            testClass.AOTestSingleSample();

            Console.WriteLine("Running Test2AOFiniteSamples...");
            testClass.AOTestFiniteSamples();

            Console.WriteLine("All AO tests completed.");


            var testAiClass = new DAQmxAITestClass();

            testAiClass.AITestSingleSamplesSoftwareTrigger();
            testAiClass.AITestFiniteSamplesSoftwareTrigger();
            testAiClass.AITestFiniteSamplesSoftwareTriggerWEvent();

            Console.WriteLine("All AI tests completed.");

            var TestDITestClass = new DAQmxDITestClass();

            TestDITestClass.DiTestsLines();
            TestDITestClass.DiTestReadU32();
            TestDITestClass.DiTestReadU16();


            var doTest = new DAQmxDOTestClass();
            doTest.DoTestLines();
            doTest.DoTestWriteU32();
            doTest.DoTestWriteScalar();

            Console.WriteLine("All Do tests completed.");


            var counterTest = new CounterTest();

            counterTest.TestCreateCOPulseChanTime();

            // Test1();

            Test2(out List<ChannelConfiguration> channels);

            var serverConfig = new DAQmxDeviceServerConfiguration();

            serverConfig.Channels = channels;
            Console.WriteLine($"\n~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~\n");

            string? serialized = serverConfig.SerializeToString(out string? error);
            if (serialized != null) {
                Console.WriteLine($"Serialized object to string: " +
                    $"\n {serialized}");
            }
            else {
                Console.WriteLine($"Failed to serialize object. Error: " +
                    $"\n{error}");
            }


            var channels2 = serverConfig.GetChannels(IOTypes.AnalogOutput, IOModes.FiniteSamples);

            var serverConfig2 = new DAQmxDeviceServerConfiguration() {
                Channels = channels2!
            };

            Console.WriteLine($"\n~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~\n");

            string? serialized2 = serverConfig2.SerializeToString(out string? error2);
            if (serialized2 != null) {
                Console.WriteLine($"Serialized object to string: " +
                    $"\n {serialized2}");
            }
            else {
                Console.WriteLine($"Failed to serialize object. Error: " +
                    $"\n{error2}");
            }
        }

    }
}
