﻿namespace Dragonfly.NetHelpers
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Web;

    /// <summary>
    /// Helpers to handle File I/O
    /// </summary>
    public static class Files
    {
        //TODO: Cleanup, throw errors
        private const string ThisClassName = "Dragonfly.NetHelpers.Files";

        #region Retrieve Remote Files (HTTP)

        /// <summary>
        /// Get a file from a url and save it to the filesystem.
        /// </summary>
        /// <param name="FileUrl">Http url of file to save</param>
        /// <param name="SaveLocationFolder">disk folder where the file should be saved (can be virtual or mapped)</param>
        /// <param name="SaveFileName">Desired filename for saved file</param>
        public static void DownloadAndSaveHttpFile(string FileUrl, string SaveLocationFolder, string SaveFileName)
        {
            string SaveLocation = String.Concat(SaveLocationFolder, "\\", SaveFileName);

            DownloadAndSaveHttpFile(FileUrl, SaveLocation);
        }

        /// <summary>
        /// Get a file from a url and save it to the filesystem.
        /// </summary>
        /// <param name="FileUrl">Http url of file to save</param>
        /// <param name="SaveLocation">Disk location (incl. filename) where the file should be saved (can be virtual or mapped)</param>
        public static void DownloadAndSaveHttpFile(string FileUrl, string SaveLocation)
        {
            string RemoteURL = FileUrl;
            //TODO: Update using new code pattern:
            //var functionName = string.Format("{0}.GetMySQLDataSet", ThisClassName);
            //var msg = string.Format("");
            //Info.LogInfo("Files.DownloadHttpFile: RemoteURL=" + RemoteURL);

            string ServerPath = "";
            try
            {
                ServerPath = HttpContext.Current.Server.MapPath(SaveLocation);
            }
            catch (HttpException exMapPath)
            {
                //TODO: Update using new code pattern:
                //var functionName = string.Format("{0}.GetMySQLDataSet", ThisClassName);
                //var msg = string.Format("");
                //Info.LogException("Files.DownloadFtpFile", exMapPath, "Error Handled: by Code - No problem");
                ServerPath = SaveLocation;
            }

            //FileStream writeStream = new FileStream(ServerPath, FileMode.Create);

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(RemoteURL);
                request.Method = WebRequestMethods.Http.Get;

                //Stream fileResponseStream;

                HttpWebResponse fileResponse = (HttpWebResponse)request.GetResponse();

                //fileResponseStream = fileResponse.GetResponseStream();

                using (Stream writeStream = File.OpenWrite(ServerPath))
                using (Stream fileResponseStream = fileResponse.GetResponseStream())
                {
                    fileResponseStream.CopyTo(writeStream);
                }
            }
            catch (Exception ex)
            {
                //TODO: Update using new code pattern:
                //var functionName = string.Format("{0}.GetMySQLDataSet", ThisClassName);
                //var msg = string.Format("");
                //Info.LogException("Files.DownloadFtpFile", ex);
            }
        }

        #endregion

        #region FTP
        public static string[] GetFtpFileList(string FtpHostServer, string FtpUserName, string FtpPassword)
        {
            string[] downloadFiles;
            StringBuilder result = new StringBuilder();
            WebResponse response = null;
            StreamReader reader = null;
            try
            {
                FtpWebRequest reqFTP;
                reqFTP = (FtpWebRequest)FtpWebRequest.Create(new Uri("ftp://" + FtpHostServer + "/"));
                reqFTP.UseBinary = true;
                reqFTP.Credentials = new NetworkCredential(FtpUserName, FtpPassword);
                reqFTP.Method = WebRequestMethods.Ftp.ListDirectory;
                reqFTP.Proxy = null;
                reqFTP.KeepAlive = false;
                reqFTP.UsePassive = false;
                response = reqFTP.GetResponse();
                reader = new StreamReader(response.GetResponseStream());
                string line = reader.ReadLine();
                while (line != null)
                {
                    result.Append(line);
                    result.Append("\n");
                    line = reader.ReadLine();
                }
                // to remove the trailing '\n'
                result.Remove(result.ToString().LastIndexOf('\n'), 1);
                return result.ToString().Split('\n');
            }
            catch (Exception ex)
            {
                if (reader != null)
                {
                    reader.Close();
                }
                if (response != null)
                {
                    response.Close();
                }
                downloadFiles = null;
                return downloadFiles;
            }
        }

        public static bool DownloadFtpFile(string FtpHostServer, string FtpUserName, string FtpPassword, string FtpDirectoryPath, string FtpFileName, string SaveLocationPath, string SaveFileName)
        {
            string RemoteURL = "ftp://" + FtpHostServer + "/" + FtpDirectoryPath + "/" + FtpFileName;
            //TODO: Update using new code pattern:
            //var functionName = string.Format("{0}.GetMySQLDataSet", ThisClassName);
            //var msg = string.Format("");
            //Info.LogInfo("Files.DownloadFtpFile: RemoteURL=" + RemoteURL);

            string FilePath = FtpDirectoryPath + "/" + FtpFileName;
            string ServerPath = "";
            try
            {
                ServerPath = HttpContext.Current.Server.MapPath(SaveLocationPath);
            }
            catch (HttpException exMapPath)
            {
                //TODO: Update using new code pattern:
                //var functionName = string.Format("{0}.GetMySQLDataSet", ThisClassName);
                //var msg = string.Format("");
                //Info.LogException("Files.DownloadFtpFile", exMapPath, "Error Handled: by Code - No problem");
                ServerPath = SaveLocationPath;
            }

            string FullSaveLocation = String.Concat(ServerPath, "\\", SaveFileName);
            //TODO: Update using new code pattern:
            //var functionName = string.Format("{0}.GetMySQLDataSet", ThisClassName);
            //var msg = string.Format("");
            //Info.LogInfo("Files.DownloadFtpFile: FullSaveLocation=" + FullSaveLocation);

            //Test that server can be accessed
            //TODO: Update using new code pattern:
            //var functionName = string.Format("{0}.GetMySQLDataSet", ThisClassName);
            //var msg = string.Format("");
            //Info.LogInfo("Files.DownloadFtpFile: FtpServerStatus=" + FtpServerStatus(FtpHostServer, FtpUserName, FtpPassword));
            //Info.LogInfo("Files.DownloadFtpFile: FtpDirectoryStatus=" + FtpDirectoryStatus(FtpHostServer, FtpUserName, FtpPassword, FtpDirectoryPath));

            FileStream writeStream = new FileStream(FullSaveLocation, FileMode.Create);
            try
            {
                long length = GetFileLength(FtpHostServer, FtpUserName, FtpPassword, FilePath, true);
                //TODO: Update using new code pattern:
                //var functionName = string.Format("{0}.GetMySQLDataSet", ThisClassName);
                //var msg = string.Format("");
                //Info.LogInfo("Files.DownloadFtpFile: GetFileLength(RemoteURL)=" + length);
                long offset = 0;
                int retryCount = 10;
                int? readTimeout = 5 * 60 * 1000; //five minutes

                while (retryCount > 0)
                {
                    using (Stream responseStream = GetFileAsStream(RemoteURL, FtpUserName, FtpPassword, true, offset, requestTimeout: readTimeout != null ? readTimeout.Value : Timeout.Infinite))
                    {
                        //TODO: Update using new code pattern:
                        //var functionName = string.Format("{0}.GetMySQLDataSet", ThisClassName);
                        //var msg = string.Format("");
                        //Info.LogInfo("Files.DownloadFtpFile: GetFileAsStream(RemoteURL).length" + responseStream.Length);

                        using (FileStream fileStream = new FileStream(FullSaveLocation, FileMode.Append))
                        {
                            byte[] buffer = new byte[4096];
                            try
                            {
                                int bytesRead = responseStream.Read(buffer, 0, buffer.Length);

                                while (bytesRead > 0)
                                {
                                    fileStream.Write(buffer, 0, bytesRead);

                                    bytesRead = responseStream.Read(buffer, 0, buffer.Length);
                                }

                                return true;
                            }
                            catch (WebException exWeb)
                            {
                                // Do nothing - consume this exception to force a new read of the rest of the file

                                //TODO: Update using new code pattern:
                                //var functionName = string.Format("{0}.GetMySQLDataSet", ThisClassName);
                                //var msg = string.Format("");
                                //Info.LogException("Files.DownloadFtpFile", webex, "HANDLED");
                            }
                        }

                        //TODO: Update using new code pattern:
                        //var functionName = string.Format("{0}.GetMySQLDataSet", ThisClassName);
                        //var msg = string.Format("");
                        //Info.LogInfo("Files.DownloadFtpFile : File.Exists(FullSaveLocation)=", File.Exists(FullSaveLocation));

                        if (File.Exists(FullSaveLocation))
                        {
                            offset = new FileInfo(FullSaveLocation).Length;
                        }
                        else
                        {
                            offset = 0;
                        }

                        retryCount--;

                        if (offset == length)
                        {
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //TODO: Update using new code pattern:
                //var functionName = string.Format("{0}.GetMySQLDataSet", ThisClassName);
                //var msg = string.Format("");
                //Info.LogException("Files.DownloadFtpFile", ex);
            }

            return false;
        }

        private static string FtpDirectoryStatus(string FtpHostServer, string username, string password, string FtpFolderPathToTest = "")
        {
            string ReturnMsg = "";
            string FullDirToTest = "ftp://" + FtpHostServer + "/" + FtpFolderPathToTest;
            //TODO: Update using new code pattern:
            //var functionName = string.Format("{0}.GetMySQLDataSet", ThisClassName);
            //var msg = string.Format("");
            //Info.LogInfo("Files.FtpDirectoryExists: FullDirToTest=" + FullDirToTest);

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(FullDirToTest);
            request.Credentials = new NetworkCredential(username, password);
            string CredentialsInfo = request.Credentials.ToString();
            request.Method = WebRequestMethods.Ftp.ListDirectory;

            try
            {
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    // Okay.  
                    ReturnMsg = "Server Connection Sucessful";
                }
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    FtpWebResponse response = (FtpWebResponse)ex.Response;
                    ReturnMsg = response.StatusCode.ToString();

                    if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                    {
                        // Directory not found.  
                        ReturnMsg += " - " + FullDirToTest;
                    }
                    else if (response.StatusCode == FtpStatusCode.NotLoggedIn)
                    {
                        // Directory not found.  
                        ReturnMsg += " - " + CredentialsInfo;
                    }

                }
                ReturnMsg = "Unknown";
            }
            return ReturnMsg;
        }

        private static string FtpServerStatus(string FtpHostServer, string username, string password)
        {
            string ReturnMsg = "";
            string ServerToTest = "ftp://" + FtpHostServer + "/";
            //TODO: Update using new code pattern:
            //var functionName = string.Format("{0}.GetMySQLDataSet", ThisClassName);
            //var msg = string.Format("");
            //Info.LogInfo("Files.FtpServerStatus: ServerToTest=" + ServerToTest);

            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ServerToTest);
            request.Credentials = new NetworkCredential(username, password);
            string CredentialsInfo = request.Credentials.ToString();
            request.Method = WebRequestMethods.Ftp.ListDirectory;

            try
            {
                using (FtpWebResponse response = (FtpWebResponse)request.GetResponse())
                {
                    // Okay.  
                    ReturnMsg = "Server Connection Sucessful";
                }
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                {
                    FtpWebResponse response = (FtpWebResponse)ex.Response;
                    ReturnMsg = response.StatusCode.ToString();

                    if (response.StatusCode == FtpStatusCode.ActionNotTakenFileUnavailable)
                    {
                        // Directory not found.  
                        ReturnMsg += " - " + ServerToTest;
                    }
                    else if (response.StatusCode == FtpStatusCode.NotLoggedIn)
                    {
                        // Directory not found.  
                        ReturnMsg += " - " + CredentialsInfo;
                    }

                }
                ReturnMsg = "Unknown";
            }
            return ReturnMsg;
        }

        private static Stream GetFileAsStream(string ftpUrl, string username, string password, bool usePassive, long offset, int requestTimeout)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(ftpUrl);

            request.KeepAlive = false;
            request.ReadWriteTimeout = requestTimeout;
            request.Timeout = requestTimeout;
            request.ContentOffset = offset;
            request.UsePassive = usePassive;
            request.UseBinary = true;

            request.Credentials = new NetworkCredential(username, password);

            request.Method = WebRequestMethods.Ftp.DownloadFile;

            Stream fileResponseStream;

            FtpWebResponse fileResponse = (FtpWebResponse)request.GetResponse();

            fileResponseStream = fileResponse.GetResponseStream();

            return fileResponseStream;
        }


        #endregion

        #region Create Files/Folders

        /// <summary>
        /// Will check for the existence of a directory on disk and create it if missing
        /// </summary>
        /// <param name="FolderPath">Path to directory</param>
        /// <returns>TRUE if sucessful</returns>
        public static bool CreateDirectoryIfMissing(string FolderPath)
        {
            bool Success = false;

            string MappedFolderPath = GetMappedPath(FolderPath);

            if (Directory.Exists(MappedFolderPath))
            {
                Success = true;
            }
            else
            {
                try
                {
                    Directory.CreateDirectory(MappedFolderPath);
                    Success = true;
                }
                catch (Exception ex)
                {
                    //TODO: Update using new code pattern:
                    //var functionName = string.Format("{0}.GetMySQLDataSet", ThisClassName);
                    //var msg = string.Format("");
                    //Info.LogException("Files.CreateDirectoryIfMissing", ex, "[MappedFolderPath=" + MappedFolderPath + "]");
                    Success = false;
                }
            }
            return Success;
        }

        /// <summary>
        /// Creates an empty file at a location, creating directories as needed
        /// </summary>
        /// <param name="FullFilePath">Path for directories and file</param>
        /// <returns>Filestream for new file</returns>
        public static FileStream CreateFileAndDirectory(string FullFilePath)
        {
            string directoryName = Path.GetDirectoryName(FullFilePath);

            if (Directory.Exists(directoryName) == false)
            {
                Directory.CreateDirectory(directoryName);
            }

            FileStream fs = File.Create(FullFilePath);

            return fs;
        }

        /// <summary>
        /// Writes some text to a provided file location.
        /// </summary>
        /// <param name="FilePath">Virtual or Physical path - Inlcuding the desired filename with a text-compatible extension (ex: .txt, .xml, .json, etc.)</param>
        /// <param name="TextContent">Text to write to file</param>
        /// <param name="CreateDirectoryIfMissing">If the directories int he path don't exist, create them rather than failing</param>
        /// <param name="FailSilently">If TRUE won't throw an error on failure. Included for backward compatibility.</param>
        /// <returns></returns>
        public static bool CreateTextFile(string FilePath, string TextContent, bool CreateDirectoryIfMissing = false, bool FailSilently = true)
        {
            var mappedFilePath = Files.GetMappedPath(FilePath);

            try
            {
                if (CreateDirectoryIfMissing)
                {
                    string directoryName = Path.GetDirectoryName(Files.GetMappedPath(FilePath));

                    if (Directory.Exists(directoryName) == false)
                    {
                        Directory.CreateDirectory(directoryName);
                    }
                }

                // WriteAllText creates a file, writes the specified string to the file,
                // and then closes the file.    You do NOT need to call Flush() or Close().
                System.IO.File.WriteAllText(mappedFilePath, TextContent);
            }
            catch (Exception ex)
            {
                if (!FailSilently)
                {
                    //Pass error back up
                    throw ex;
                }

                return false;
                //var msg = "";
                //if (ex.Message.Contains("path's format is not supported"))
                //{
                //    var functionName = string.Format("{0}.CreateTextFile", ThisClassName);
                //    if (mappedFilePath.Contains(":"))
                //    {msg = "Do you have a colon in the filename?";}
                //   // Info.LogException(functionName, ex, msg);
                //}
            }
            return true;
        }

        /// <summary>
        /// Writes text to a file
        /// </summary>
        /// <param name="FilePath">Path and filename</param>
        /// <param name="TextToWrite">Text content to add to file</param>
        /// <param name="Overwrite">If FALSE will just append as a line to existing file contents, TRUE will overwrite all file contents</param>
        /// <param name="PrefixWithTimestamp">Add a timestamp to the beginning of the line appended (useful for log files)</param>
        public static void WriteToTextFile(string FilePath, string TextToWrite, bool Overwrite = false, bool PrefixWithTimestamp = true)
        {
            string LogFilePath = "";
            try
            {
                LogFilePath = HttpContext.Current.Server.MapPath(FilePath);
            }
            catch (System.Web.HttpException exMapPath)
            {
                var functionName = string.Format("{0}.WriteToTextFile", ThisClassName);
                //Info.LogException(functionName, exMapPath, "(Error handled by Code)", true);
                LogFilePath = FilePath;
            }

            string textLine;

            if (PrefixWithTimestamp)
            {
                textLine = DateTime.Now + "---" + TextToWrite;
                //('yyyy-mm-dd-HH:MM:SS') + 
            }
            else
            {
                textLine = TextToWrite;
            }

            if (Overwrite == true | File.Exists(LogFilePath) == false)
            {
                FileStream fsNew = Files.CreateFileAndDirectory(LogFilePath);
                StreamWriter swNew = new StreamWriter(fsNew);
                swNew.WriteLine(textLine);
                swNew.Close();
                fsNew.Close();
            }
            else
            {
                StreamWriter swAppend = File.AppendText(LogFilePath);
                swAppend.WriteLine(textLine);
                swAppend.Close();
            }
        }

        #endregion

        #region Read Files

        /// <summary>
        /// Reads a Text file, returning contents as a string
        /// </summary>
        /// <param name="FilePath">Full path to file</param>
        /// <returns></returns>
        public static string GetTextFileContents(string FilePath)
        {
            var mappedFilePath = Files.GetMappedPath(FilePath);

            string readText = File.ReadAllText(mappedFilePath);

            return readText;
        }

        //public static bool DisplayFileFromServer(Uri serverUri)
        //{
        //    // The serverUri parameter should start with the ftp:// scheme. 
        //    if (serverUri.Scheme != Uri.UriSchemeFtp)
        //    {
        //        return false;
        //    }
        //    // Get the object used to communicate with the server.
        //    WebClient request = new WebClient();

        //    // This example assumes the FTP site uses anonymous logon.
        //    request.Credentials = new NetworkCredential("anonymous", "janeDoe@contoso.com");
        //    try
        //    {
        //        byte[] newFileData = request.DownloadData(serverUri.ToString());
        //        string fileString = System.Text.Encoding.UTF8.GetString(newFileData);
        //        Console.WriteLine(fileString);
        //    }
        //    catch (WebException e)
        //    {
        //        Console.WriteLine(e.ToString());
        //    }
        //    return true;
        //}

        #endregion

        #region Get File Information

        /// <summary>
        /// Checks whether a file exists on disk
        /// </summary>
        /// <param name="FullFilePath">Relative or Mapped Path</param>
        /// <returns>True if file found, false if not</returns>
        public static bool FileExists(string FullFilePath)
        {
            string mappedFilePath = "";
            try
            {
                mappedFilePath = HttpContext.Current.Server.MapPath(FullFilePath);
            }
            catch (System.Web.HttpException exMapPath)
            {
                var functionName = string.Format("{0}.FileExists", ThisClassName);
                //Info.LogException(functionName, exMapPath, "(Error handled by Code)", true);
                mappedFilePath = FullFilePath;
            }

            if (File.Exists(mappedFilePath))
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        /// <summary>
        /// Convert bytes into a friendlier format
        /// </summary>
        /// <param name="Bytes">value of the file size in bytes</param>
        /// <param name="FormatString">Adjust the format string to your preferences. For example "{0:0.#}{1}" would show a single decimal place, and no space.</param>
        /// <returns></returns>
        public static string GetFriendlyFileSize(double Bytes, string FormatString = "{0:0.##} {1}")
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = Bytes;
            int order = 0;
            while (len >= 1024 && order + 1 < sizes.Length)
            {
                order++;
                len = len / 1024;
            }

            string result = string.Format(FormatString, len, sizes[order]);

            return result;
        }

        public static Size GetJpegDimensions(string filename)
        {
            FileStream stream = null;
            BinaryReader rdr = null;
            try
            {
                stream = System.IO.File.OpenRead(filename);
                rdr = new BinaryReader(stream);
                // keep reading packets until we find one that contains Size info
                for (; ; )
                {
                    byte code = rdr.ReadByte();
                    if (code != 0xFF) throw new ApplicationException(
                               "Unexpected value in file " + filename);
                    code = rdr.ReadByte();
                    switch (code)
                    {
                        // filler byte
                        case 0xFF:
                            stream.Position--;
                            break;
                        // packets without data
                        case 0xD0:
                        case 0xD1:
                        case 0xD2:
                        case 0xD3:
                        case 0xD4:
                        case 0xD5:
                        case 0xD6:
                        case 0xD7:
                        case 0xD8:
                        case 0xD9:
                            break;
                        // packets with size information
                        case 0xC0:
                        case 0xC1:
                        case 0xC2:
                        case 0xC3:
                        case 0xC4:
                        case 0xC5:
                        case 0xC6:
                        case 0xC7:
                        case 0xC8:
                        case 0xC9:
                        case 0xCA:
                        case 0xCB:
                        case 0xCC:
                        case 0xCD:
                        case 0xCE:
                        case 0xCF:
                            ReadBEUshort(rdr);
                            rdr.ReadByte();
                            ushort h = ReadBEUshort(rdr);
                            ushort w = ReadBEUshort(rdr);
                            return new System.Drawing.Size(w, h);
                        // irrelevant variable-length packets
                        default:
                            int len = ReadBEUshort(rdr);
                            stream.Position += len - 2;
                            break;
                    }
                }
            }
            finally
            {
                if (rdr != null) rdr.Close();
                if (stream != null) stream.Close();
            }
        }

        private static long GetFileLength(string FtpHostServer, string username, string password, string FtpFilePath, bool usePassive)
        {
            string RemoteURL = "ftp://" + FtpHostServer + "/" + FtpFilePath;

            FtpWebRequest requestServerTest = (FtpWebRequest)WebRequest.Create(FtpHostServer);
            requestServerTest.Credentials = new NetworkCredential(username, password);
            requestServerTest.Method = WebRequestMethods.Ftp.ListDirectory;
            FtpWebResponse ServerResponse = (FtpWebResponse)requestServerTest.GetResponse();
            //TODO: Update using new code pattern:
            //var functionName = string.Format("{0}.GetMySQLDataSet", ThisClassName);
            //var msg = string.Format("");
            //Info.LogInfo("Files.GetFileLength : Server Test Response=" + ServerResponse.ToString());


            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(RemoteURL);
            //TODO: Update using new code pattern:
            //var functionName = string.Format("{0}.GetMySQLDataSet", ThisClassName);
            //var msg = string.Format("");
            //Info.LogInfo("Files.GetFileLength : RequestUri=" + request.RequestUri);
            request.KeepAlive = false;
            request.UsePassive = usePassive;
            request.Credentials = new NetworkCredential(username, password);
            request.Method = WebRequestMethods.Ftp.GetFileSize;

            FtpWebResponse lengthResponse = (FtpWebResponse)request.GetResponse();
            long length = lengthResponse.ContentLength;
            lengthResponse.Close();
            return length;

        }

        private static ushort ReadBEUshort(BinaryReader rdr)
        {
            ushort hi = rdr.ReadByte();
            hi <<= 8;
            ushort lo = rdr.ReadByte();
            return (ushort)(hi | lo);
        }

        #endregion

        #region MapPath

        public static string UnMapPath(string MappedPath)
        {
            string RootMapPath = HttpContext.Current.Server.MapPath("/");
            string VirtualPath = "";


            VirtualPath = MappedPath.ToLower(); //start with the provided MappedPath, standardized to lowercase to make replacing easy.
            VirtualPath = VirtualPath.Replace(RootMapPath.ToLower(), ""); //Get rid of the portion to the website root

            string BackslashChar = @"\";
            VirtualPath = VirtualPath.Replace(BackslashChar, "/"); //flip the slashes

            return VirtualPath;
        }

        public static string GetMappedPath(string MappedOrRelativePath)
        {
            string MappedFolderPath = "";
            if (MappedOrRelativePath != null)
            {
                try
                {
                    MappedFolderPath = HttpContext.Current.Server.MapPath(MappedOrRelativePath);
                }
                catch (HttpException exMapPath)
                {
                    //TODO: Update using new code pattern:
                    //var functionName = string.Format("{0}.GetMySQLDataSet", ThisClassName);
                    //var msg = string.Format("");
                    //Info.LogException("Files.GetMappedPath", exMapPath, "(Error handled by Code - Path was already mapped)", true);
                    MappedFolderPath = MappedOrRelativePath;
                }
            }

            return MappedFolderPath;
        }

        #endregion

    }
}