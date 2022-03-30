using ForbiddenWords.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace ForbiddenWords
{
    public partial class Form1 : Form
    {
        #region - Variables -
        private string ReportPath { get; set; } = @"D:\\ForbiddenWordsReport.txt";
        private string WordsPath { get; set; } = @"C:\Users\user\source\repos\ForbiddenWords\ForbiddenWords\Words.txt";
        private List<string> CmdPath { get; set; }
        private List<string> TruePath { get; set; } = new List<string>();

        private List<string> Words { get; set; }
        private StringBuilder reporter { get; set; } = new StringBuilder();

        private string FileName { get; set; } = string.Empty;
        private long FileSize { get; set; } = 0;
        private int CountOfChanges { get; set; } = 0;
        private int CountOfFiles { get; set; } = 0;
        public bool IsArgs { get; set; } = false;

        public Thread Finder;
        public Thread Writer;
        public Thread Reporter;
        #endregion

        public Form1(string[] args)
        {
            InitializeComponent();
            if (args.Length > 0)
            {
                CmdPath = args.ToList();
                IsArgs = true;
            }
            Thread LoadDataThread = new Thread(LoadData);
            LoadDataThread.Start();
        }

        public void FindFile(object obj)
        {
            string path = obj as string;
            try { label_Redaction.Text = "Search for files ..."; }
            catch (Exception) { }
            
            try
            {
                DirectoryInfo dir = new DirectoryInfo(path);
                foreach (FileInfo f in dir.GetFiles())
                {
                    if (f.Name.Contains(".txt"))
                    {
                        TruePath.Add(f.FullName);
                    }
                }

                foreach (DirectoryInfo d in dir.GetDirectories())
                {
                    if (d.Attributes.HasFlag(FileAttributes.Hidden) == false && d.Attributes.HasFlag(FileAttributes.System) == false)
                    {
                        FindFile($@"{path}\{d.Name}");
                    }
                }
            }
            catch (Exception e) { MessageBox.Show(e.Message); }
        }

        public void Func(object obj)
        {
            FindFile(obj);
            Thread.Sleep(2000);
            Writer.Start(TruePath);
        }

        public void FindWords(object obj)
        {
            try { label_Redaction.Text = "Editing a file:"; }
            catch (Exception) { }
            var collection = obj as List<string>;
            foreach (var item in collection)
            {
                if (File.Exists(item) && Path.GetExtension(item).Contains(".txt"))
                {
                    string path = item;
                    string data = Model.ReaderWriterLock.ReadData(path);

                    try { label_FileName.Text = Model.ReaderWriterLock.GetFileName(path); }
                    catch (Exception) { }

                    progressBar1.Invoke((MethodInvoker)delegate
                    {
                        progressBar1.Value = 0;
                    });
                    Thread.Sleep(1000);
                    progressBar1.Invoke((MethodInvoker)delegate
                    {
                        progressBar1.Value += new Random().Next(1, 40);
                    });

                    CountOfChanges = 0;

                    if (data.Length > 0)
                    {
                        data = Regex.Replace(data, string.Join("|", Words), m =>
                        {
                            CountOfChanges++;
                            return "*******";
                        });
                    }
                    Thread.Sleep(1000);
                    progressBar1.Invoke((MethodInvoker)delegate
                    {
                        progressBar1.Value += new Random().Next(1, 40);
                    });

                    string newPath = path.Split('.')[0];
                    string extension = path.Split('.')[1];
                    Model.ReaderWriterLock.WriteData(newPath + "CENSURED." + extension, data);

                    Thread.Sleep(1000);
                    progressBar1.Invoke((MethodInvoker)delegate
                    {
                        progressBar1.Value += new Random().Next(1, 40);
                    });

                    FileName = Model.ReaderWriterLock.GetFileName(path);
                    FileSize = Model.ReaderWriterLock.GetFileSize(path);
                    CountOfFiles++;

                    Thread.Sleep(1000);
                    if (progressBar1.Value < 100)
                    {
                        progressBar1.Invoke((MethodInvoker)delegate
                        {
                            progressBar1.Value = 100;
                        });
                    }
                    reporter.Append($"#{CountOfFiles}\nFile Name - {FileName}\nFile path - {path}\nFile Size = {FileSize}byte(s)\nNumber of substitutions = {CountOfChanges} pieces\n\n");
                    richTextBox1.Invoke((MethodInvoker)delegate
                    {
                        richTextBox1.Text = reporter.ToString();
                    });
                    Thread.Sleep(1000);
                }
            }
            Reporter.Start();
        }

        public void SaveReport()
        {
            reporter.Append($"\n\nTotal files: {CountOfFiles}");
            richTextBox1.Invoke((MethodInvoker)delegate
            {
                richTextBox1.Text = reporter.ToString();
            });
            Model.ReaderWriterLock.WriteData(ReportPath, reporter);
            MessageBox.Show($"{TruePath.Count} The files have been verified!", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public void ClearData()
        {
            FileName = string.Empty;
            FileSize = CountOfChanges = CountOfFiles = 0;
            label_FileName.Text = string.Empty;
            label_Redaction.Text = string.Empty;
            richTextBox1.Text = string.Empty;
            progressBar1.Value = 0;
            reporter = new StringBuilder();
            TruePath.Clear();
        }

        private void LoadData()
        {
            if (File.Exists(WordsPath))
            {
                var data = Model.ReaderWriterLock.ReadSplitData(WordsPath);
                Words = new List<string>();
                foreach (var item in data)
                {
                    Words.Add(item.Split('\r')[0]);
                }
            }
        }

        private void button_Start_Click(object sender, EventArgs e)
        {
            if (Finder == null && Writer == null && Reporter == null)
            {
                ClearData();
                Finder = new Thread(new ParameterizedThreadStart(Func));
                Writer = new Thread(new ParameterizedThreadStart(FindWords));
                Reporter = new Thread(SaveReport);

                if (IsArgs) Writer.Start(CmdPath);
                else
                {
                    if (textBox_Path.Text.Length > 0)
                    {
                        Finder.Start(textBox_Path.Text);
                    }
                    else MessageBox.Show("Enter the path!", "Attention", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else if (Finder.ThreadState == ThreadState.Unstarted || Finder.ThreadState == ThreadState.Stopped || Finder.ThreadState == ThreadState.Aborted)
            {
                if (Writer.ThreadState == ThreadState.Unstarted || Writer.ThreadState == ThreadState.Stopped || Writer.ThreadState == ThreadState.Aborted)
                {
                    if (Reporter.ThreadState == ThreadState.Unstarted || Reporter.ThreadState == ThreadState.Stopped || Reporter.ThreadState == ThreadState.Aborted)
                    {
                        ClearData();
                        Finder = new Thread(new ParameterizedThreadStart(Func));
                        Writer = new Thread(new ParameterizedThreadStart(FindWords));
                        Reporter = new Thread(SaveReport);

                        if (IsArgs) Writer.Start(CmdPath);
                        else
                        {
                            if (textBox_Path.Text.Length > 0)
                            {
                                Finder.Start(textBox_Path.Text);
                            }
                            else MessageBox.Show("Enter the path!", "Attention", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                    }
                    else MessageBox.Show("The search has already started!", "Attention", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else MessageBox.Show("The search has already started!", "Attention", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else MessageBox.Show("The search has already started!", "Attention", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void button_Stop_Click(object sender, EventArgs e)
        {
            try
            {
                if (Finder.ThreadState == ThreadState.WaitSleepJoin || Finder.ThreadState == ThreadState.Suspended)
                {
                    Finder.Abort();
                    ClearData();
                }
                if (Writer.ThreadState == ThreadState.WaitSleepJoin || Writer.ThreadState == ThreadState.Suspended)
                {
                    Writer.Abort();
                    ClearData();
                }
            }
            catch (Exception) { }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (Finder.ThreadState == ThreadState.WaitSleepJoin || Finder.ThreadState == ThreadState.Suspended)
                {
                    Finder.Abort();
                }
                if (Writer.ThreadState == ThreadState.WaitSleepJoin || Writer.ThreadState == ThreadState.Suspended)
                {
                    Writer.Abort();
                }
                if (Reporter.ThreadState == ThreadState.WaitSleepJoin || Reporter.ThreadState == ThreadState.Suspended)
                {
                    Reporter.Abort();
                }
            }
            catch (Exception) { }
        } 
    }
}
