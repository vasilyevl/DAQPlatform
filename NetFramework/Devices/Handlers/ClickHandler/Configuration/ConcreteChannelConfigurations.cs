﻿using PissedEngineer.HWControl;

using Newtonsoft.Json;

namespace PissedEngineer.ClickPLCHandler
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class InputConfiguration : ChannelConfigurationBase<bool>
    {
        public InputConfiguration() : base() { }

        public override bool IsValid()
        {
            return base.IsValid() && IOType == IOType.Input;
        }
        public override bool IsReadOnly() => true;
    }

    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class OutputConfiguration : ChannelConfigurationBase<SwitchSt>
    {
        public OutputConfiguration() : base() { }

        public override bool IsValid()
        {
            return base.IsValid() && IOType == IOType.Output;
        }
    }

    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class ControlRelayConfiguration : ChannelConfigurationBase<SwitchSt>
    {
        public ControlRelayConfiguration() : base() { }

        public override bool IsValid()
        {
            return base.IsValid() && IOType == IOType.ControlRelay;
        }
    }

}
