namespace GSE.ClickPLCHandler
{
    public delegate bool Write<TValue>(TValue value);
    public delegate bool Read<TValue>(out TValue value);

    public class ControlBase
    {
        public ControlBase(string name, IOType type, int len = 1, string endName = null)
        {
            _name = name;
            _type = type;
            _len = len;
            _endName = endName;
        }

        private string _name;
        public string Name => (string)_name?.Clone() ?? null;

        private string _endName;
        public string EndName => (string)_name?.Clone() ?? null;

        private int _len;
        public int Length => _len;

        private IOType _type;
        public IOType Type => _type;

    }


    public class ReadOnlyControlBase<TRead> : ControlBase
    {
        public ReadOnlyControlBase(string name, Read<TRead> rd, IOType type, int len = 1, string endName = null) :
        base(name, type, len, endName)
        {
            Get = rd;
        }

        public Read<TRead> Get { get; private set; }
    }


    public class WriteOnlyControlBase<TWrite> : ControlBase
    {
        public WriteOnlyControlBase(string name, Write<TWrite> rd, IOType type, int len = 1, string endName = null) :
        base(name, type, len, endName)
        {
            Set = rd;
        }

        public Write<TWrite> Set { get; protected set; }
    }


    public class ReadWriteControlBase<TWrite, TRead> : ControlBase
    {
        public ReadWriteControlBase(string name, Write<TWrite> wrt, Read<TRead> rd, IOType type, int len = 1, string endName = null) :
            base(name, type, len, endName)
        {
            Set = wrt;
            Get = rd;
        }

        public Write<TWrite> Set { get; protected set; }
        public Read<TRead> Get { get; protected set; }
    }


}
