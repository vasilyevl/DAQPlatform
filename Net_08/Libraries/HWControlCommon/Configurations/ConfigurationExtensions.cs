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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grumpy.DAQFramework.Configuration
{
    public static class ConfigurationExtensions
    {
     
        public static bool DeserializeFromString<T>(string source, out T? o, 
            out string error)
            where T : ConfigurationBase {

            error = String.Empty;
            o = null;

            if (!IsJsonObjectType(typeof(T))) {

                error = $"DeserialiseFromString. Target object must have \"JsonObject\" attribute.";
                return false;
            }


            try {

                JToken deserialized = JToken.Parse(source);
                if (deserialized is not null) {

                    o = deserialized.ToObject<T>();

                    error = (o != null) ? String.Empty :
                        $"Failed to deserialize object of type {typeof(T).FullName} from string.";
                }
            }
            catch (Exception ex) {

                error = ex.Message;
            }


            return String.IsNullOrEmpty(error);
        } 

        public static bool DeserializeFromFile<T>(string source, out T? o,
                out string error, List<string> paths = null!) where T : ConfigurationBase {

            o = null;

            if (!IsJsonObjectType(typeof(T))) {

                error = $"DeserialiseFromFile. Target object must have \"JsonObject\" attribute.";
                return false;
            }
           

            return LoadJsonFile(name: source, out string? text, out error) ?
                DeserializeFromString(text!, out o, out error) : false;

        }

        public static bool LoadJsonFile(string name, out string? jsonText, 
            out string error)
        {

            error = String.Empty;
            jsonText = null;

            if (FileUtilities.FileExists(name: name,
                out string fileName, out string directory,
                paths: null!)) {

                if (!FileUtilities.ReadTextFile(directory: directory, 
                    fileName: fileName,
                    out jsonText, filter: null)) {
                    error = FileUtilities.LastError;
                }
            }
            else {

                error = $"File {name} does not exist.";
            }
            return string.IsNullOrEmpty(error) ;
        }

        public static bool PopulateFromString<T>( this T obj, string text,
            out string error)
            where T : ConfigurationBase {

            if (!IsJsonObjectType(typeof(T))) {

                error = $"PopulateFromString. Target object must have \"JsonObject\" attribute.";
                return false;
            }

            error = String.Empty;   

            if (!string.IsNullOrEmpty(text)) {
                try {
                    JsonConvert.PopulateObject(text, obj);

                    return true;
                }
                catch (Exception ex) {

                    error = $" Object update from string faled. Exception {ex.Message}";
                    return false;
                }
            }
            else {
                error = $"No or empty JSON string provided.";
                return false;
            }
        }

        public static bool PopulateFromFile<T>(this T obj, string source, 
            out string error) where T : ConfigurationBase {

            if (!IsJsonObjectType(typeof(T))) {

                error = $"PopulateFromFile. Target object must have \"JsonObject\" attribute.";
                return false;
            }

            return LoadJsonFile(source, out string? text, out error) ?
                PopulateFromString(obj, text!, out error) : false;
        }

        public static string SerializeToString<T>(this T obj, out string error, 
            bool indented = true)
            where T : ConfigurationBase {

            error = String.Empty;

            if (!IsJsonObjectType(typeof(T))) {

                error = $"SerializeToString. Target object must have \"JsonObject\" attribute.";
                return string.Empty ;
            }
            
            try {
                return JsonConvert.SerializeObject(obj, indented? Formatting.Indented : Formatting.None);
            }
            catch (Exception ex) {

                error = $"Object serializationFailed. Exception: {ex.Message}";
                return String.Empty;
            }
        }

        public static bool SerializeToFile<T>(this T obj, string filePathName, 
            out string error, bool indented = true)
            where T : ConfigurationBase {

            if (!IsJsonObjectType(typeof(T))) {

                error = $"SerializeToFile. Target object must have \"JsonObject\" attribute.";
                return false;
            }

            string text = SerializeToString(obj, out error, indented);

            if (!string.IsNullOrEmpty(text)) {

                if (FileUtilities.SaveTextFile(text: text,
                    directory: Path.GetFullPath(filePathName),
                    fileName: Path.GetFileName(filePathName))) {

                    return true;
                }
                else {

                    error = FileUtilities.LastError;
                }
            }
            return false;
        }

        public static bool CopyFrom<T>(this T target, T source, 
            out string error) where T: ConfigurationBase {

            if (!IsJsonObjectType(typeof(T))) {

                error = $"CopyFrom. Target and source objects must have \"JsonObject\" attribute.";
                return false;
            }


            if (source is null) {

                error = "Source object is null.";
                return false;
            }

            try {

                string serialized = source.SerializeToString(out error);
                if(!string.IsNullOrEmpty(serialized)) {

                    return target.PopulateFromString(serialized, out error);
                }
            }
            catch (Exception ex) {

                error = $"CopyFrom. Attempt to copy JSON properties failed. " +
                    $"Exception: {ex.Message}";
            }

            return false;
        }

        public static T? Clone<T>(this T source, out string error)
            where T : ConfigurationBase {

            if (!IsJsonObjectType(typeof(T))) {

                error = $"Clone. Target must have \"JsonObject\" attribute.";
                return null;
            }

            error = String.Empty;

            try {

                JToken token = JToken.FromObject(source);
                if (token is not null) {

                    return token?.ToObject<T>() ?? null;
                }
                else {
                    error = "Clone. Failed to convert source object to JToken. ";
                }
            }
            catch (Exception ex) {

                error = $"Clone. Attempt to clone object failed. " +
                    $"Exception: {ex.Message}";
            }

            return null;
        }



        public static bool IsJsonObjectType( Type T) {

            var attributes = T.GetCustomAttributes(typeof(JsonObjectAttribute), true);

            return attributes.Length > 0;
        }

        public static bool IsJsonObject(object obj) {

            return IsJsonObjectType(obj.GetType());
        }     

    }

}
