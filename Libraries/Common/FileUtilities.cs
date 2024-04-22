
using LV.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LV.Common.Utilities
{
    public delegate void ErrorEvent(string source, ErrorEventArgs args);

    public class ErrorEventArgs : EventArgs
    {
        public ErrorEventArgs(string source, string message)
        {
            ObjectTypeName = source;
            Description = (string)(message?.Clone() ?? null);
        }

        public String ObjectTypeName { get; private set; }
        public string Description { get; private set; }
    }

    public static class FileUtilities
    {
        public const int DeafultMinTimeoutGranularityMs = 15;
        public const int DefaultFolderCleanUpTimeoutMs = 30000;
        private const int DefaultFolderCleanUpCheckPeriodMs = 50;

        public static event ErrorEvent ErrorEvent;

        private static string _lastError = string.Empty;
        public static string LastError {  
            get {
                return (string) _lastError.Clone();
            }
            set {
                _lastError = value;
                if (!string.IsNullOrEmpty(value)
                    && ErrorEvent.GetInvocationList().Count() > 0) {
                    ErrorEvent.Invoke(null, new ErrorEventArgs("FIleUtilities", (string)value.Clone()));
                }
            }
        } 
        
        public static bool FileExists(string name, List<string> paths = null)
        {
            if (paths == null) {

                var r = File.Exists(name.Replace(@"\\", @"\"));

                return r;
            }

            foreach (string path in paths) {

                string p = Path.Combine(path, Path.GetFileName(name));

                if (File.Exists(p)) {
                    return true;
                }
            }
            return false;
        }

        public static bool DirectoryExists(string path)
        {
            string directoryPath = path;
            if (File.Exists(path))
                directoryPath = Path.GetDirectoryName(path);

            return Directory.Exists(directoryPath);
        }

        public static string CheckFileName(string name, string extension, out string error)
        {
            error = string.Empty;

            if (string.IsNullOrEmpty(name)) {

                error = "Utilities.CheckFileName() name" +
                    " can not be both null or empty";

                return null;
            }

            string ext = (extension.Substring(0, 1) == ".") ? extension : "." + extension;

            string r = name.ToLower();
            string e = ext.ToLower();

            return r.Contains(e) ? name : name + ext;
        }

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

        public static bool SaveTextFile(string text, string directory,
                    string fileName)
        {

            if (!Directory.Exists(directory)) {
                LastError =  $"FileUtilities. Can't save file. Folder {directory} does not exist.";
                return false;
            }

            string filepathName = Path.Combine(directory, fileName);

            try {
                File.WriteAllText(filepathName, text);
                return true;
            }
            catch (Exception e) {
                LastError = $"Failed to save {filepathName}. Exception: {e.Message}";
                return false;
            }
        }

        public static bool SaveTextFileWithDialog(string text, string directory,
                            string fileName, string filter = null)
        {
            string filepathName = Path.Combine(directory, fileName);

            if (string.IsNullOrEmpty(text))
                return false;

            if (string.IsNullOrEmpty(filepathName) || File.Exists(filepathName) || (!Directory.Exists(directory))) {
                SaveFileDialog saveFileDialog = new SaveFileDialog();

                if (Directory.Exists(directory))
                    saveFileDialog.InitialDirectory = directory;

                if (!string.IsNullOrEmpty(filter))
                    saveFileDialog.Filter = filter;

                saveFileDialog.Title = File.Exists(filepathName) ? "Save As" : "Save";

                saveFileDialog.ShowDialog();

                filepathName = saveFileDialog.FileName;
            }

            try {
                File.WriteAllText(filepathName, text);
                return true;
            }
            catch (Exception e) {
                LastError = $"Failed to save {filepathName}. Exception: {e.Message}";
                return false;
            }

        }

        public static string SelectFileToLoad(string directory = null, string filter = null)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (!string.IsNullOrEmpty(filter))
                openFileDialog.Filter = filter;

            if (DirectoryExists(directory))
                openFileDialog.InitialDirectory = directory;

            DialogResult result = openFileDialog.ShowDialog();
            return openFileDialog.FileName;
        }

        public static bool ReadTextFile(string directory, string fileName, out string text, string filter = null)
        {
            string filepathName = Path.Combine(directory, fileName);
            text = null;

            if (!File.Exists(filepathName)) {
                filepathName = SelectFileToLoad(directory, fileName);

                if (string.IsNullOrEmpty(filepathName)) {
                    LastError = "File not found";
                    return false;
                }
            }

            try {
                using (StreamReader file = File.OpenText(filepathName)) {
                    text = file.ReadToEnd();
                }
                return true;
            }
            catch (Exception e) {
                LastError = $"Faild to read text file {filepathName}. " +
                    $"Exception {e.Message}";
                return false;
            }
        }

        public static bool EmptyFolder(string folder,
            out string errorMessage, bool createIfMissing = true)
        {
            errorMessage = string.Empty;
            if (!Directory.Exists(folder)) {

                return true;
            }

            string[] files = null;
            string[] dirs = null;

            try {
                files = Directory.GetFiles(folder);
            }
            catch (Exception ex) {
                LastError = $"Failed to get file list in " +
                    $"\"{folder}\" folder. Exception : {ex.Message}";
                files = new string[0];
            }

            try {
                dirs = Directory.GetDirectories(folder);
            }
            catch (Exception ex) {
                errorMessage = $"Failed to get directory list in " +
                                $"\"{folder}\" folder. Exception : {ex.Message}";
                LastError = $"FileExportHelper. {errorMessage}";
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

                    LastError = $"FileExportHelper. Failed to cleanup folder " +
                        $"\"{folder}\". Can't delete file \"{file}\"";
                    return false;
                }
            }

            return true;
        }

        public static bool CreateFolder(string folderPath, out string errorMessage, int timeoutMs = DefaultFolderCleanUpTimeoutMs)
        {
            errorMessage = string.Empty;

            if (Directory.Exists(folderPath)) {
                return true;
            }

            try {
                Directory.CreateDirectory(folderPath);
                DateTime timeout = DateTime.Now.AddMilliseconds(timeoutMs);

                while (!Directory.Exists(folderPath) && (DateTime.Now < timeout)) {
                    Thread.Sleep(DefaultFolderCleanUpCheckPeriodMs);
                }

                if (Directory.Exists(folderPath)) {
                    return true;
                }

                else {
                    errorMessage = $"Failed to create " +
                        $"folder \"{folderPath}\" during {timeoutMs} ms";
                    LastError =  errorMessage;
                    return false;
                }
            }
            catch (Exception ex) {
                LastError = $"Failed to create folder: {folderPath}. Exeption {ex.Message}";
                return false;
            }
        }

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

                LastError =  $"FileExportHelper. {errorMessage}";
                return false;
            }
        }

        public static bool FolderCleanup(string folder,
                                  out string errorMessage,
                                  int timeoutMs = DefaultFolderCleanUpTimeoutMs)
        {
            LastError =  $"FileExportHelper. Cleaning export folder \"{folder}\".";
            errorMessage = string.Empty;

            if (Directory.Exists(folder)) {

                if (!EmptyFolder(folder, out errorMessage, false)) {
                    LastError =  $"FileExportHelper. FolderCleanup(). " +
                        $"Folder: {folder}. Error: {errorMessage}";
                    return false;
                }
            }

            if (!Directory.Exists(folder)) {

                if (!CreateFolder(folder, out errorMessage, timeoutMs)) {
                    LastError =  $"FileExportHelper. FolderCleanup(). " +
                        $"Folder: {folder}. Error: {errorMessage}";
                    return false;
                }
            }

            return true;
        }
    }
}
