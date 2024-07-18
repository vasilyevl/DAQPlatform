
namespace Grumpy.HWControl.IO
{
    public interface ISwitchState : IEquatable<Object?>, IEquatable<SwitchSt>
    {
        public SwitchSt State { get; set; }
        public String ToString();
    }
}