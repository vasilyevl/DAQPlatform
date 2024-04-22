using LV.Common;
using LV.Common.Utilities;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using System;


namespace LV.Common
{

    public interface IConfigurationBase: ICloneable
    {
        bool CopyFrom(object src);
        bool Init(string configuration);
        bool LoadFromFile(string configuration);
        string ToString();
        bool SaveToFile(string filePath);
        string LastErrorComment { get; }
    }

    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public abstract class ConfigurationBase : ObservableObject,  IConfigurationBase
    {
        private string _fileName;

        public ConfigurationBase( ) : base() {
            _fileName = null;
        }

        public abstract bool CopyFrom(object src);

        public virtual object Clone()
        {
            return base.MemberwiseClone();
        }

        public bool SaveToFile(string fileName = null)
        {
            if (fileName == null) {
                fileName = FileName;
            }
            else {
                FileName = fileName;
            }

            if (string.IsNullOrEmpty(fileName)) {
                LastErrorComment = "Can't save into a file. " +
                    "Name not provided.";
                return false;
            }

            if ( FileUtilities.FileExists(fileName) 
                && !FileUtilities.DeleteFile(fileName)) {

                LastErrorComment = $"Failed to delete file " +
                    $"\"{fileName}\". {FileUtilities.LastError}";
                return false;
            }

            if (FileUtilities.SaveTextFile(this.ToString(), null, fileName)) {
                LastErrorComment = $"Failes to save to the " +
                    $"\"{fileName}\" file: {FileUtilities.LastError}";
            }

            return true;
        }

        public bool LoadFromFile(string filePathName)
        {
            if (FileUtilities.ReadTextFile(null, filePathName, out string text)) {

                return Init(text);
            }
            else {
                LastErrorComment = (string)FileUtilities.LastError.Clone();
                return false;
            }
        }

        public bool Init(string configuration)
        {
            try {
                var jt = JToken.Parse(configuration);
                object src = jt.ToObject(this.GetType());
                return CopyFrom(src);
            }
            catch (Exception ex) {
                LastErrorComment = ex.Message;
                return false;
            }
        }

        private string _lastErrorComment = null;
        private object _lastErrorLock = new object();

        [JsonIgnore]
        public string LastErrorComment {
            get {
                lock (_lastErrorLock) {
                    return (string)(_lastErrorComment?.Clone() ?? null);
                }
            }

            protected set {
                lock (_lastErrorLock) {
                    _lastErrorComment = value;
                }
            }
        }

        [JsonIgnore]
        public  int LastErrorCode {
            get; protected set;
        }

        public string GetSammary(){ 
 
            if (string.IsNullOrEmpty(FileName)) {                
                return this.ToString();
            }
            else {
                return "{\n\t\"FileName\": \"" + FileName + "\"\n}";
            }
        }



        [JsonProperty]
        public string FileName { 
            get => (string) _fileName.Clone(); 
            set => _fileName = (string)(_fileName?.Clone() ?? null); 
        }
        public bool ShouldSerializeFileName() =>  false;
    }
}
