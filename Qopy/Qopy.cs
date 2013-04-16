using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Management.Automation;
using Microsoft.PowerShell.Commands;


namespace Qopy
{
    public class FileCopyResultsItem
    {
        public string Source;
        public string Destination;
        public long Size;
        public TimeSpan Time;
        public string SourceCRC;
        public string DestinationCRC;
        public bool Match = false;
        public string ErrorMessage = string.Empty;
    }

    public class FileCopyResultsReport
    {
        public TimeSpan TotalTime;
        public int FileCount;
        public long Bytes;
        public List<FileCopyResultsItem> FailedItemList;

        public FileCopyResultsReport()
        {
            FileCount = 0;
            FailedItemList = new List<FileCopyResultsItem>();
        }
    }

    [Cmdlet(VerbsCommon.Copy, "Files")]
    [CmdletBinding]
    public class CopyFiles : Cmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        public string Source
        {
            get { return source; }
            set { source = value.TrimEnd(new char[] {'\\', '/'}); }
        }
        private string source;

        [Parameter(Mandatory = true, Position = 1)]
        public string Destination
        {
            get { return destination; }
            set { destination = value.TrimEnd(new char[] { '\\', '/' }); }
        }
        private string destination;

        [Parameter(Mandatory = false, Position = 2)]
        public string Filter
        {
            get { return filter; }
            set { filter = value; }
        }
        private string filter = "*";

        [Parameter(Mandatory = false, Position = 3)]
        public SwitchParameter Recurse
        {
            get { return recurse; }
            set { recurse = value; }
        }
        private bool recurse;

        [Parameter(Mandatory = false, Position = 4)]
        public SwitchParameter Overwrite
        {
            get { return overwrite; }
            set { overwrite = value; }
        }
        private bool overwrite;

        [Parameter(Mandatory = false, Position = 5)]
        public SwitchParameter ShowProgress
        {
            get { return showProgress; }
            set { showProgress = value; }
        }
        private bool showProgress;


        private IEnumerable<string> listOfFiles = null;
        private List<string> listofDestinationDirs = new List<string>();
        private int countOfFiles = 0;
        private Crc32 crc32 = new Crc32();
        

        protected override void BeginProcessing()
        {
            
            SearchOption searchOption = recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            try
            {
                listOfFiles = Directory.EnumerateFiles(source, filter, searchOption);

                List<string> destPathList = new List<string>();

                foreach (string file in listOfFiles)
                {
                    countOfFiles++;

                    string destinationPath = Path.GetDirectoryName(file.Replace(source, destination));

                    if (!listofDestinationDirs.Contains(destinationPath))
                        listofDestinationDirs.Add(destinationPath);
                }
            }
            catch (ArgumentException ex)
            { WriteError(new ErrorRecord(ex, "1", ErrorCategory.InvalidArgument, source)); }
            catch (DirectoryNotFoundException ex)
            { WriteError(new ErrorRecord(ex, "2", ErrorCategory.ObjectNotFound, source)); }
            catch (IOException ex)
            { WriteError(new ErrorRecord(ex, "3", ErrorCategory.ReadError, source)); }
            catch (UnauthorizedAccessException ex)
            { WriteError(new ErrorRecord(ex, "4", ErrorCategory.PermissionDenied, source)); }
        }

