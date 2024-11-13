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

using Grumpy.DAQFramework.Common;
using Newtonsoft.Json;

namespace DAQFramework.Common.Configuration
{

    [JsonObject(MemberSerialization.OptIn)]
    public class ConfigurationBase : ObservableObject, IConfigurationBase
    {
        protected const double Epsilon = 1.0e-6;

        private string? _fileName;
        public ConfigurationBase()
        {

            _fileName = null;
        }

        [JsonProperty]
        public string FileName
        {
            get => (string)(_fileName?.Clone() ?? string.Empty);
            set => _fileName = (string)(_fileName?.Clone() ?? null!);
        }

        public bool PopulateFromString(string jsonString,
                                       out string? error)
        {

            return ConfigurationExtensions.PopulateFromString(this,
                                            jsonString, out error);
        }

        public bool PopulateFromFile(string filePath,
                                     out string? error)
        {

            return ConfigurationExtensions.PopulateFromFile(
                this, filePath, out error);
        }


        public bool SerializeToString(out string? serialized,
                                      out string? error)
        {

            serialized =
                ConfigurationExtensions.SerializeToString(this,
                                                           out error);

            return !string.IsNullOrEmpty(serialized);
        }

        public bool SerializeToFile(string filePath,
                                    out string? error)
        {

            return ConfigurationExtensions.SerializeToFile(this,
                                                filePath, out error);
        }

        public bool PopulateFromString<T>(string jsonString,
            out string? error) where T : ConfigurationBase
        {

            return PopulateFromString(jsonString, out error);
        }

        public bool PopulateFromFile<T>(string filePath,
            out string? error) where T : ConfigurationBase
        {

            return PopulateFromFile(filePath, out error);
        }

        public static bool DeserializeFromString<T>(string source,
            out T? o, out string error) where T : ConfigurationBase
        {

            return ConfigurationExtensions.DeserializeFromString(
                source, out o, out error);
        }

        public static bool DeserializeFromFile<T>(string source,
            out T? o, out string error) where T : ConfigurationBase
        {

            return ConfigurationExtensions.DeserializeFromFile(
                source, out o, out error);
        }

  

        public string LastError
        {
            get;
            protected set;
        } = string.Empty;

    }
}
