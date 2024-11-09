using Grumpy.DaqFramework.Common;
using Grumpy.DAQFramework.Drivers;

using Microsoft.Extensions.Logging;


namespace Grumpy.StatePatternFramework
{

    public class ConfigurationAccessException : Exception
    {
        public ConfigurationAccessException(Exception? e = null) : base("Configuration access attempt failed.", e) { 
        }
    }

    public class ConfigurationUpdateException : Exception
    {
        public ConfigurationUpdateException(Exception? e = null) : base("Configuration update attempt failed.", e) {
        }
    }

    public class FsmDevice<TConfiguration, TCommandType, THwDriver, TModule>: Fsm 
        where TConfiguration : ConfigurationBase, new()
        where TModule : class
        where THwDriver : IGenericDeviceDriver
    {
        public const int ConfigurationLockTimeoutMs = 2000;
        private object _configurationLock;
        private  TConfiguration? _configuration;
        private THwDriver? _driver;

        public FsmDevice( string deviceName,
                          THwDriver? hwHandler,
                          TModule? module = null,
                          ILogger? logger = null,
                          bool logTransitions = false) : base(deviceName, logTransitions, logger)
        {
            _configuration = null;
            _configurationLock = new object();
            Module = module;
            _driver = hwHandler;
        }

        public virtual THwDriver? Driver {
            get => _driver;
            set => _driver = value;
        }

        public TModule? Module { get; private set; }

        // Interface...

        #region Interface

        public TConfiguration? Configuration {
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
                    cnfg.CopyFrom(value!);

                    Monitor.TryEnter(_configurationLock, 
                        ConfigurationLockTimeoutMs, ref lockAcuared);

                    if (lockAcuared) {

                        _configuration = cnfg;

                        if ( (_workerThread != null) && 
                             (_workerThread.ThreadState == ThreadState.WaitSleepJoin)) {

                                ResumeWorker();
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

                    if (lockAcuared) {
                        Monitor.Exit(_configurationLock);
                    }
                }
            }
        }

        public bool ConfigurationIsSet {

            get {
            
                bool lockAcuared = false;
                
                try {
                    
                    Monitor.TryEnter(_configurationLock, 
                        ConfigurationLockTimeoutMs, ref lockAcuared);
                    
                    if (lockAcuared) { 
                    
                        return _configuration != null;
                    }
                    else { 
                    
                        throw new Exception();
                    }
                }
                finally {
                
                    if (lockAcuared) { 
                    
                        Monitor.Exit(_configurationLock); 
                    }
                }
            }
        }

        virtual public bool SetConfiguration( TConfiguration configuration)
        {
            // Configuration must not be canged when device is operating.

            StateIDBase id = 
                (StateIDBase) (CurrentState?.ID ?? StateIDBase.NA);

            if ( (id == StateIDBase.Start) || (id == StateIDBase.Loaded)) {

                var cnfg = configuration;

                Configuration = configuration;

                // Setting configuration triggers transition to the next
                // (typically "Configured") state.
                // Resume FSM if idling.
                if (WorkerIsPaused) { 

                    ResumeWorker();
                }

                return true;
            }

            _logger?.LogError ($"{Name}. Can't set configuration in " +
                $"{CurrentState?.Name ?? "\"Unknown state\""} state.");
            return false;
        }

        public virtual bool IsOpen => Driver?.IsOpen ?? false;
        
        #endregion Interface
    }
}
