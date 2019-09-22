using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace AutoUpdater.ZipInstaller
{
    internal class Program
    {
        private static void CopyFiles(StringBuilder logBuilder, string zipSetupFilePath, string assemblyName)
        {
            var unZipSetupFolderPath = $"{zipSetupFilePath}/{Guid.NewGuid()}";
            logBuilder.AppendLine($"Extracting to Folder {unZipSetupFolderPath}");

            if (Directory.Exists(unZipSetupFolderPath) == false)
            {
                Directory.CreateDirectory(unZipSetupFolderPath);
            }

            ZipFile.ExtractToDirectory(zipSetupFilePath, unZipSetupFolderPath, true);

            logBuilder.AppendLine($"Done Extracting");

            var assemblyFilePath = $"{unZipSetupFolderPath}/{assemblyName}";

            logBuilder.AppendLine($"Assembly File Path [{assemblyFilePath}]");

            var appAssembly = Assembly.LoadFrom(assemblyFilePath);
            var companyAttribute = GetAttribute<AssemblyCompanyAttribute>(appAssembly);
            var titleAttribute = GetAttribute<AssemblyTitleAttribute>(appAssembly);
            var versionAttribute = GetAttribute<AssemblyVersionAttribute>(appAssembly);

            var appCompany = companyAttribute != null ? companyAttribute.Company : string.Empty;
            var appTitle = titleAttribute != null ? titleAttribute.Title : appAssembly.GetName().Name;
            var appVersion = versionAttribute != null ? versionAttribute.Version : appAssembly.GetName().Version?.ToString();

            logBuilder.AppendLine($"App Company [{appCompany}]");
            logBuilder.AppendLine($"App Title [{appTitle}]");
            logBuilder.AppendLine($"App Version [{appVersion}]");

            var appFodlerPath = "%ProgramData%";
            if (string.IsNullOrWhiteSpace(appCompany) == false)
            {
                appFodlerPath += $"/{appCompany}";
            }
            if (string.IsNullOrWhiteSpace(appTitle) == false)
            {
                appFodlerPath += $"/{appTitle}";
            }

            logBuilder.AppendLine($"App Fodler Path [{appFodlerPath}]");

            var appVersionFodlerPath = appFodlerPath;
            if (string.IsNullOrWhiteSpace(appVersion) == false)
            {
                appVersionFodlerPath += $"/{appVersion}";
            }

            if (Directory.Exists(appVersionFodlerPath))
            {
                Directory.Delete(appVersionFodlerPath, true);
            }
            Directory.Move(unZipSetupFolderPath, appVersionFodlerPath);
        }

        private static T GetAttribute<T>(Assembly assembly)
        {
            var attributes = assembly.GetCustomAttributes(typeof(T), false);
            if (attributes.Length == 0)
            {
                return default;
            }

            return (T)attributes[0];
        }

        private static (string Message, string StackTrace) GetErrorData(Exception ex)
        {
            var message = $"{ex.Message}";
            var stackTrace = $"{ex.StackTrace}";

            var innerException = ex.InnerException;
            while (innerException != null)
            {
                message += $"{Environment.NewLine} { innerException.Message}";
                stackTrace += $"{Environment.NewLine} { innerException.StackTrace}";

                innerException = innerException.InnerException;
            }

            return (message, stackTrace);
        }

        private static void Main(string[] args)
        {
            var logBuilder = new StringBuilder();
            try
            {
                logBuilder.AppendLine("--------------------------------------------------------------");
                logBuilder.AppendLine(DateTime.Now.ToString("F"));
                logBuilder.AppendLine();
                logBuilder.AppendLine($"{typeof(Program).Namespace} started with following command line arguments.");

                foreach (var arg in args)
                {
                    logBuilder.AppendLine($"[{Array.IndexOf(args, arg)}] {arg}");
                }

                logBuilder.AppendLine();

                var argCount = 3;
                if (args.Length < argCount)
                {
                    logBuilder.AppendLine($"Must have at least {argCount} arguments");
                    return;
                }

                var zipSetupFilePath = args[0];
                var assemblyName = args[1];
                var appExePath = args[2];

                logBuilder.AppendLine("Started Installation");

                CopyFiles(logBuilder, zipSetupFilePath, assemblyName);

                logBuilder.AppendLine("Finished Installation");

                logBuilder.AppendLine("Started launching the App");

                RunApp(logBuilder, appExePath);

                logBuilder.AppendLine("Finised launching the App");
            }
            catch (Exception ex)
            {
                logBuilder.AppendLine();
                var errorData = GetErrorData(ex);
                logBuilder.AppendLine($"{errorData.Message} {Environment.NewLine} {errorData.StackTrace}");
            }
            finally
            {
                try
                {
                    logBuilder.AppendLine("--------------------------------------------------------------");
                    File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AutoUpdaterZipInstaller.log"), logBuilder.ToString());
                }
                catch
                {
                    // Igone;
                }
            }
        }

        private static void RunApp(StringBuilder logBuilder, string appExePath)
        {
            if (string.IsNullOrWhiteSpace(appExePath))
            {
                logBuilder.AppendLine("App Exe Path not set");
                return;
            }
            if (File.Exists(appExePath) == false)
            {
                logBuilder.AppendLine($"App Exe does not exist [{appExePath}]");
                return;
            }

            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    string processPath;
                    try
                    {
                        processPath = process.MainModule.FileName;
                    }
                    catch (Win32Exception)
                    {
                        // Current process should be same as processes created by other instances of the application so it should be able to access modules of other instances.
                        // This means this is not the process we are looking for so we can safely skip this.
                        continue;
                    }
                    if (processPath != appExePath)
                    {
                        continue;
                    }

                    logBuilder.AppendLine("Waiting for application process to Exit...");

                    if (process.CloseMainWindow())
                    {
                        process.WaitForExit((int)TimeSpan.FromSeconds(10).TotalMilliseconds); //give some time to process message
                    }

                    if (process.HasExited == false)
                    {
                        process.Kill(); //silently killing it
                    }
                }
                catch (Exception exception)
                {
                    Debug.WriteLine(exception.Message);
                }
            }

            Process.Start(new ProcessStartInfo(appExePath));
        }
    }
}