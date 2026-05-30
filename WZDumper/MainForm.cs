using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WzComparerR2.WzLib;
using WzDumper.WZDumper;

namespace WzDumper {
    public partial class MainForm : Form {
        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();
        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);
        [DllImport("kernel32.dll", EntryPoint = "CreateSymbolicLinkW", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);

        public MainForm() {
            InitializeComponent();
            ToolTip toolTip = new ToolTip();
            toolTip.SetToolTip(includePngMp3Box, "Extracts png and mp3 files along with the generated XML/JSON files");
            string linkTypeText = "Sets the method for creating link files\n" +
                "Note: Symbolic and Hard links cannot be created when extracting to a remote drive.\n" +
                "Methods:\n" +
                "Symbolic (recommended, requires admin privilage, default when running as admin)\n" +
                "Hard (default when not running as admin, falls back to Copy mode for files that have reached the link limit)\n" +
                "Copy (creates another copy entirely, use this if extracting to a remote drive)";
            toolTip.SetToolTip(LinkTypeLabel, linkTypeText);
            toolTip.SetToolTip(LinkTypeComboBox, linkTypeText);
            toolTip.SetToolTip(includeVersionInFolderBox, "Adds the file version to the end of the WZ folder (e.g. Base.wz_v81)");
            toolTip.SetToolTip(multiThreadCheckBox, "This only applies when dumping from a folder.\nDumps multiple WZ files concurrently depending on the number of extractor threads.");
            toolTip.SetToolTip(extractorThreadsNum, "Sets the max number of WZ files you want to extract at once.\nDefault is the number of processor threads available.");
            toolTip.SetToolTip(extractAsJson, "Extract as JSON files instead of XML");
            toolTip.ReshowDelay = 500;
            toolTip.ShowAlways = true;
            extractorThreadsNum.Maximum = Environment.ProcessorCount;
            extractorThreadsNum.Value = Environment.ProcessorCount;
        }

        public bool IsError { get; set; }
        public bool IsFinished { get; set; } = true;
        public bool Exit { get; set; }
        public TextBox InfoTextBox { get { return Info; } }
        public CancellationTokenSource CancelSource { get; set; }
        public bool ShouldExtractMP3PNG => includePngMp3Box.Checked;
        public LinkType SelectedLinkType { get; set; }

        private bool IsElevated {
            get {
                bool isElevated;
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent()) {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
                return isElevated;
            }
        }

        private void SelectWzFile(object sender, EventArgs e) {
            var openFile = new OpenFileDialog { Title = "Select File", Filter = "WZ Files|*.wz|WZ Image Files|*.img|MS Files|*.ms|MN Files|*.mn" };
            if (openFile.ShowDialog() == DialogResult.OK) {
                InputSelected(openFile.FileName);
            }
            openFile.Dispose();
        }

        private void InputSelected(string input) {
            WZFileTB.Text = input;
            versionBox.Text = String.Empty;
            toolStripStatusLabel1.Text = String.Empty;
            DumpWzButton.Enabled = WZFileTB.Text.Length > 0 && outputFolderTB.Text.Length > 0;
        }

        public void UpdateToolstripStatus(string status) {
            toolStripStatusLabel1.Text = status;
        }

        private static void UpdateTextBox(TextBox tb, string info, bool append) {
            if (append)
                tb.AppendText(info);
            else
                tb.Text = info;
        }

        private void DumpListWz(Wz_Structure file, string fName, string directory, DateTime startTime) {
            var error = false;
            TextWriter tw = new StreamWriter(directory + "\\List.txt");
            try {
                foreach (var listEntry in file.WzNode.Nodes) {
                    tw.WriteLine(listEntry.Text);
                }
            } catch (Exception e) {
                error = true;
                MessageBox.Show("An error occurred: " + e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                if (Directory.GetFiles(directory).Length == 0)
                    Directory.Delete(directory, true);
            } finally {
                tw.Dispose();
            }
            if (error) {
                UpdateTextBoxInfo(Info, "An error occurred while dumping " + fName, true);
                UpdateToolstripStatus("An error occurred while dumping " + fName);
            } else {
                var duration = DateTime.Now - startTime;
                UpdateTextBoxInfo(Info, "Finished dumping " + fName + " in " + GetDurationAsString(duration), true);
                UpdateToolstripStatus("Dumped " + fName + " successfully");
            }
        }

        private static string GetValidFolderName(string baseFolder, bool checkFileOnly) {
            var currFolder = baseFolder;
            var index = 1;
            if (checkFileOnly) {
                while (File.Exists(currFolder)) {
                    currFolder = baseFolder + "-" + index;
                    index++;
                }
            } else {
                while (Directory.Exists(currFolder) || File.Exists(currFolder)) {
                    currFolder = baseFolder + "-" + index;
                    index++;
                }
            }
            return currFolder;
        }

        private void DumpFile(object sender, EventArgs e) {
            var filePath = WZFileTB.Text;
            if (!Directory.Exists(filePath) && !File.Exists(filePath)) {
                MessageBox.Show("Unable to access file", "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }
            CheckOutputPath();
            UpdateToolstripStatus("Parsing...");
            DisableButtons();
            FileAttributes attr = File.GetAttributes(filePath);
            if (!attr.HasFlag(FileAttributes.Directory)) {
                var extension = Path.GetExtension(filePath);
                if (extension != null && String.Compare(extension, ".img", StringComparison.OrdinalIgnoreCase) == 0) {
                    DumpFromWzImage(filePath);
                    return;
                }
                Wz_Structure structure = new Wz_Structure();
                try {
                    string[] msFileExtensions = { ".ms", ".mn" };
                    if (msFileExtensions.Any(ext => string.Equals(Path.GetExtension(filePath), ext, StringComparison.OrdinalIgnoreCase))) {
                        structure.LoadMsFile(filePath);
                    } else if (structure.IsKMST1125WzFormat(filePath)) {
                        structure.LoadKMST1125DataWz(filePath);
                        string packsDir = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(filePath)), "Packs");
                        string wzFileName = Path.GetFileNameWithoutExtension(filePath);
                        if (Directory.Exists(packsDir)) {
                            foreach (var ext in msFileExtensions) {
                                foreach (var msFile in Directory.GetFiles(packsDir, $"{wzFileName}*{ext}")) {
                                    structure.LoadMsFile(msFile);
                                }
                            }
                        }
                    } else {
                        structure.Load(filePath, false);
                    }
                } catch (UnauthorizedAccessException) {
                    structure.Clear();
                    MessageBox.Show("Please re-run this program as an administrator.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    UpdateToolstripStatus("");
                    EnableButtons();
                    return;
                } catch (Exception ex) {
                    structure.Clear();
                    MessageBox.Show("An error occurred while parsing this file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    //Info.AppendText(ex.StackTrace);
                    UpdateToolstripStatus("");
                    EnableButtons();
                    return;
                }
                var isListWz = structure.encryption.UseListWz;
                if (structure.encryption.KnownProfiles.Count > 0) {
                    versionBox.Text = structure.encryption.KnownProfiles[0].WzVersion.ToString();
                }
                if (structure.encryption.Pkg1EncType != Wz_CryptoKeyType.Unknown) {
                    var encType = structure.encryption.Pkg1EncType.ToString();
                    if (encType.Equals("BMS"))
                        encType = "None";
                    EncryptionType.Text = encType;
                }
                var fileName = Path.GetFileName(filePath);
                var extractDir = outputFolderTB.Text;
                var extractFolder = Path.Combine(extractDir, fileName);
                if (!isListWz && includeVersionInFolderBox.Checked && structure.encryption.KnownProfiles.Count > 0)
                    extractFolder += "_v" + structure.encryption.KnownProfiles[0].WzVersion;
                if (File.Exists(extractFolder)) {
                    extractFolder = GetValidFolderName(extractFolder, true);
                }
                if (Directory.Exists(extractFolder)) {
                    var result = MessageBox.Show(extractFolder + " already exists.\r\nDo you want to overwrite that folder?\r\nNote: Clicking No will make a new folder.", "Folder Already Exists", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                    if (result == DialogResult.Cancel) {
                        structure.Clear();
                        UpdateToolstripStatus("");
                        EnableButtons();
                        return;
                    }
                    if (result != DialogResult.Yes) {
                        extractFolder = GetValidFolderName(extractFolder, false);
                    }
                }
                if (!Directory.Exists(extractFolder))
                    Directory.CreateDirectory(extractFolder);
                if (isListWz) {
                    Info.AppendText("Dumping data from " + fileName + " to " + extractFolder + "...\r\n");
                } else if (includePngMp3Box.Checked) {
                    Info.AppendText("Dumping MP3s, PNGs and " + GetDumpFormat() + "s from " + fileName + " to " + extractFolder + "...\r\n");
                } else {
                    Info.AppendText("Dumping " + GetDumpFormat() + "s from " + fileName + " to " + extractFolder + "...\r\n");
                }
                if (isListWz) {
                    DumpListWz(structure, fileName, extractFolder, DateTime.Now);
                    structure.Clear();
                    EnableButtons();
                } else {
                    UpdateToolstripStatus("Preparing...");
                    CancelSource = new CancellationTokenSource();
                    var extractor = extractAsJson.Checked ? (WzExtractor)new WzJsonExtractor(this, extractDir, new DirectoryInfo(extractFolder).Name, includePngMp3Box.Checked, SelectedLinkType) : new WzXmlExtractor(this, extractDir, new DirectoryInfo(extractFolder).Name, includePngMp3Box.Checked, SelectedLinkType);
                    CreateSingleDumperThread(structure, extractor, fileName);
                }
            } else {
                string filesFound = "WZ Files Found: ";
                var allFiles = Directory.GetFiles(filePath, "*.wz");
                var nextLevelFiles = GetWzFilesInFolder(filePath);
                if (allFiles.Length != 0 || nextLevelFiles.Count != 0) {
                    if (allFiles.Length == 0)
                        allFiles = nextLevelFiles.ToArray();
                    allFiles = allFiles.Where(fileName => !Regex.IsMatch(fileName, "[0-9]{3}.wz$", RegexOptions.IgnoreCase)).ToArray();
                    foreach (var fileName in allFiles) {
                        filesFound += Path.GetFileName(fileName) + ", ";
                    }
                    Info.AppendText(filesFound.Substring(0, filesFound.Length - 2) + "\r\n");
                    if (allFiles.Length != 0 && nextLevelFiles.Count == 0) {
                        Array.Sort(allFiles, FileSizeCompare);
                    } else {
                        SortedDictionary<long, string> fileOrder = new SortedDictionary<long, string>();
                        foreach (var fileName in allFiles) {
                            FileInfo wzFile = new FileInfo(fileName);
                            fileOrder.Add(DirSize(wzFile.Directory), fileName);
                        }
                        allFiles = fileOrder.Values.ToArray();
                    }
                    CreateMultipleDumperThreads(filePath, allFiles, outputFolderTB.Text);
                } else {
                    MessageBox.Show("There are no WZ Files located in the selected folder. Please choose a different folder.", "No WZ Files Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    UpdateToolstripStatus("");
                    EnableButtons();
                }
            }
        }

        private static long DirSize(DirectoryInfo dirInfo) {
            long size = 0;
            FileInfo[] fis = dirInfo.GetFiles();
            foreach (FileInfo fi in fis) {
                size += fi.Length;
            }
            DirectoryInfo[] dis = dirInfo.GetDirectories();
            foreach (DirectoryInfo di in dis) {
                size += DirSize(di);
            }
            return size;
        }

        private void DumpFromWzImage(string path) {
            var fName = Path.GetFileName(path);
            if (fName == null)
                return;
            Wz_Structure structure = new Wz_Structure();
            try {
                structure.LoadImg(path);
            } catch (IOException ex) {
                structure.Clear();
                MessageBox.Show("Unable to read file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            } catch (ArgumentException) {
                structure.Clear();
                MessageBox.Show("Please select a WZ Image File.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            } catch (UnauthorizedAccessException) {
                structure.Clear();
                MessageBox.Show("Please re-run this program as an administrator to be able to dump files that are not in the OS drive.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            } catch (Exception) {
                structure.Clear();
                MessageBox.Show("Please select a valid WZ Image File.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var open = new FolderBrowserDialog();
            if (open.ShowDialog() != DialogResult.OK) {
                structure.Clear();
                open.Dispose();
                return;
            }
            var extractFolder = Path.Combine(open.SelectedPath, fName);
            if (File.Exists(extractFolder)) {
                extractFolder = GetValidFolderName(extractFolder, true);
            }
            if (Directory.Exists(extractFolder)) {
                var result = MessageBox.Show(extractFolder + " already exists.\r\nDo you want to overwrite that folder?\r\nNote: Clicking No will make a new folder.", "Folder Already Exists", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                if (result == DialogResult.Cancel)
                    return;
                if (result != DialogResult.Yes) {
                    extractFolder = GetValidFolderName(Path.Combine(open.SelectedPath, fName), false);
                }
            }

            if (!Directory.Exists(extractFolder))
                Directory.CreateDirectory(extractFolder);
            DisableButtons();
            Info.AppendText("Dumping data from " + fName + " to " + extractFolder + "...\r\n");
            var startTime = DateTime.Now;
            CancelSource = new CancellationTokenSource();
            string startingPath = new DirectoryInfo(extractFolder).Name;
            if (extractAsJson.Checked)
                new WzJsonExtractor(this, open.SelectedPath, startingPath, includePngMp3Box.Checked, SelectedLinkType).DumpImage(structure.WzNode.GetNodeWzImage(), startingPath);
            else
                new WzXmlExtractor(this, open.SelectedPath, startingPath, includePngMp3Box.Checked, SelectedLinkType).DumpImage(structure.WzNode.GetNodeWzImage(), startingPath);
            open.Dispose();
            structure.Clear();
            CancelSource.Dispose();
            var duration = DateTime.Now - startTime;
            UpdateTextBoxInfo(Info, "Finished dumping " + fName + " in " + GetDurationAsString(duration), true);
            UpdateToolstripStatus("Dumped " + fName + " successfully");
            EnableButtons();
        }

        private void CreateSingleDumperThread(Wz_Structure file, WzExtractor extractor, string fileName) {
            IsFinished = false;
            var startTime = DateTime.Now;
            var mainTask = Task.Factory.StartNew(() => DirectoryDumperThread(file, extractor, true));
            mainTask.ContinueWith(p => {
                var duration = DateTime.Now - startTime;
                string message = String.Empty;
                if (CancelSource.Token.IsCancellationRequested) {
                    if (Exit)
                        return;
                    message = "Canceled dumping " + fileName;
                } else if (IsError) {
                    message = "An error occurred while dumping " + fileName;
                } else {
                    message = "Finished dumping " + fileName;
                }
                UpdateToolstripStatus(message);
                UpdateTextBoxInfo(Info, message + ".\r\nElapsed time: " + GetDurationAsString(duration), true);
                IsError = false;
                IsFinished = true;
                EnableButtons();
                CancelSource.Dispose();
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void InitThread(string fileName, string dumpFolder) {
            var message = String.Empty;
            Wz_Structure structure = new Wz_Structure();
            try {
                string[] msFileExtensions = { ".ms", ".mn" };
                if (msFileExtensions.Any(ext => string.Equals(Path.GetExtension(fileName), ext, StringComparison.OrdinalIgnoreCase))) {
                    structure.LoadMsFile(fileName);
                } else if (structure.IsKMST1125WzFormat(fileName)) {
                    structure.LoadKMST1125DataWz(fileName);
                    string packsDir = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(fileName)), "Packs");
                    string wzFileName = Path.GetFileNameWithoutExtension(fileName);
                    if (Directory.Exists(packsDir)) {
                        foreach (var ext in msFileExtensions) {
                            foreach (var msFile in Directory.GetFiles(packsDir, $"{wzFileName}*{ext}")) {
                                structure.LoadMsFile(msFile);
                            }
                        }
                    }
                } else {
                    structure.Load(fileName, false);
                }
            } catch (IOException ex) {
                structure.Clear();
                message = "An IO error occurred: " + ex.Message;
            } catch (UnauthorizedAccessException) {
                structure.Clear();
                message = "Please re-run this program as an administrator.";
            } catch (Exception ex) {
                structure.Clear();
                message = "An error occurred while parsing this file: " + ex.Message;
            }
            if (!String.IsNullOrEmpty(message)) {
                UpdateTextBoxInfo(Info, "Error while parsing file " + Path.GetFileName(fileName) + "\r\nMessage: " + message + "\r\nContinuing...", true);
                if (!fileName.EndsWith("List.wz"))
                    IsError = true;
                return;
            }
            var wzName = Path.GetFileName(fileName);
            var nFolder = Path.Combine(dumpFolder, wzName);
            var isListWz = structure.encryption.UseListWz;
            if (!isListWz && includeVersionInFolderBox.Checked)
                nFolder += "_v" + structure.encryption.KnownProfiles[0].WzVersion.ToString();
            nFolder = GetValidFolderName(nFolder, false);
            if (!Directory.Exists(nFolder))
                Directory.CreateDirectory(nFolder);
            if (!isListWz) {
                var encType = structure.encryption.Pkg1EncType.ToString();
                if (encType.Equals("BMS"))
                    encType = "None";
                EncryptionType.Text = encType;
                UpdateTextBoxInfo(EncryptionType, encType, false);
                UpdateTextBoxInfo(versionBox, structure.encryption.KnownProfiles[0].WzVersion.ToString(), false);
            }
            if (isListWz) {
                UpdateTextBoxInfo(Info, "Dumping data from " + wzName + " to " + nFolder + "...", true);
            } else if (includePngMp3Box.Checked) {
                UpdateTextBoxInfo(Info, "Dumping MP3s, PNGs and " + GetDumpFormat() + "s from " + wzName + " to " + nFolder + "...", true);
            } else {
                UpdateTextBoxInfo(Info, "Dumping " + GetDumpFormat() + "s from " + wzName + " to " + nFolder + "...", true);
            }
            if (isListWz) {
                DumpListWz(structure, wzName, nFolder, DateTime.Now);
                structure.Clear();
            } else {
                var extractor = extractAsJson.Checked ? (WzExtractor)new WzJsonExtractor(this, dumpFolder, new DirectoryInfo(nFolder).Name, includePngMp3Box.Checked, SelectedLinkType) : new WzXmlExtractor(this, dumpFolder, new DirectoryInfo(nFolder).Name, includePngMp3Box.Checked, SelectedLinkType);
                DirectoryDumperThread(structure, extractor);
            }
        }

        private string GetDumpFormat() {
            return extractAsJson.Checked ? "JSON" : "XML";
        }

        private void CreateMultipleDumperThreads(string wzFolder, IEnumerable<string> files, string dumpFolder) {
            IsFinished = false;
            var startTime = DateTime.Now;
            CancelSource = new CancellationTokenSource();
            var t = Task.Factory.StartNew(() => {
                var pOps = new ParallelOptions { MaxDegreeOfParallelism = multiThreadCheckBox.Checked ? Math.Min(((string[])files).Length, (int)extractorThreadsNum.Value) : 1 };
                Parallel.ForEach(files, pOps, file => {
                    if (CancelSource.Token.IsCancellationRequested)
                        return;
                    InitThread(file, dumpFolder);
                });
            });
            t.ContinueWith(p => {
                var duration = DateTime.Now - startTime;
                if (CancelSource.Token.IsCancellationRequested) {
                    if (Exit)
                        return;
                    UpdateTextBoxInfo(Info, "Canceled dumping WZ Files. Elapsed time: " + GetDurationAsString(duration), true);
                    UpdateToolstripStatus("Dumping WZ Files canceled");
                } else if (IsError) {
                    UpdateTextBoxInfo(Info, "An error occurred while dumping WZ Files. Elapsed time: " + GetDurationAsString(duration), true);
                    UpdateToolstripStatus("An error occurred while dumping WZ Files");
                } else {
                    UpdateTextBoxInfo(Info, "Finished dumping all WZ Files in " + wzFolder + " in " + GetDurationAsString(duration), true);
                    UpdateToolstripStatus("Dumped all WZ Files successfully");
                }
                IsError = false;
                IsFinished = true;
                EnableButtons();
                CancelSource.Dispose();
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void DirectoryDumperThread(Wz_Structure dir, WzExtractor extractor, bool singleDump = false) {
            if (CancelSource.Token.IsCancellationRequested)
                return;
            try {
                extractor.DumpDir(dir);
                if (!singleDump && !CancelSource.Token.IsCancellationRequested)
                    UpdateTextBoxInfo(Info, "Finished dumping " + dir.WzNode.Text, true);
            } catch (Exception ex) {
                if (!CancelSource.Token.IsCancellationRequested) {
                    UpdateTextBoxInfo(Info, dir.WzNode.Text + " Exception: " + ex.Message + " " + ex.StackTrace, true);
                    IsError = true;
                }
            } finally {
                dir.Clear();
            }
        }

        private static string GetDurationAsString(TimeSpan duration) {
            string s = String.Empty;
            if (duration.Hours != 0) {
                s += duration.Hours + " hour";
                if (duration.Hours != 1)
                    s += "s";
            }
            if (duration.Minutes != 0) {
                if (!string.IsNullOrEmpty(s))
                    s += ", ";
                s += duration.Minutes + " minute";
                if (duration.Minutes != 1)
                    s += "s";
                s += ", ";
            }
            s += duration.Seconds + " second";
            if (duration.Seconds != 1)
                s += "s";
            s += " and ";
            s += duration.Milliseconds + " millisecond";
            if (duration.Milliseconds != 1)
                s += "s";
            return s;
        }

        private void CancelOperation(object sender, EventArgs e) {
            if (MessageBox.Show("Are you sure you want to cancel the current operation?", "Cancel", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;
            CancelSource?.Cancel(true);
            CancelOpButton.Enabled = false;
            UpdateTextBoxInfo(Info, "Canceling... Waiting for the current image(s) to finish dumping...", true);
        }

        private void Form1Load(object sender, EventArgs e) {
            var args = Environment.GetCommandLineArgs();
            for (int i = 1; i < args.Length; i++) {
                string arg = args[i];
                if (arg.Equals("-a")) {
                    includePngMp3Box.Checked = true;
                } else if (arg.Equals("-o")) {
                    if (i + 1 < args.Length) {
                        string output = args[++i];
                        if (Directory.Exists(output))
                            outputFolderTB.Text = output;
                    }
                } else {
                    if (!arg.Equals("") && (arg.Contains("wz") || Directory.Exists(arg))) {
                        InputSelected(arg);
                    }
                }
            }
            LinkTypeComboBox.DataSource = Enum.GetValues(typeof(LinkType));
            LinkTypeComboBox.SelectedItem = IsElevated ? LinkType.Symbolic : LinkType.Hard;
        }

        private void Form1FormClosing(object sender, FormClosingEventArgs e) {
            if (IsFinished)
                return;
            if (MessageBox.Show("You can not close the program while it is still dumping. Do you wish to cancel the operation?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) {
                Exit = true;
                CancelSource?.Cancel(true);
            } else {
                e.Cancel = true;
            }
        }

        private void ClearInfoToolStripMenuItemClick(object sender, EventArgs e) {
            Info.Text = String.Empty;
            toolStripStatusLabel1.Text = String.Empty;
        }

        private void OpenFolder(object sender, EventArgs e) {
            Process.Start("explorer.exe", outputFolderTB.Text);
        }

        private void ExitToolStripMenuItemClick(object sender, EventArgs e) {
            Close();
        }

        private static Form IsFormAlreadyOpen(Type formType) {
            return Application.OpenForms.Cast<Form>().FirstOrDefault(openForm => openForm.GetType() == formType);
        }

        private void AboutToolStripMenuItem1Click(object sender, EventArgs e) {
            Form taskFormName;
            if ((taskFormName = IsFormAlreadyOpen(typeof(About))) == null) {
                taskFormName = new About();
                taskFormName.Show();
            } else {
                taskFormName.WindowState = FormWindowState.Normal;
                taskFormName.BringToFront();
            }
        }

        private void EnableButtons() {
            SelectWzFileButton.Enabled = true;
            SelectWzFolder.Enabled = true;
            SelectExtractDestination.Enabled = true;
            LinkTypeComboBox.Enabled = includePngMp3Box.Checked;
            DumpWzButton.Enabled = true;
            CancelOpButton.Enabled = false;
            includePngMp3Box.Enabled = true;
            includeVersionInFolderBox.Enabled = true;
            multiThreadCheckBox.Enabled = true;
            extractAsJson.Enabled = true;
            if (!string.IsNullOrEmpty(outputFolderTB.Text))
                openFolderButton.Focus();
            else
                SelectWzFileButton.Focus();
            extractorThreadsLabel.Enabled = multiThreadCheckBox.Checked;
            extractorThreadsNum.Enabled = multiThreadCheckBox.Checked;
        }

        private void DisableButtons() {
            SelectWzFileButton.Enabled = false;
            SelectWzFolder.Enabled = false;
            SelectExtractDestination.Enabled = false;
            LinkTypeComboBox.Enabled = false;
            DumpWzButton.Enabled = false;
            CancelOpButton.Enabled = true;
            includePngMp3Box.Enabled = false;
            includeVersionInFolderBox.Enabled = false;
            multiThreadCheckBox.Enabled = false;
            extractAsJson.Enabled = false;
            extractorThreadsLabel.Enabled = false;
            extractorThreadsNum.Enabled = false;
            Info.Focus();
        }

        private static int FileSizeCompare(string x, string y) {
            var file1 = new FileInfo(x);
            var file2 = new FileInfo(y);
            return Convert.ToInt32(file1.Length - file2.Length);
        }

        public void UpdateTextBoxInfo(TextBox tb, string info, bool appendNewLine) {
            if (appendNewLine)
                info += "\r\n";
            if (tb.InvokeRequired && tb.IsHandleCreated) {
                Invoke(new UpdateTextBoxDelegate(UpdateTextBox), new object[] { tb, info, appendNewLine });
            } else {
                UpdateTextBox(tb, info, appendNewLine);
            }
        }


        #region Nested type: UpdateTextBoxDelegate

        private delegate void UpdateTextBoxDelegate(TextBox tb, string info, bool append);

        #endregion

        private void MultiThreadCheckBox_CheckedChanged(object sender, EventArgs e) {
            extractorThreadsLabel.Enabled = multiThreadCheckBox.Checked;
            extractorThreadsNum.Enabled = multiThreadCheckBox.Checked;
        }

        private void CheckOutputPath() {
            if (outputFolderTB.Text.Length != 0) {
                if (includePngMp3Box.Checked && !LinkTypeComboBox.SelectedItem.Equals(LinkType.Copy)) {
                    String testFile = Path.Combine(outputFolderTB.Text, "test", "file");
                    String testFile2 = Path.Combine(outputFolderTB.Text, "test", "link");
                    FileInfo fi = new FileInfo(testFile);
                    fi.Directory.Create();
                    fi.Create().Close();
                    bool res = LinkTypeComboBox.SelectedItem.Equals(LinkType.Symbolic) ? CreateSymbolicLink(testFile2, testFile, 0) : CreateHardLink(testFile2, testFile, IntPtr.Zero);
                    if (!res) {
                        MessageBox.Show("A test link could not be created on the output drive. The Link Type will be changed to Copy.", "Unable to Create Test Link", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        LinkTypeComboBox.SelectedItem = LinkType.Copy;
                    }
                    fi.Directory.Delete(true);
                }
            }
        }

        private void IncludePngMp3Box_CheckedChanged(object sender, EventArgs e) {
            LinkTypeComboBox.Enabled = includePngMp3Box.Checked;
            CheckOutputPath();
        }

        public List<String> GetWzFilesInFolder(String path) {
            List<String> wzFiles = new List<String>();
            string[] dirs = Directory.GetDirectories(path);
            foreach (var dir in dirs) {
                wzFiles.AddRange(Directory.GetFiles(dir, "*.wz"));
            }
            return wzFiles;
        }

        private void SelectWzFolder_Click(object sender, EventArgs e) {
            var open = new FolderBrowserDialog { Description = "Select the folder that contains the WZ Files you wish to dump" };
            if (open.ShowDialog() == DialogResult.OK) {
                var allFiles = Directory.GetFiles(open.SelectedPath, "*.wz");
                if (allFiles.Length == 0 && GetWzFilesInFolder(open.SelectedPath).Count == 0) {
                    MessageBox.Show("There are no WZ Files located in the selected folder. Please choose a different folder.", "No WZ Files Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                } else {
                    InputSelected(open.SelectedPath);
                }
            }
            open.Dispose();
        }

        private void SelectExtractDestination_Click(object sender, EventArgs e) {
            var open = new FolderBrowserDialog { Description = "Select the folder you wish to dump to" };
            if (open.ShowDialog() == DialogResult.OK) {
                outputFolderTB.Text = open.SelectedPath;
                DumpWzButton.Enabled = WZFileTB.Text.Length > 0 && outputFolderTB.Text.Length > 0;
                openFolderButton.Enabled = true;
                CheckOutputPath();
            }
            open.Dispose();

        }

        private void LinkTypeComboBox_SelectedIndexChanged(object sender, EventArgs e) {
            if (LinkTypeComboBox.SelectedItem.Equals(LinkType.Symbolic) && !IsElevated) {
                var result = MessageBox.Show("Creating symbolic links require administrative permission. Do you want to restart this program as an administrator?", "Admin Privileges Required", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (result == DialogResult.Yes) {
                    try {
                        ProcessStartInfo processInfo = new ProcessStartInfo {
                            Verb = "runas",
                            FileName = Assembly.GetExecutingAssembly().Location,
                            UseShellExecute = true,
                            Arguments = "\"" + WZFileTB.Text + "\" -o \"" + outputFolderTB.Text + "\"" + (includePngMp3Box.Checked ? " -a" : "")
                        };
                        Process.Start(processInfo);
                        Close();
                    } catch (Exception) { }
                } else {
                    LinkTypeComboBox.SelectedItem = SelectedLinkType;
                }
            }
            SelectedLinkType = (LinkType)LinkTypeComboBox.SelectedItem;
        }

        private void LinkTypeComboBox_KeyPress(object sender, KeyPressEventArgs e) {
            e.Handled = true;
        }
    }

    public enum LinkType : int {
        Hard = 1,
        Symbolic = 2,
        Copy = 3
    }
}