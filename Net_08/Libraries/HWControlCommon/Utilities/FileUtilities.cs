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

namespace Grumpy.SDAQFramework.Utilities
{
    public delegate void ErrorEvent(string source, ErrorEventArgs args);

    public class ErrorEventArgs : EventArgs
    {
        public ErrorEventArgs(string source, string message)
        {
            ObjectTypeName = source;
            Description = (string) (message?.Clone() ?? null!);
        }

        public string ObjectTypeName { get; private set; }
        public string Description { get; private set; }
    }

    /// <summary>
    /// Provides utility methods for file and directory operations, including checking existence, 
    /// creating, deleting, reading, and saving files and folders. Also includes error handling 
    /// and event notification for file-related errors.
    /// </summary>
    public static class FileUtilities
    {
        /// <summary>
        /// Default minimum timeout granularity in milliseconds.
        /// </summary>
        public const int DefaultMinTimeoutGranularityMs = 15;

        /// <summary>
        /// Default timeout for folder cleanup operations in milliseconds.
        /// </summary>
        public const int DefaultFolderCleanUpTimeoutMs = 30000;

        /// <summary>
        ///  Default period in milliseconds for checking folder cleanup status.
        /// </summary>
        private const int DefaultFolderCleanUpCheckPeriodMs = 50;

        /// <summary>
        /// Event triggered when an error occurs during file operations.
        /// </summary>
        public static event ErrorEvent? ErrorEvent;

        private static string _lastError = string.Empty;

        /// <summary>
        /// Gets or sets the last error message. Setting this property triggers the <see cref="ErrorEvent"/> if subscribed.
        /// </summary>
        public static string LastError
        {
            get {
                return (string) _lastError.Clone();
            }
            set {
                _lastError = value;
                if (!string.IsNullOrEmpty(value)
                    && (ErrorEvent?.GetInvocationList().Count() ?? 0) > 0) {
                    ErrorEvent?.Invoke(null!, new ErrorEventArgs("FIleUtilities", (string) value.Clone()));
                }
            }
        }



