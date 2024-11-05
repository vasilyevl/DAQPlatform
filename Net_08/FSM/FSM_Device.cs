using System;
using System.Threading;


using Tau.Common.Configuration;
using Tau.HwControlBase;
using Tau.HwControlBase.Interfaces;

namespace FSM
{ 
    public class FsmDevice<TConfiguration, TCommandType, THwHandler, TModule> : 
                    Fsm<TCommandType> where TConfiguration : ConfigurationItemBase, new()
                                      where TModule : class
                                      where THwHandler : IDeviceHandlerBase
    {
        public const int ConfigurationLockTimeoutMs = 2000;
        private object _configurationLock;
        private  TConfiguration _configuration;
        private THwHandler _handler;

        public FsmDevice( string deviceName,
                          THwHandler hwHandler,
                          TModule module = null,
                          bool logTransitions = false) : base(deviceName)
        {
            _configuration = null;
            _configurationLock = new object();
            Module = module;
            _handler = hwHandler;
        }

        public virtual THwHandler Handler {
            get => _handler;
            set => _handler = value;
        }

        public TModule Module { get; private set; }




        // Interface...

        #region Interface




        public TConfiguration Configuration {
            get {

                bool lockAcuared = false;

                try {
                    Monitor.TryEnter(_configurationLock, 
                        ConfigurationLockTimeoutMs, ref lockAcuared);

                    if (lockAcuared) {

                        if (_configuration != null) {

                            TConfiguration c =  new TConfiguration();
                            c.CopyFrom(this._configuration);
                            return c;
                        }
                        else {
                            return null;
                        }
                    }
                    else {
                        throw new ConfigurationAccessException();
                    }
                }
                finally {
                    if (lockAcuared)
                        Monitor.Exit(_configurationLock);
                }
            }

            private set {
                bool lockAcuared = false;
                try {
                    TConfiguration cnfg = new TConfiguration();
                    cnfg.CopyFrom(value);

                    Monitor.TryEnter(_configurationLock, 
                        ConfigurationLockTimeoutMs, ref lockAcuared);

                    if (lockAcuared) {

                        _configuration = cnfg;

                        if ( (_fsmWorkerThread != null) && 
                             (_fsmWorkerThread.ThreadState == ThreadState.WaitSleepJoin)) {

                                ResumeFsmThread();
                        }
                    }
                    else {
                        throw new ConfigurationAccessException();
                    }
                }
                catch (Exception e) {
                    throw new ConfigurationUpdateException(e);
                }
                finally {
                    if (lockAcuared)
                        Monitor.Exit(_configurationLock);
                }
            }
        }

        public bool ConfigurationIsSet {
            get {
                bool lockAcuared = false;
                try {
                    Monitor.TryEnter(_configurationLock, 
                        ConfigurationLockTimeoutMs, ref lockAcuared);
                    
                    if (lockAcuared) { return _configuration != null;}
                    else { throw new ConfigurationAccessException();}
                }
                finally {
                    if (lockAcuared) { Monitor.Exit(_configurationLock); }
                }
            }
        }

        virtual public SetValueResult SetConfiguration(
            TConfiguration configuration)
        {
            // Configuration must not be canged when device is operating. 
            if ( (CurrentState.Name == "Start") || 
                 (CurrentState.Name == "Loaded")) {

                var cnfg = configuration;

                Configuration = configuration;

                // Setting configuration triggers transition to the next
                // (typically "Configured") state.
                // Resume FSM if idling.
                if (FsmIsPaused) { ResumeFsmThread();}

                return SetValueResult.Success;
            }

            _logger.Warning($"{Name}. Can't set configuration in " +
                $"{CurrentState.Name} state. ");
            return SetValueResult.IncompatibleState;
        }

        public virtual bool IsOpen => Handler?.IsOpen ?? false;
        
        #endregion Interface
    }
}
