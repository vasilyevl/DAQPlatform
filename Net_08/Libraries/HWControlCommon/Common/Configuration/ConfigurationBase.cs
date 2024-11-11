/* 
Copyright (c) 2024 vasilyevl (Grumpy). Permission is hereby granted, 
free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"),to deal in the Software 
without restriction, including without limitation the rights to use, copy, 
modify, merge, publish, distribute, sublicense, and/or sell copies of the 
Software, and to permit persons to whom the Software is furnished to do so, 
subject to the following conditions:

The above copyright notice and this permission notice shall be included 
in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, 
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,FITNESS FOR A 
PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION 
OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE 
OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using DAQFramework.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Grumpy.DAQFramework.Common
{

    public interface IConfigurationBase : ICloneable
    {
        bool CopyFrom(object src);
        bool Init(string configuration);
        bool LoadFromFile(string configuration);
        string? ToString();
        bool SaveToFile(string filePath);
        string LastErrorComment { get; }
    }

    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public abstract class ConfigurationBase : ObservableObject, IConfigurationBase
    {
        private string _fileName;

        public ConfigurationBase() : base()
        {
            _fileName = null!;
            Reset();
        }

        public abstract bool CopyFrom(object? src);

        public virtual object Clone()
        {
            return MemberwiseClone();
        }

        public bool SaveToFile(string? fileName = null)
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

            if (FileUtilities.FileExists(fileName, out string file, out string dir)
                && !FileUtilities.DeleteFile(fileName)) {

                LastErrorComment = $"Failed to delete file " +
                    $"\"{fileName}\". {FileUtilities.LastError}";
                return false;
            }

            if (FileUtilities.SaveTextFile(ToString()!, null!, fileName)) {

                LastErrorComment = $"Failed to save to the " +
                    $"\"{fileName}\" file: {FileUtilities.LastError}";
            }

            return true;
        }

        public bool LoadFromFile(string filePathName)
        {
            if (FileUtilities.ReadTextFile( null!, filePathName, 
                                            out string text)) {

                return Init(text);
            }
            else {

                LastErrorComment = (string)FileUtilities.LastError.Clone();
                return false;
            }
        }

        public bool Init(string configuration)
        {
            try
            {
                var jt = JToken.Parse(configuration);
                object src = jt.ToObject(GetType())!;
                return CopyFrom(src);
            }
            catch (Exception ex)
            {
                LastErrorComment = ex.Message;
                return false;
            }
        }

        private string? _lastErrorComment = null;
        private object _lastErrorLock = new object();

        [JsonIgnore]
        public string LastErrorComment
        {
            get {
                lock (_lastErrorLock) {

                    return (string)(_lastErrorComment?.Clone() ?? 
                                    string.Empty);
                }
            }

            protected set {
                lock (_lastErrorLock) {

                    _lastErrorComment = value;
                }
            }
        }

        [JsonIgnore]
        public int LastErrorCode
        {
            get; protected set;
        }

        public string GetSummary()
        {
            if (string.IsNullOrEmpty(FileName)) {

                return ToString()!;
            }
            else {

                return "{\n\t\"FileName\": \"" + FileName + "\"\n}";
            }
        }

        [JsonProperty]
        public string FileName
        {
            get => (string)_fileName.Clone();
            set => _fileName = (string)(_fileName?.Clone() ?? null!);
        }
        public bool ShouldSerializeFileName() => false;

        public abstract void Reset();
    }
}
