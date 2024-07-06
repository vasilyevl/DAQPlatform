namespace Grumpy.HWControl.Common
{
    public interface ISwitchState : IEquatable<Object?>, IEquatable<SwitchSt>
    {
        public SwitchSt State { get; set; }
        public String ToString();
    }
}