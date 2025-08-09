using Grumpy.SDAQFramework.Common;
using Grumpy.SDAQFramework.Utilities;

namespace Grumpy.SDAQFramework.Configuration
{
    /// <summary>
    /// Defines the contract for configuration objects, including cloning, initialization, file operations, and error reporting.
    /// </summary>
    public interface IConfigurationBase : ICloneable
    {
        /// <summary>
        /// Copies configuration data from another object.
        /// </summary>
        /// <param name="src">The source object to copy from.</param>
        /// <returns>True if the copy was successful; otherwise, false.</returns>
        bool CopyFrom(object src);

        /// <summary>
        /// Initializes the configuration from a string (typically JSON).
        /// </summary>
        /// <param name="configuration">The configuration string.</param>
        /// <returns>True if initialization was successful; otherwise, false.</returns>
        bool Init(string configuration);

        /// <summary>
        /// Loads the configuration from a file.
        /// </summary>
        /// <param name="configuration">The file path to load from.</param>
        /// <returns>True if loading was successful; otherwise, false.</returns>
        bool LoadFromFile(string configuration);

        /// <summary>
        /// Returns a string representation of the configuration.
        /// </summary>
        /// <returns>A string representation of the configuration.</returns>
        string? ToString();

        /// <summary>
        /// Saves the configuration to a file.
        /// </summary>
        /// <param name="filePath">The file path to save to.</param>
        /// <returns>True if saving was successful; otherwise, false.</returns>
        bool SaveToFile(string filePath);

        /// <summary>
        /// Gets the last error message encountered by the configuration.
        /// </summary>
        string LastErrorComment { get; }
    }

    /// <summary>
    /// Provides a base implementation for configuration objects, including file operations, error handling, and cloning.
    /// </summary>
    public abstract class ConfigurationBase : ObservableDisposableBase, IConfigurationBase
    {
        private string _fileName;
        private string? _lastErrorComment = null;
        private object _lastErrorLock = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationBase"/> class.
        /// </summary>
        public ConfigurationBase() : base()
        {
            _fileName = null!;
            _lastErrorComment = null;
            _lastErrorLock = new object();
            Reset();
        }

        /// <summary>
        /// Copies configuration data from another object.
        /// </summary>
        /// <param name="src">The source object to copy from.</param>
        /// <returns>True if the copy was successful; otherwise, false.</returns>
        public abstract bool CopyFrom(object src);

        /// <summary>
        /// Resets the configuration to its default state.
        /// </summary>
        public abstract void Reset();

        /// <summary>
        /// Creates a shallow copy of the current configuration object.
        /// </summary>
        /// <returns>A shallow copy of the configuration object.</returns>
        public virtual object Clone()
        {
            return MemberwiseClone();
        }

        /// <summary>
        /// Saves the configuration to a file.
        /// </summary>
        /// <param name="fileName">The file path to save to. If null, uses the current <see cref="FileName"/>.</param>
        /// <returns>True if saving was successful; otherwise, false.</returns>
        public bool SaveToFile(string? fileName = null)
        {
            if (fileName == null) {
                fileName = FileName;
            }
            else {
                FileName = fileName;
            }

            if (string.IsNullOrEmpty(fileName)) {
                LastErrorComment = "Can't save into a file. Name not provided.";
                return false;
            }

            if (FileUtilities.FileExists(fileName, out string fn, out string dir)
                && !FileUtilities.DeleteFile(fileName)) {
                LastErrorComment = $"Failed to delete file \"{fileName}\". {FileUtilities.LastError}";
                return false;
            }

            if (FileUtilities.SaveTextFile(ToString()!, null!, fileName)) {
                LastErrorComment = $"Failed to save to the \"{fileName}\" file: {FileUtilities.LastError}";
            }

            return true;
        }

        /// <summary>
        /// Loads the configuration from a file.
        /// </summary>
        /// <param name="filePathName">The file path to load from.</param>
        /// <returns>True if loading was successful; otherwise, false.</returns>
        public bool LoadFromFile(string filePathName)
        {
            if (FileUtilities.ReadTextFile(null!, filePathName, out string? text)) {
                if (string.IsNullOrEmpty(text) || string.IsNullOrWhiteSpace(text)) {
                    LastErrorComment = "File is empty or contains only white spaces.";
                    return false;
                }

                return Init(text);
            }
            else {
                LastErrorComment = (string) FileUtilities.LastError.Clone();
                return false;
            }
        }

        /// <summary>
        /// Initializes the configuration from a string (typically JSON).
        /// </summary>
        /// <param name="configuration">The configuration string.</param>
        /// <returns>True if initialization was successful; otherwise, false.</returns>
        public virtual bool Init(string configuration)
        {
            try {
                object src = JsonSerializer.Deserialize<ConfigurationBase>(configuration)!;
                return CopyFrom(src);
            }
            catch (Exception ex) {
                LastErrorComment = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Gets the last error message encountered by the configuration.
        /// </summary>
        [JsonIgnore]
        public string LastErrorComment
        {
            get {
                lock (_lastErrorLock) {
                    return (string) (_lastErrorComment?.Clone() ?? string.Empty);
                }
            }
            protected set {
                lock (_lastErrorLock) {
                    _lastErrorComment = value;
                }
            }
        }

        /// <summary>
        /// Gets the last error code encountered by the configuration.
        /// </summary>
        [JsonIgnore]
        public int LastErrorCode
        {
            get; protected set;
        }

        /// <summary>
        /// Returns a summary of the configuration, including the file name if available.
        /// </summary>
        /// <returns>A summary string of the configuration.</returns>
        public string GetSummary()
        {
            if (string.IsNullOrEmpty(FileName)) {
                return ToString()!;
            }
            else {
                return "{\n\t\"FileName\": \"" + FileName + "\"\n}";
            }
        }

        /// <summary>
        /// Gets or sets the file name associated with this configuration.
        /// </summary>
        public string FileName
        {
            get => (string) _fileName.Clone();
            set => _fileName = (string) (_fileName?.Clone() ?? null!);
        }

        /// <summary>
        /// Determines whether the <see cref="FileName"/> property should be serialized.
        /// </summary>
        /// <returns>False, as the file name is not intended for serialization.</returns>
        public bool ShouldSerializeFileName() => false;
    }
}