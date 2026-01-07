using LocalRAGChatbotUI.Models;
using LocalRAGChatbotUI.Services;
using Microsoft.Win32;
using Microsoft.Win32;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.ComponentModel;
namespace LocalRAGChatbotUI
{
    public partial class MainWindow : Window , INotifyPropertyChanged
    {
        private readonly LlamaServerService _serverService = new();
        private readonly RagServerService _ragserverService = new();
        private readonly RagClient _ragClient = new();
        private readonly LlamaClient _llamaClient = new();
        private readonly Dispatcher _uiDispatcher;
        private bool rag = false;

        private bool _isIndexing;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        public bool IsIndexing
        {
            get => _isIndexing;
            set
            {
                _isIndexing = value;
                OnPropertyChanged(nameof(IsIndexing));
            }
        }
        public MainWindow()
        {

            InitializeComponent();
            _uiDispatcher = Dispatcher;
            DataContext = this;

            _ = _serverService.StartServerAsync("llama-server.exe","phi-3.5-mini-instruct.gguf");
            _ = _ragserverService.StartRagServerAsync("rag_server.exe","");
        }


        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            string prompt = PromptBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(prompt)) return;
            StringBuilder fullResponse = new StringBuilder();
            int counter = 0;
            AddMessage("You", prompt, true);
            PromptBox.Text = "";

            var (aiBlock, aiRun) = AddMessageWithRun("AI");
            string buffer = "";
            await foreach (var chunk in _llamaClient.StreamCompletion(prompt,rag))
            {
                counter++;
                buffer += chunk;
                if (counter % 4 == 0) {
                    _uiDispatcher.Invoke(() =>
                    {
                        aiRun.Text += buffer;
                        ChatScroll.ScrollToEnd();
                        buffer="";
                        
                    });
                }
                await Task.Yield();
                await Task.Delay(20);
            }
            _uiDispatcher.Invoke(() =>
            {
                aiRun.Text += buffer;
                ChatScroll.ScrollToEnd();
                buffer = "";

            });

        }
        private async void Rag_Click(object sender, RoutedEventArgs e)
        {
            if (rag)
            {
                rag = false;
                MessageBox.Show("RAG Mode Disabled!");
                return;
            }

            bool success = await _ragClient.Initialize();

            if (success)
            {
                rag = true;
                MessageBox.Show("RAG Mode Triggered!");
            }
            else
            {
                MessageBox.Show("Failed to initialize RAG server.");
            }
        }

        private void AddDocs_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select document to add",
                Filter = "PDF files (*.pdf)|*.pdf|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                Multiselect = false
            };

            if (dialog.ShowDialog() != true)
                return;

            string selectedPath = dialog.FileName;
            IsIndexing = true;
            rag = false;
            _ = Task.Run(async () =>
            {
                try
                {
                    await _ragClient.AddDocumentsAsync(selectedPath);

                    Dispatcher.Invoke(() =>
                    {
                        IsIndexing = false;
                        rag = true;
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        IsIndexing = false;
                        MessageBox.Show(ex.Message);
                    });
                }

                MessageBox.Show("Added Documents Successfully!");
            });
        }
        private (TextBlock block, Run run) AddMessageWithRun(string sender, bool isUser = false)
        {
            var run = new Run("");

            var block = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap
            };
            block.Inlines.Add(run);

            var border = new Border
            {
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(10),
                Margin = new Thickness(5),
                Background = isUser ? Brushes.LightBlue : Brushes.LightGray,
                MaxWidth = 400,
                HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                Child = block 
            };

            ChatStack.Children.Add(border);
            ChatScroll.ScrollToEnd();

            return (block, run);
        }

        private void AddMessage(string sender, string message, bool isUser = false)
        {
            var border = new Border
            {
                CornerRadius = new CornerRadius(12),  
                Padding = new Thickness(10),
                Margin = new Thickness(5),
                Background = isUser ? Brushes.LightBlue : Brushes.LightGray,
                MaxWidth = 400, 
                HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left
            };

            var textBlock = new TextBlock
            {
                Text = message,
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left
            };

            border.Child = textBlock;

            ChatStack.Children.Add(border);
            ChatScroll.ScrollToEnd();
        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            
            var result = MessageBox.Show(
                "Do you really want to exit?\nThe AI server will be closed.",
                "Confirm Exit",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.No)
            {
                e.Cancel = true; 
                return;
            }

            
            try
            {
                Task.Run(() => _serverService?.StopServer());
                Task.Run(() => _ragserverService?.StopServer());
            }
            catch { }
            base.OnClosing(e);
        }


    }
}