using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalRAGChatbotUI.Services
{
    internal class RagServerService
    {
        public event Action<string>? OnLog;

        private Process? _process;

        public async Task StartRagServerAsync(string executablePath, string modelPath)
        {
            if (_process != null && !_process.HasExited)
                return;

            var WorkingDir = AppDomain.CurrentDomain.BaseDirectory;
            Debug.Write("Opening Rag Server\n");
            var parentDir = System.IO.Path.Combine(WorkingDir, "External","RagServer");
            Debug.Write(parentDir);
            var serverDir = System.IO.Path.Combine(parentDir, "rag_server");
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = serverDir
                };

                _process = Process.Start(startInfo);

                OnLog?.Invoke($"Rag server started  (PID {_process?.Id})");
            }
            catch (Exception ex)
            {
                OnLog?.Invoke($"Failed to start Rag server: {ex.Message}");
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
