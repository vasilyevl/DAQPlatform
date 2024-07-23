using Grumpy.DAQmxCLIWrap;
using Xunit.Abstractions;


namespace Grumpy.DAQmxWrapUnitTest
{
    public class UnitTest1
    {

        private readonly ITestOutputHelper _testOutputHelper;

        [Fact]
        public void Test1() {
            _testOutputHelper.WriteLine("Test1 Started.");
            long handle = 0;


            Int32 result = DAQmxCLIWrapper.CreateTask("myTask", out handle);

            String err = DAQmxCLIWrapper.GetErrorDescription(result);

            Assert.True(DAQmxCLIWrapper.Success(result), err);
            
            _testOutputHelper.WriteLine("Test1 Complete.");



            
        }
    }
}