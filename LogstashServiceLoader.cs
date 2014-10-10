using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using Bottles.Services;
using System;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.Hosting;
using Microsoft.Owin.StaticFiles;
using Owin;

namespace LogstashService
{
    public class LogstashServiceLoader : IApplicationLoader, IDisposable
    {
        private Process _process;
        private IDisposable _kibanaApp;
        private bool _shouldBeStarted;

        public IDisposable Load()
        {
            _shouldBeStarted = true;

            var configuration = ConfigurationManager.AppSettings["Logstash.Configuration"];
            var logTo = ConfigurationManager.AppSettings["Logstash.Paths.Log"];

            if (!string.IsNullOrEmpty(logTo))
            {
                var logToDirectory = Path.GetDirectoryName(logTo);

                if (!string.IsNullOrEmpty(logToDirectory) && !Directory.Exists(logToDirectory))
                    Directory.CreateDirectory(logToDirectory);
            }

            var arguments = string.Format("agent -f {0}{1}", configuration, string.IsNullOrEmpty(logTo) ? "" : string.Format(" -l {0}", logTo));

            var location = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            using (var configFile = File.Open(string.Format("{0}\\Binaries\\bin\\elasticsearch.{1}.yml", location, configuration), FileMode.Open))
            using (var destinationConfig = File.Create(string.Format("{0}\\Binaries\\bin\\elasticsearch.yml", location)))
            {
                configFile.CopyTo(destinationConfig);
            }

            var startInfo = new ProcessStartInfo(string.Format("{0}\\Binaries\\bin\\logstash.bat", location), arguments)
            {
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = string.Format("{0}\\Binaries\\bin\\", location),
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true
            };

            _process = new Process
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true
            };

            _process.OutputDataReceived += (x, y) => Console.WriteLine(y.Data);

            _process.Exited += (x, y) => StartProcess();

            StartProcess();

            using (var configFile = File.Open(string.Format("{0}\\Kibana\\config.{1}.js", location, configuration), FileMode.Open))
            using (var destinationConfig = File.Create(string.Format("{0}\\Kibana\\config.js", location)))
            {
                configFile.CopyTo(destinationConfig);
            }

            var fileSystem = new PhysicalFileSystem(string.Format("{0}\\Kibana", location));

            var options = new FileServerOptions
            {
                FileSystem = fileSystem
            };

            options.StaticFileOptions.ContentTypeProvider = new CustomContentTypeProvider();

            var kibanaUrl = ConfigurationManager.AppSettings["Kibana.Url"];

            if (!string.IsNullOrEmpty(kibanaUrl))
            {
                _kibanaApp = WebApp.Start(kibanaUrl, x => x.UseFileServer(options));
            }

            return this;
        }

        public void Dispose()
        {
            _shouldBeStarted = false;

            if (_process != null)
                KillProcessAndChildren(_process.Id);

            if (_kibanaApp != null)
                _kibanaApp.Dispose();
        }

        private void StartProcess()
        {
            if (!_shouldBeStarted || !_process.Start())
                return;

            _process.BeginOutputReadLine();
        }

        private static void KillProcessAndChildren(int pid)
        {
            var searcher = new ManagementObjectSearcher("Select * From Win32_Process Where ParentProcessID=" + pid);
            var moc = searcher.Get();

            foreach (var mo in moc.Cast<ManagementObject>())
                KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));

            try
            {
                var proc = Process.GetProcessById(pid);
                proc.Kill();
            }
            catch (ArgumentException)
            { /* process already exited */ }
        }
    }
}