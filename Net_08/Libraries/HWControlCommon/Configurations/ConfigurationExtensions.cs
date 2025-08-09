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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Grumpy.SDAQFramework.Utilities;

namespace Grumpy.SDAQFramework.Configuration
{
    /// <summary>
    /// Provides extension methods for JSON serialization, deserialization, cloning, and copying of configuration objects.
    /// </summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Deserializes a JSON string into an object of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of configuration object, must inherit from <see cref="ConfigurationBase"/> and have a <c>JsonObject</c> attribute.</typeparam>
        /// <param name="source">The JSON string to deserialize.</param>
        /// <param name="o">The resulting object if successful; otherwise, null.</param>
        /// <param name="error">The error message if deserialization fails; otherwise, empty.</param>
        /// <returns>True if deserialization was successful; otherwise, false.</returns>
        public static bool DeserializeFromString<T>(string source, out T? o, out string error)
            where T : ConfigurationBase
        {
            error = string.Empty;
            o = null;

            if (!IsJsonObjectType(typeof(T))) {
                error = $"DeserialiseFromString. Target object must have \"JsonObject\" attribute.";
                return false;
            }

            try {
                JToken deserialized = JToken.Parse(source);
                if (deserialized is not null) {
                    o = deserialized.ToObject<T>();
                    error = o != null ? string.Empty :
                        $"Failed to deserialize object of type {typeof(T).FullName} from string.";
                }
            }
            catch (Exception ex) {
                error = ex.Message;
            }

            return string.IsNullOrEmpty(error);
        }

        /// <summary>
        /// Deserializes a JSON file into an object of type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of configuration object, must inherit from <see cref="ConfigurationBase"/> and have a <c>JsonObject</c> attribute.</typeparam>
        /// <param name="source">The file path to the JSON file.</param>
        /// <param name="o">The resulting object if successful; otherwise, null.</param>
        /// <param name="error">The error message if deserialization fails; otherwise, empty.</param>
        /// <param name="paths">Optional list of search paths for the file.</param>
        /// <returns>True if deserialization was successful; otherwise, false.</returns>
        public static bool DeserializeFromFile<T>(string source, out T? o, out string error, List<string> paths = null!) where T : ConfigurationBase
        {
            o = null;

            if (!IsJsonObjectType(typeof(T))) {
                error = $"DeserialiseFromFile. Target object must have \"JsonObject\" attribute.";
                return false;
            }

            return LoadJsonFile(name: source, out string? text, out error) ?
                DeserializeFromString(text!, out o, out error) : false;
        }

        /// <summary>
        /// Loads the contents of a JSON file as a string.
        /// </summary>
        /// <param name="name">The file path to the JSON file.</param>
        /// <param name="jsonText">The loaded JSON text if successful; otherwise, null.</param>
        /// <param name="error">The error message if loading fails; otherwise, empty.</param>
        /// <returns>True if the file was loaded successfully; otherwise, false.</returns>
        public static bool LoadJsonFile(string name, out string? jsonText, out string error)
        {
            error = string.Empty;
            jsonText = null;

            if (FileUtilities.FileExists(name: name, out string fileName, out string directory, paths: null!)) {
                if (!FileUtilities.ReadTextFile(directory: directory, fileName: fileName, out jsonText, filter: null)) {
                    error = FileUtilities.LastError;
                }
            }
            else {
                error = $"File {name} does not exist.";
            }
            return string.IsNullOrEmpty(error);
        }

