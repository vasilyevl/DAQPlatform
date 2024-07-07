namespace Grumpy.ClickPLC
{
    public delegate bool Write<TValue>(TValue value);
    public delegate bool Read<TValue>(out TValue value);

    public class ControlBase
    {

        private string? _name;
        private string? _endName;
        private int _len;
        private IOType _type;


        public ControlBase(string name, IOType type, int len = 1, string? endName = null) {
            _name = name;
            _type = type;
            _len = len;
            _endName = endName;
        }


        public string Name => (string)_name?.Clone()! ?? string.Empty;



        public string EndName => (string)_endName?.Clone()! ?? string.Empty;


        public int Length => _len;


        public IOType Type => _type;

    }


    public class ReadOnlyControlBase<TRead> : ControlBase
    {
        public ReadOnlyControlBase(string name, Read<TRead> rd, IOType type, int len = 1, string? endName = null) :
        base(name, type, len, endName) {
            Get = rd;
        }

        public Read<TRead> Get { get; private set; }
    }


    public class WriteOnlyControlBase<TWrite> : ControlBase
    {
        public WriteOnlyControlBase(string name, Write<TWrite> rd, IOType type, int len = 1, string? endName = null) :
        base(name, type, len, endName) {
            Set = rd;
        }

        public Write<TWrite> Set { get; protected set; }
    }


    public class ReadWriteControlBase<TWrite, TRead> : ControlBase
    {
        public ReadWriteControlBase(string name, Write<TWrite> wrt, Read<TRead> rd, IOType type, int len = 1, string? endName = null) :
            base(name, type, len, endName) {
            Set = wrt;
            Get = rd;
        }

        public Write<TWrite> Set { get; protected set; }
        public Read<TRead> Get { get; protected set; }
    }


}