        /// <summary>
        /// Checks if a file exists in the specified paths or the default location.
        /// </summary>
        /// <param name="name">The name of the file to check.</param>
        /// <param name="fileName">The name of the file if found.</param>
        /// <param name="directory">The directory of the file if found.</param>
        /// <param name="paths">Optional list of paths to search.</param>
        /// <returns>True if the file exists; otherwise, false.</returns>
        public static bool FileExists(string name,
            out string fileName, out string directory,
            List<string>? paths = null)
        {
            directory = string.Empty;
            fileName = string.Empty;

            if (paths == null) {
                if (File.Exists(name.Replace(@"\\", @"\"))) {

                    fileName = Path.GetFileName(name);
                    directory = Path.GetDirectoryName(name)!;

                    return true;
                }

                return false;
            }

            foreach (string p in paths) {

                string fp = Path.Combine(p, Path.GetFileName(name));

                if (File.Exists(fp)) {
                    directory = p;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if a directory exists.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns>True if the directory exists; otherwise, false.</returns>
        public static bool DirectoryExists(string path)
        {
            string directoryPath = path;
            if (File.Exists(path))
                directoryPath = Path.GetDirectoryName(path)!;

            return Directory.Exists(directoryPath);
        }

        /// <summary>
        /// Validates and adjusts a file name to ensure it has the correct extension.
        /// </summary>
        /// <param name="name">The file name to check.</param>
        /// <param name="extension">The required file extension.</param>
        /// <param name="error">Error message if validation fails.</param>
        /// <returns>The validated file name with the correct extension.</returns>
        public static string CheckFileName(string name, string extension, out string error)
        {
            error = string.Empty;

            if (string.IsNullOrEmpty(name)) {

                error = "Utilities.CheckFileName() name" +
                    " can not be both null or empty";

                return string.Empty;
            }

            string ext = extension.Substring(0, 1) == "." ? extension : "." + extension;

            string r = name.ToLower();
            string e = ext.ToLower();

            return r.Contains(e) ? name : name + ext;
        }


        /// <summary>
        /// Deletes a file if it exists.
        /// </summary>
        /// <param name="filePathName">The full path of the file to delete.</param>
        /// <returns>True if the file was deleted; otherwise, false.</returns>
        public static bool DeleteFile(string filePathName)
        {
            if (!string.IsNullOrEmpty(Path.GetFileName(filePathName)))
                if (File.Exists(filePathName)) {
                    File.Delete(filePathName);
                    return true;
                }
            return false;
        }

        /*
        public static void SaveAsPng(this BitmapImage image, string filePath)
        {
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(image));

            using (var fileStream = new System.IO.FileStream(filePath, System.IO.FileMode.Create)) {
                encoder.Save(fileStream);
            }
        }
        */
        /// <summary>
        /// Saves text to a file, with options for appending or overwriting.
        /// </summary>
        /// <param name="text">The text to save.</param>
        /// <param name="directory">The directory to save the file in.</param>
        /// <param name="fileName">The name of the file.</param>
        /// <param name="append">Whether to append to the file if it exists.</param>
        /// <param name="overwrite">Whether to overwrite the file if it exists.</param>
        /// <returns>True if the file was saved successfully; otherwise, false.</returns>
        public static bool SaveTextFile(string text, string? directory,
                    string? fileName, bool append = false,
                    bool overwrite = true)
        {
            if (directory == null) {
                directory = string.Empty;
            }

            if (fileName == null) {
                fileName = string.Empty;
            }

            string filePathName = Path.Combine(directory, fileName!);

            if (string.IsNullOrEmpty(filePathName)) {
                return FailExit("FileUtilities. Can't save file. " +
                    "File path and name are empty.");
            }

            var dir = Path.GetDirectoryName(filePathName);

            if (!Directory.Exists(dir)) {
                return FailExit($"FileUtilities. Can't save file. " +
                    $"Folder {dir} does not exist.");
            }

            try {
                if (!File.Exists(filePathName) || overwrite) {
                    File.WriteAllText(filePathName, text);
                }
                else if (append) {
                    File.AppendAllText(filePathName, text);
                }
                else {
                    return FailExit($"FileUtilities. File already exists");
                }

                return true;
            }
            catch (Exception e) {

                return FailExit($"FileUtilities. Failed to save {filePathName}. " +
                    $"Exception: {e.Message}");
            }
        }


        private static bool FailExit(string message)
        {

            LastError = message;
            return false;
        }

        public static bool SaveTextFileWithDialog(string text,
            string directory, string fileName,
            string filter = null!, bool overwrite = false)
        {
            string filePathName = Path.Combine(directory, fileName);

            if (string.IsNullOrEmpty(filePathName)) {
                return FailExit("File name can't be empty.");
            }

            if (text == null) {
                return FailExit("FileUtilities. " +
                    "Can't save text to file. Text is null.");
            }

            if (string.IsNullOrEmpty(fileName)) {
                return FailExit("FileUtilities. Can't save file. " +
                    "File name is empty.");
            }

            var dir = Path.GetDirectoryName(filePathName);


            if (!Directory.Exists(dir)) {
                return FailExit($"FileUtilities. Can't save file. " +
                    $"Folder {dir} does not exist.");
            }

            if (!Directory.Exists(directory)) {
                try {
                    Directory.CreateDirectory(directory);
                }
                catch {
                    return FailExit($"FileUtilities. " +
                        $"Can't create folder {directory}.");
                }
            }

            try {
                File.WriteAllText(filePathName, text);
                return true;
            }
            catch (Exception e) {
                return FailExit($"FileUtilities. Failed to save {filePathName}. " +
                    $"Exception: {e.Message}");
            }
        }

        /// <summary>
        /// Reads text from a file.
        /// </summary>
        /// <param name="directory">The directory containing the file.</param>
        /// <param name="fileName">The name of the file to read.</param>
        /// <param name="text">The text read from the file.</param>
        /// <param name="filter">Optional filter for the file.</param>
        /// <returns>True if the file was read successfully; otherwise, false.</returns>

        public static bool ReadTextFile(string directory,
            string fileName, out string? text,
            string? filter = null)
        {
            string filePathName = Path.Combine(directory, fileName);
            text = null!;


            try {
                using (StreamReader file = File.OpenText(filePathName)) {
                    text = file.ReadToEnd();
                }
                return true;
            }
            catch (Exception e) {
                return FailExit($"FileUtilities. Faild to read text file " +
                    $"{filePathName}. Exception {e.Message}");
            }
        }

        /// <summary>
        /// Empties a folder by deleting all files and subdirectories.
        /// </summary>
        /// <param name="folder">The folder to empty.</param>
        /// <param name="errorMessage">Error message if the operation fails.</param>
        /// <param name="createIfMissing">Whether to create the folder if it does not exist.</param>
        /// <returns>True if the folder was emptied successfully; otherwise, false.</returns>
        public static bool EmptyFolder(string folder,
            out string errorMessage, bool createIfMissing = true)
        {

            if (string.IsNullOrEmpty(folder)) {
                errorMessage = "FileUtilties. EmptyFolder(). The " +
                    "folder argument is null or empty.";
                LastError = errorMessage;
                return false;
            }

            errorMessage = string.Empty;
            if (!Directory.Exists(folder)) {

                return true;
            }

            string[] files = null!;
            string[] dirs = null!;

            try {
                files = Directory.GetFiles(folder);
            }
            catch (Exception ex) {
                LastError = $"FileUtilities. Failed to get file list in " +
                    $"\"{folder}\" folder. Exception : {ex.Message}";
                files = new string[0];
            }

            try {
                dirs = Directory.GetDirectories(folder);
            }
            catch (Exception ex) {
                errorMessage = $"Failed to get directory list in " +
                                $"\"{folder}\" folder. Exception : {ex.Message}";
                LastError = $"FileUtilities. {errorMessage}";
                dirs = new string[0];
            }

            foreach (string dir in dirs) {

                if (EmptyFolder(dir, out errorMessage)) {

                    try {
                        Directory.Delete(dir, true);
                    }
                    catch {
                        // This folder is empty. Ignore for now. 
                    }

                }
                else {
                    return false;
                }
            }

            foreach (string file in files) {

                try {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
                catch {

                    LastError = $"FileUtilities. Failed to cleanup folder " +
                        $"\"{folder}\". Can't delete file \"{file}\"";
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Creates a folder if it does not exist.
        /// </summary>
        /// <param name="folderPath">The path of the folder to create.</param>
        /// <param name="errorMessage">Error message if the operation fails.</param>
        /// <param name="timeoutMs">Timeout for the operation in milliseconds.</param>
        /// <returns>True if the folder was created successfully; otherwise, false.</returns>

        public static bool CreateFolder(string folderPath,
            out string errorMessage, int timeoutMs = DefaultFolderCleanUpTimeoutMs)
        {
            errorMessage = string.Empty;

            if (Directory.Exists(folderPath)) {
                return true;
            }

            try {
                Directory.CreateDirectory(folderPath);
                DateTime timeout = DateTime.Now.AddMilliseconds(timeoutMs);

                while (!Directory.Exists(folderPath) && DateTime.Now < timeout) {
                    Thread.Sleep(DefaultFolderCleanUpCheckPeriodMs);
                }

                if (Directory.Exists(folderPath)) {
                    return true;
                }

                else {
                    errorMessage = $"Failed to create " +
                        $"folder \"{folderPath}\" during {timeoutMs} ms";
                    LastError = errorMessage;
                    return false;
                }
            }
            catch (Exception ex) {
                LastError = $"Failed to create folder: {folderPath}. Exeption {ex.Message}";
                return false;
            }
        }


        /// <summary>
        /// Deletes a folder and its contents.
        /// </summary>
        /// <param name="folder">The folder to delete.</param>
        /// <param name="errorMessage">Error message if the operation fails.</param>
        /// <returns>True if the folder was deleted successfully; otherwise, false.</returns>
        public static bool DeleteFolder(string folder, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (!Directory.Exists(folder)) { return true; }

            try {
                string[] files = Directory.GetFiles(folder);
                string[] dirs = Directory.GetDirectories(folder);
                foreach (string file in files) {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
                foreach (string dir in dirs) {
                    if (!DeleteFolder(dir, out errorMessage))
                        return false;
                }

                Directory.Delete(folder, true);
                return true;
            }
            catch (Exception ex) {
                errorMessage = $"Failed to delete folder: {folder}. Exeption {ex.Message}";

                LastError = $"FileExportHelper. {errorMessage}";
                return false;
            }
        }

        /// <summary>
        /// Cleans up a folder by emptying it and ensuring it exists.
        /// </summary>
        /// <param name="folder">The folder to clean up.</param>
        /// <param name="errorMessage">Error message if the operation fails.</param>
        /// <param name="timeoutMs">Timeout for the operation in milliseconds.</param>
        /// <returns>True if the folder was cleaned up successfully; otherwise, false.</returns>
        public static bool FolderCleanup(string folder,
                                  out string errorMessage,
                                  int timeoutMs = DefaultFolderCleanUpTimeoutMs)
        {
            LastError = $"FileExportHelper. Cleaning export folder \"{folder}\".";
            errorMessage = string.Empty;

            if (Directory.Exists(folder)) {

                if (!EmptyFolder(folder, out errorMessage, false)) {
                    LastError = $"FileExportHelper. FolderCleanup(). " +
                        $"Folder: {folder}. Error: {errorMessage}";
                    return false;
                }
            }

            if (!Directory.Exists(folder)) {
                if (!CreateFolder(folder, out errorMessage, timeoutMs)) {
                    return FailExit($"FileExportHelper. FolderCleanup(). " +
                        $"Folder: {folder}. Error: {errorMessage}");
                }
            }

            return true;
        }
    }
}