        /// <summary>
        /// Populates an existing configuration object from a JSON string.
        /// </summary>
        /// <typeparam name="T">The type of configuration object, must inherit from <see cref="ConfigurationBase"/> and have a <c>JsonObject</c> attribute.</typeparam>
        /// <param name="obj">The object to populate.</param>
        /// <param name="text">The JSON string to populate from.</param>
        /// <param name="error">The error message if population fails; otherwise, empty.</param>
        /// <returns>True if population was successful; otherwise, false.</returns>
        public static bool PopulateFromString<T>(this T obj, string text, out string error)
            where T : ConfigurationBase
        {
            if (!IsJsonObjectType(typeof(T))) {
                error = $"PopulateFromString. Target object must have \"JsonObject\" attribute.";
                return false;
            }

            error = string.Empty;

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

        /// <summary>
        /// Populates an existing configuration object from a JSON file.
        /// </summary>
        /// <typeparam name="T">The type of configuration object, must inherit from <see cref="ConfigurationBase"/> and have a <c>JsonObject</c> attribute.</typeparam>
        /// <param name="obj">The object to populate.</param>
        /// <param name="source">The file path to the JSON file.</param>
        /// <param name="error">The error message if population fails; otherwise, empty.</param>
        /// <returns>True if population was successful; otherwise, false.</returns>
        public static bool PopulateFromFile<T>(this T obj, string source, out string error) where T : ConfigurationBase
        {
            if (!IsJsonObjectType(typeof(T))) {
                error = $"PopulateFromFile. Target object must have \"JsonObject\" attribute.";
                return false;
            }

            return LoadJsonFile(source, out string? text, out error) ?
                PopulateFromString(obj, text!, out error) : false;
        }

        /// <summary>
        /// Serializes a configuration object to a JSON string.
        /// </summary>
        /// <typeparam name="T">The type of configuration object, must inherit from <see cref="ConfigurationBase"/> and have a <c>JsonObject</c> attribute.</typeparam>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="error">The error message if serialization fails; otherwise, empty.</param>
        /// <param name="indented">Whether to format the JSON output with indentation.</param>
        /// <returns>The JSON string if successful; otherwise, an empty string.</returns>
        public static string SerializeToString<T>(this T obj, out string error, bool indented = true)
            where T : ConfigurationBase
        {
            error = string.Empty;

            if (!IsJsonObjectType(typeof(T))) {
                error = $"SerializeToString. Target object must have \"JsonObject\" attribute.";
                return string.Empty;
            }

            try {
                return JsonConvert.SerializeObject(obj, indented ? Formatting.Indented : Formatting.None);
            }
            catch (Exception ex) {
                error = $"Object serializationFailed. Exception: {ex.Message}";
                return string.Empty;
            }
        }

        /// <summary>
        /// Serializes a configuration object to a JSON file.
        /// </summary>
        /// <typeparam name="T">The type of configuration object, must inherit from <see cref="ConfigurationBase"/> and have a <c>JsonObject</c> attribute.</typeparam>
        /// <param name="obj">The object to serialize.</param>
        /// <param name="filePathName">The file path to save the JSON to.</param>
        /// <param name="error">The error message if serialization fails; otherwise, empty.</param>
        /// <param name="indented">Whether to format the JSON output with indentation.</param>
        /// <returns>True if serialization and file save were successful; otherwise, false.</returns>
        public static bool SerializeToFile<T>(this T obj, string filePathName, out string error, bool indented = true)
            where T : ConfigurationBase
        {
            if (!IsJsonObjectType(typeof(T))) {
                error = $"SerializeToFile. Target object must have \"JsonObject\" attribute.";
                return false;
            }

            string text = obj.SerializeToString(out error, indented);

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

        /// <summary>
        /// Copies the contents of one configuration object to another using JSON serialization.
        /// </summary>
        /// <typeparam name="T">The type of configuration object, must inherit from <see cref="ConfigurationBase"/> and have a <c>JsonObject</c> attribute.</typeparam>
        /// <param name="target">The target object to copy to.</param>
        /// <param name="source">The source object to copy from.</param>
        /// <param name="error">The error message if the copy fails; otherwise, empty.</param>
        /// <returns>True if the copy was successful; otherwise, false.</returns>
        public static bool CopyFrom<T>(this T target, T source, out string? error) where T : ConfigurationBase
        {
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
                if (!string.IsNullOrEmpty(serialized)) {
                    return target.PopulateFromString(serialized, out error);
                }
            }
            catch (Exception ex) {
                error = $"CopyFrom. Attempt to copy JSON properties failed. Exception: {ex.Message}";
            }

            return false;
        }

        /// <summary>
        /// Creates a deep clone of a configuration object using JSON serialization.
        /// </summary>
        /// <typeparam name="T">The type of configuration object, must inherit from <see cref="ConfigurationBase"/> and have a <c>JsonObject</c> attribute.</typeparam>
        /// <param name="source">The source object to clone.</param>
        /// <param name="error">The error message if cloning fails; otherwise, empty.</param>
        /// <returns>A new cloned object if successful; otherwise, null.</returns>
        public static T? Clone<T>(this T source, out string error)
            where T : ConfigurationBase
        {
            if (!IsJsonObjectType(typeof(T))) {
                error = $"Clone. Target must have \"JsonObject\" attribute.";
                return null;
            }

            error = string.Empty;

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
                error = $"Clone. Attempt to clone object failed. Exception: {ex.Message}";
            }

            return null;
        }

        /// <summary>
        /// Determines whether a type is decorated with the <see cref="JsonObjectAttribute"/>.
        /// </summary>
        /// <param name="T">The type to check.</param>
        /// <returns>True if the type has the <c>JsonObject</c> attribute; otherwise, false.</returns>
        public static bool IsJsonObjectType(Type T)
        {
            var attributes = T.GetCustomAttributes(typeof(JsonObjectAttribute), true);
            return attributes.Length > 0;
        }

        /// <summary>
        /// Determines whether an object is of a type decorated with the <see cref="JsonObjectAttribute"/>.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <returns>True if the object's type has the <c>JsonObject</c> attribute; otherwise, false.</returns>
        public static bool IsJsonObject(object obj)
        {
            return IsJsonObjectType(obj.GetType());
        }
    }
}
