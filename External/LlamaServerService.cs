using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace LocalRAGChatbotUI.Services
{
    public class LlamaServerService
    {
        public event Action<string>? OnLog;

        private Process? _process;

        public async Task StartServerAsync(string executablePath, string modelPath)
        {
            if (_process != null && !_process.HasExited)
                return;
            string folderPath = Path.Combine(
    AppDomain.CurrentDomain.BaseDirectory,
    "logs"
);
            string filePath = Path.Combine(folderPath, "app.log");


            Directory.CreateDirectory(folderPath);
            var WorkingDir = AppDomain.CurrentDomain.BaseDirectory;
            File.AppendAllText(filePath, WorkingDir+"\n");
            var parentDir = System.IO.Path.Combine(WorkingDir,"External");
            File.AppendAllText(filePath, parentDir + "\n");
            var model = System.IO.Path.Combine(parentDir, "Models",modelPath);
            var serverDir = System.IO.Path.Combine(parentDir, "Server");
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    Arguments = $"-m {model} ",
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = serverDir
                };

                _process = Process.Start(startInfo);

                OnLog?.Invoke($"Llama server started with model {modelPath} (PID {_process?.Id})");
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Failed to start Llama server: {ex.Message}");
            }

            await Task.CompletedTask;
        }
        public void StopServer()
        {
            try
            {
                if (_process != null && !_process.HasExited)
                {
                    _process.Kill();
                    _process.Dispose();
                    _process = null;
                    OnLog?.Invoke("Llama server stopped.");
                }
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Failed to stop Llama server: {ex.Message}");
            }
        }
    }
}