        protected override void EndProcessing()
        {
            if (listOfFiles != null)
            {
                ProgressRecord progress = new ProgressRecord(0, String.Format("Copy from {0} to {1}", source, destination), "Copying");
                DateTime startTime = DateTime.Now;
                int i = 0;

                foreach (string dir in listofDestinationDirs)
                {
                    try
                    {
                        if (!Directory.Exists(dir))
                            Directory.CreateDirectory(dir);
                    }
                    catch (UnauthorizedAccessException ex)
                    { WriteError(new ErrorRecord(ex, "8", ErrorCategory.PermissionDenied, dir)); }
                    catch (PathTooLongException ex)
                    { WriteError(new ErrorRecord(ex, "9", ErrorCategory.InvalidArgument, dir)); }
                    catch (ArgumentNullException ex)
                    { WriteError(new ErrorRecord(ex, "10", ErrorCategory.InvalidArgument, dir)); }
                    catch (ArgumentException ex)
                    { WriteError(new ErrorRecord(ex, "10", ErrorCategory.InvalidArgument, dir)); }
                    catch (DirectoryNotFoundException ex)
                    { WriteError(new ErrorRecord(ex, "11", ErrorCategory.ObjectNotFound, dir)); }
                    catch (NotSupportedException ex)
                    { WriteError(new ErrorRecord(ex, "12", ErrorCategory.InvalidOperation, dir)); }
                    catch (IOException ex)
                    { WriteError(new ErrorRecord(ex, "13", ErrorCategory.WriteError, dir)); }
                }

                foreach (string file in listOfFiles)
                {
                    string fullDestination = file.Replace(source, destination);

                    FileCopyResultsItem item = new FileCopyResultsItem() { Source = file, Destination = fullDestination };

                    DateTime start = DateTime.Now;

                    using (FileStream sourceFs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read ))
                    {
                        foreach (byte b in crc32.ComputeHash(sourceFs)) item.SourceCRC += b.ToString("x2").ToLower();
                        
                        try
                        {
                            using (FileStream dstFs = File.Open(fullDestination, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                            {
                                bool copyTheFile = false;

                                if (sourceFs.Length > 0 && (dstFs.Length == 0 || overwrite)) 
                                    copyTheFile = true;
                                
                                if (dstFs.Length > 0 && overwrite)
                                {
                                    dstFs.SetLength(0);
                                    dstFs.Flush();
                                    copyTheFile = true;

                                }

                                if (copyTheFile)
                                {
                                    sourceFs.Position = 0;
                                    dstFs.Position = 0;
                                    sourceFs.CopyTo(dstFs);
                                }                               
                                
                                dstFs.Position = 0;
                                foreach (byte b in crc32.ComputeHash(dstFs)) item.DestinationCRC += b.ToString("x2").ToLower();
                                item.Size = dstFs.Length;
                            }
                        }
                        catch (UnauthorizedAccessException ex)
                        {   ErrorRecord er = new ErrorRecord(ex, "5", ErrorCategory.SecurityError, fullDestination);
                            item.ErrorMessage = er.Exception.Message;
                        }
                        catch (NotSupportedException ex)
                        {   
                            ErrorRecord er = new ErrorRecord(ex, "5", ErrorCategory.InvalidOperation, sourceFs);
                            item.ErrorMessage = er.Exception.Message;
                        }
                        catch (ObjectDisposedException ex)
                        { 
                            ErrorRecord er = new ErrorRecord(ex, "6", ErrorCategory.ResourceUnavailable, sourceFs);
                            item.ErrorMessage = er.Exception.Message;
                        }
                        catch (IOException ex)
                        { 
                            ErrorRecord er = new ErrorRecord(ex, "7", ErrorCategory.WriteError, fullDestination);
                            item.ErrorMessage = er.Exception.Message;  
                        }

                    }

                    DateTime end = DateTime.Now;

                    item.Time = end - start;
                    item.Match = item.SourceCRC == item.DestinationCRC;

                    int pct = (int)((double)++i / (double)countOfFiles * 100);
                    progress.PercentComplete = pct <= 100 ? pct : 100;
                    progress.SecondsRemaining = (int)(((DateTime.Now - startTime).TotalSeconds / (double)i) * (countOfFiles - i));

                    if (showProgress)
                        WriteProgress(progress);

                    if (!string.IsNullOrEmpty(item.ErrorMessage))
                        WriteVerbose(item.ErrorMessage);

                    WriteObject(item);
                }

                progress.RecordType = ProgressRecordType.Completed;
                progress.PercentComplete = 100;
                
                if (showProgress)
                    WriteProgress(progress);
            }
        }
    }

    [Cmdlet(VerbsCommon.Get, "CopyResultsReport")]
    public class CopyResultsReport : Cmdlet
    {
        [Parameter(Mandatory=true, ValueFromPipeline=true)]
        public FileCopyResultsItem InputObject
        {
            get { return inputObject; }
            set { inputObject = value; }
        }
        private FileCopyResultsItem inputObject;

        FileCopyResultsReport report = new FileCopyResultsReport();

        protected override void ProcessRecord()
        {
            report.TotalTime += inputObject.Time;
            report.FileCount++;
            
            if (!inputObject.Match)
                report.FailedItemList.Add(inputObject);
            else
                report.Bytes += inputObject.Size;
        }

        protected override void EndProcessing()
        {
            WriteObject(report);
        }

    }
}
