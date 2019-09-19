using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using ZipExtractor.Properties;

namespace ZipExtractor
{
    public partial class FormMain : Form
    {
        private BackgroundWorker _backgroundWorker;
        private readonly StringBuilder _logBuilder = new StringBuilder();

        public FormMain()
        {
            InitializeComponent();
        }

        private void FormMain_Shown(object sender, EventArgs e)
        {
            _logBuilder.AppendLine(DateTime.Now.ToString("F"));
            _logBuilder.AppendLine();
            _logBuilder.AppendLine("ZipExtractor started with following command line arguments.");

            var args = Environment.GetCommandLineArgs();
            for (var index = 0; index < args.Length; index++)
            {
                var arg = args[index];
                _logBuilder.AppendLine($"[{index}] {arg}");
            }

            _logBuilder.AppendLine();

            if (args.Length >= 3)
            {
                // Extract all the files.
                _backgroundWorker = new BackgroundWorker
                {
                    WorkerReportsProgress = true,
                    WorkerSupportsCancellation = true
                };

                _backgroundWorker.DoWork += (o, eventArgs) =>
                {
                    foreach (var process in Process.GetProcesses())
                    {
                        try
                        {
                            if (process.MainModule.FileName.Equals(args[2]))
                            {
                                _logBuilder.AppendLine("Waiting for application process to Exit...");

                                _backgroundWorker.ReportProgress(0, "Waiting for application to Exit...");
                                process.Kill();
                                process.WaitForExit();
                            }
                        }
                        catch (Exception exception)
                        {
                            Debug.WriteLine(exception.Message);
                        }
                    }

                    Thread.Sleep(2000);

                    _logBuilder.AppendLine("BackgroundWorker started successfully.");

                    var path = Path.GetDirectoryName(args[2]);

                    // Open an existing zip file for reading.
                    var zip = ZipStorer.Open(args[1], FileAccess.Read);

                    // Read the central directory collection.
                    var dir = zip.ReadCentralDir();

                    _logBuilder.AppendLine($"Found total of {dir.Count} files and folders inside the zip file.");

                    for (var index = 0; index < dir.Count; index++)
                    {
                        if (_backgroundWorker.CancellationPending)
                        {
                            eventArgs.Cancel = true;
                            zip.Close();
                            return;
                        }
                                                
                        var entry = dir[index];                        
                        var attemptCount = 1;
                        var tryAgain = true;
                        while (tryAgain)
                        {
                            try
                            {
                                _logBuilder.AppendLine($"Extracting File {entry.FilenameInZip}, attempt {attemptCount}");

                                zip.ExtractFile(entry, Path.Combine(path, entry.FilenameInZip));
                                tryAgain = false;
                            }
                            catch
                            {
                                if(attemptCount > 10)
                                {
                                    throw;
                                }
                                attemptCount++;
                                tryAgain = true;
                                Thread.Sleep(2000);
                            }
                        }
                        var currentFile = string.Format(Resources.CurrentFileExtracting, entry.FilenameInZip);
                        var progress = (index + 1) * 100 / dir.Count;
                        _backgroundWorker.ReportProgress(progress, currentFile);

                        _logBuilder.AppendLine($"{currentFile} [{progress}%]");
                    }

                    zip.Close();
                };

                _backgroundWorker.ProgressChanged += (o, eventArgs) =>
                {
                    progressBar.Value = eventArgs.ProgressPercentage;
                    labelInformation.Text = eventArgs.UserState.ToString();
                };

                _backgroundWorker.RunWorkerCompleted += (o, eventArgs) =>
                {
                    try
                    {
                        if (eventArgs.Error != null)
                        {
                            throw eventArgs.Error;
                        }

                        if (!eventArgs.Cancelled)
                        {
                            labelInformation.Text = @"Finished";
                            try
                            {
                                var processStartInfo = new ProcessStartInfo(args[2]);
                                if (args.Length > 3)
                                {
                                    processStartInfo.Arguments = args[3];
                                }

                                Process.Start(processStartInfo);

                                _logBuilder.AppendLine("Successfully launched the updated application.");
                            }
                            catch (Win32Exception exception)
                            {
                                if (exception.NativeErrorCode != 1223)
                                {
                                    throw;
                                }
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        _logBuilder.AppendLine();
                        _logBuilder.AppendLine(exception.ToString());

                        MessageBox.Show(exception.Message, exception.GetType().ToString(),
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        _logBuilder.AppendLine();
                        Application.Exit();
                    }
                };

                _backgroundWorker.RunWorkerAsync();
            }
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            _backgroundWorker?.CancelAsync();

            _logBuilder.AppendLine();
            File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ZipExtractor.log"), _logBuilder.ToString());
        }
    }
}