using MapleLib.WzLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WzDumper {
    public partial class MainForm : Form {
        public MainForm() {
            InitializeComponent();
            ToolTip toolTip = new ToolTip();
            toolTip.SetToolTip(includePngMp3Box, "Extracts png and mp3 files along with the generated XML files");
            string linkTypeText = "Sets the method to handle file links.\n" +
                "Note: Symbolic and Hard links cannot be created when extracting to a remote drive.\n" +
                "Methods:\n" +
                "Symbolic (recommended, requires admin privilage, default when running as admin)\n" +
                "Hard (default when not running as admin, falls back to Copy mode for files that have reached the link limit)\n" +
                "Copy (creates another copy entirely, previous behavior, use this if extracting to a remote drive)";
            toolTip.SetToolTip(LinkTypeLabel, linkTypeText);
            toolTip.SetToolTip(LinkTypeComboBox, linkTypeText);
            toolTip.SetToolTip(includeVersionInFolderBox, "Adds the file version to the end of the WZ folder (e.g. Base.wz_v81)");
            toolTip.SetToolTip(multiThreadCheckBox, "This only applies when dumping from a folder.\nDumps multiple WZ files concurrently depending on the number of extractor threads.");
            toolTip.SetToolTip(extractorThreadsNum, "Sets the max number of WZ files you want to extract at once.\nDefault is the number of processor threads available.");
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

        private WzMapleVersion SelectedVersion {
            get {
                switch (MapleVersionComboBox.SelectedIndex) {
                    case 1:
                        return WzMapleVersion.GMS;
                    case 2:
                        return WzMapleVersion.EMS;
                    default:
                        return WzMapleVersion.CLASSIC;
                }
            }
        }

        private void SelectWzFile(object sender, EventArgs e) {
            var openFile = new OpenFileDialog { Title = "Select File", Filter = "WZ Files|*.wz|WZ Image Files|*.img" };
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

        private void DumpListWz(WzListFile file, string fName, string directory, DateTime startTime) {
            var error = false;
            TextWriter tw = new StreamWriter(directory + "\\List.txt");
            try {
                foreach (var listEntry in file.WzListEntries) {
                    tw.WriteLine(listEntry);
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

        private void getWzExtensionFiles(string path, List<WzFile> fileList) {
            FileInfo wzFileInfo = new FileInfo(path);
            string selFileName = Path.GetFileNameWithoutExtension(wzFileInfo.Name);
            var extFiles = Directory.GetFiles(wzFileInfo.DirectoryName, selFileName + "???.wz");
            foreach (string extFile in extFiles) {
                if (Regex.IsMatch(extFile, selFileName + "[0-9]{3}.wz$", RegexOptions.IgnoreCase))
                    fileList.Add(new WzFile(extFile, SelectedVersion));
            }
        }

        private void DumpFile(object sender, EventArgs e) {
            UpdateToolstripStatus("Parsing...");
            DisableButtons();
            var filePath = WZFileTB.Text;
            FileAttributes attr = File.GetAttributes(filePath);
            if (!attr.HasFlag(FileAttributes.Directory)) {
                var ext = Path.GetExtension(filePath);
                if (ext != null && String.Compare(ext, ".img", StringComparison.OrdinalIgnoreCase) == 0) {
                    DumpXmlFromWzImage(filePath);
                    return;
                }
                WzListFile listFile = null;
                WzFile regFile = null;
                try {
                    if (filePath.EndsWith("List.wz", StringComparison.CurrentCultureIgnoreCase)) {
                        listFile = new WzListFile(filePath, SelectedVersion);
                        listFile.ParseWzFile();
                    } else {
                        List<WzFile> s = new List<WzFile>();
                        getWzExtensionFiles(filePath, s);
                        regFile = new WzFile(filePath, SelectedVersion, s);
                        regFile.ParseWzFile();
                    }
                } catch (UnauthorizedAccessException) {
                    if (regFile != null) regFile.Dispose();
                    MessageBox.Show("Please re-run this program as an administrator.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    UpdateToolstripStatus("");
                    EnableButtons();
                    return;
                } catch (Exception ex) {
                    if (regFile != null) regFile.Dispose();
                    MessageBox.Show("An error occurred while parsing this file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    UpdateToolstripStatus("");
                    EnableButtons();
                    return;
                }
                if (listFile == null)
                    versionBox.Text = regFile.Version.ToString(CultureInfo.CurrentCulture);
                var fileName = Path.GetFileName(filePath);
                var extractDir = outputFolderTB.Text;
                var extractFolder = Path.Combine(extractDir, fileName);
                if (listFile == null && includeVersionInFolderBox.Checked)
                    extractFolder += "_v" + regFile.Version;
                if (File.Exists(extractFolder)) {
                    extractFolder = GetValidFolderName(extractFolder, true);
                }
                /*new Uri(extractFolder).IsUnc;
                var dir = new DirectoryInfo(extractFolder);
                var drive = new DriveInfo(dir.Root.ToString());*/
                if (Directory.Exists(extractFolder)) {
                    var result = MessageBox.Show(extractFolder + " already exists.\r\nDo you want to overwrite that folder?\r\nNote: Clicking No will make a new folder.", "Folder Already Exists", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                    if (result == DialogResult.Cancel) {
                        if (regFile != null) regFile.Dispose();
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
                if (listFile != null) {
                    Info.AppendText("Dumping data from " + fileName + " to " + extractFolder + "...\r\n");
                } else if (includePngMp3Box.Checked) {
                    Info.AppendText("Dumping MP3s, PNGs and XMLs from " + fileName + " to " + extractFolder + "...\r\n");
                } else {
                    Info.AppendText("Dumping XMLs from " + fileName + " to " + extractFolder + "...\r\n");
                }
                if (listFile != null) {
                    DumpListWz(listFile, fileName, extractFolder, DateTime.Now);
                    listFile.Dispose();
                    EnableButtons();
                } else {
                    UpdateToolstripStatus("Preparing...");
                    CancelSource = new CancellationTokenSource();
                    CreateSingleDumperThread(regFile, new WzXml(this, extractDir, new DirectoryInfo(extractFolder).Name, includePngMp3Box.Checked, (LinkType)LinkTypeComboBox.SelectedItem), fileName);
                }
            } else {
                var allFiles = Directory.GetFiles(filePath, "*.wz");
                if (allFiles.Length != 0) {
                    string filesFound = "WZ Files Found: ";
                    foreach (var fileName in allFiles) {
                        filesFound += Path.GetFileName(fileName) + ", ";
                    }
                    Info.AppendText(filesFound.Substring(0, filesFound.Length - 2) + "\r\n");
                    allFiles = allFiles.Where(fileName => !Regex.IsMatch(fileName, "[0-9]{3}.wz$", RegexOptions.IgnoreCase)).ToArray(); ;
                    Array.Sort(allFiles, Compare);
                    CreateMultipleDumperThreads(filePath, allFiles, outputFolderTB.Text);
                } else {
                    MessageBox.Show("There are no WZ Files located in the selected folder. Please choose a different folder.", "No WZ Files Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    UpdateToolstripStatus("");
                    EnableButtons();
                }
            }
        }

        private void DumpXmlFromWzImage(string path) {
            var fName = Path.GetFileName(path);
            if (fName == null)
                return;
            FileStream fStream;
            WzImage img;
            try {
                fStream = File.Open(path, FileMode.Open);
                img = new WzImage(fName, fStream, SelectedVersion);
                img.ParseImage();
            } catch (IOException ex) {
                MessageBox.Show("Unable to read file: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            } catch (ArgumentException) {
                MessageBox.Show("Please select a WZ Image File.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            } catch (UnauthorizedAccessException) {
                MessageBox.Show("Please re-run this program as an administrator to be able to dump files that are not in the OS drive.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            } catch (Exception) {
                MessageBox.Show("Please select a valid WZ Image File.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (img.WzProperties.Count() == 0) {
                img.Dispose();
                fStream.Dispose();
                MessageBox.Show("This image file contained no data when parsing. Please select a different Maple Version.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var open = new FolderBrowserDialog();
            if (open.ShowDialog() != DialogResult.OK) {
                img.Dispose();
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
            new WzXml(this, open.SelectedPath, startingPath, includePngMp3Box.Checked, (LinkType)LinkTypeComboBox.SelectedItem).DumpImage(img, startingPath);
            open.Dispose();
            img.Dispose();
            fStream.Dispose();
            CancelSource.Dispose();
            var duration = DateTime.Now - startTime;
            UpdateTextBoxInfo(Info, "Finished dumping " + fName + " in " + GetDurationAsString(duration), true);
            UpdateToolstripStatus("Dumped " + fName + " successfully");
            EnableButtons();
        }

        private void CreateSingleDumperThread(WzFile file, WzXml wzxml, string fileName) {
            IsFinished = false;
            var startTime = DateTime.Now;
            var mainTask = Task.Factory.StartNew(() => DirectoryDumperThread(file, wzxml, true));
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

        private void InitThread(string fileName, string dumpFolder, WzMapleVersion selectedValue) {
            WzListFile listFile = null;
            WzFile regFile = null;
            var message = String.Empty;
            try {
                if (fileName.EndsWith("List.wz", StringComparison.CurrentCultureIgnoreCase)) {
                    listFile = new WzListFile(fileName, selectedValue);
                    listFile.ParseWzFile();
                } else {
                    List<WzFile> s = new List<WzFile>();
                    getWzExtensionFiles(fileName, s);
                    regFile = new WzFile(fileName, selectedValue, s);
                    regFile.ParseWzFile();
                }
            } catch (IOException ex) {
                if (regFile != null) regFile.Dispose();
                message = "An IO error occurred: " + ex.Message;
            } catch (UnauthorizedAccessException) {
                if (regFile != null) regFile.Dispose();
                message = "Please re-run this program as an administrator.";
            } catch (Exception ex) {
                if (regFile != null) regFile.Dispose();
                message = "An error occurred while parsing this file: " + ex.Message;
            }
            if (!String.IsNullOrEmpty(message)) {
                UpdateTextBoxInfo(Info, "Error while parsing file " + Path.GetFileName(fileName) + "\r\nMessage: " + message + "\r\nContinuing...", true);
                IsError = true;
                return;
            }
            if (regFile == null && listFile == null)
                return;
            var wzName = Path.GetFileName(fileName);
            var nFolder = Path.Combine(dumpFolder, wzName);
            if (listFile == null && includeVersionInFolderBox.Checked)
                nFolder += "_v" + regFile.Version;
            nFolder = GetValidFolderName(nFolder, false);
            if (!Directory.Exists(nFolder))
                Directory.CreateDirectory(nFolder);
            if (listFile == null)
                UpdateTextBoxInfo(versionBox, regFile.Version.ToString(CultureInfo.CurrentCulture), false);
            if (listFile != null) {
                UpdateTextBoxInfo(Info, "Dumping data from " + wzName + " to " + nFolder + "...", true);
            } else if (includePngMp3Box.Checked) {
                UpdateTextBoxInfo(Info, "Dumping MP3s, PNGs and XMLs from " + wzName + " to " + nFolder + "...", true);
            } else {
                UpdateTextBoxInfo(Info, "Dumping XMLs from " + wzName + " to " + nFolder + "...", true);
            }
            if (listFile != null) {
                DumpListWz(listFile, wzName, nFolder, DateTime.Now);
                listFile.Dispose();
            } else {
                DirectoryDumperThread(regFile, new WzXml(this, dumpFolder, new DirectoryInfo(nFolder).Name, includePngMp3Box.Checked, (LinkType)LinkTypeComboBox.SelectedItem));
            }
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
                    InitThread(file, dumpFolder, SelectedVersion);
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

        private void DirectoryDumperThread(WzDirectory dir, WzXml wzxml, bool singleDump = false) {
            if (CancelSource.Token.IsCancellationRequested)
                return;
            try {
                wzxml.DumpDir(dir);
                if (!singleDump && !CancelSource.Token.IsCancellationRequested)
                    UpdateTextBoxInfo(Info, "Finished dumping " + dir.Name, true);
            } catch (Exception ex) {
                if (!CancelSource.Token.IsCancellationRequested) {
                    UpdateTextBoxInfo(Info, dir.Name + " Exception: " + ex.Message/* + " " + ex.StackTrace*/, true);
                    IsError = true;
                }
            } finally {
                dir.Dispose();
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
            if (CancelSource != null)
                CancelSource.Cancel(true);
            CancelOpButton.Enabled = false;
            UpdateTextBoxInfo(Info, "Canceling... Waiting for the current image(s) to finish dumping...", true);
        }

        private void Form1Load(object sender, EventArgs e) {
            if (!File.Exists(Application.StartupPath + @"\MapleLib.dll")) {
                MessageBox.Show("Please make sure MapleLib.dll is in the program directory before executing the program.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                Close();
                return;
            }
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
            MapleVersionComboBox.SelectedIndex = 0;
            LinkTypeComboBox.DataSource = Enum.GetValues(typeof(LinkType));
            LinkTypeComboBox.SelectedItem = IsElevated ? LinkType.Symbolic : LinkType.Hard;
        }

        private void Form1FormClosing(object sender, FormClosingEventArgs e) {
            if (IsFinished)
                return;
            if (MessageBox.Show("You can not close the program while it is still dumping. Do you wish to cancel the operation?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes) {
                Exit = true;
                if (CancelSource != null)
                    CancelSource.Cancel(true);
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
            LinkTypeComboBox.Enabled = true;
            DumpWzButton.Enabled = true;
            CancelOpButton.Enabled = false;
            includePngMp3Box.Enabled = true;
            includeVersionInFolderBox.Enabled = true;
            MapleVersionComboBox.Enabled = true;
            multiThreadCheckBox.Enabled = true;
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
            MapleVersionComboBox.Enabled = false;
            multiThreadCheckBox.Enabled = false;
            extractorThreadsLabel.Enabled = false;
            extractorThreadsNum.Enabled = false;
            Info.Focus();
        }

        private static int Compare(string x, string y) {
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

        private void MapleVersionComboBoxKeyPress(object sender, KeyPressEventArgs e) {
            e.Handled = true;
        }

        #region Nested type: UpdateTextBoxDelegate

        private delegate void UpdateTextBoxDelegate(TextBox tb, string info, bool append);

        #endregion

        private void MultiThreadCheckBox_CheckedChanged(object sender, EventArgs e) {
            extractorThreadsLabel.Enabled = multiThreadCheckBox.Checked;
            extractorThreadsNum.Enabled = multiThreadCheckBox.Checked;
        }

        private void IncludePngMp3Box_CheckedChanged(object sender, EventArgs e) {
            LinkTypeComboBox.Enabled = includePngMp3Box.Checked;
        }

        private void SelectWzFolder_Click(object sender, EventArgs e) {
            var open = new FolderBrowserDialog { Description = "Select the folder that contains the WZ Files you wish to dump" };
            if (open.ShowDialog() == DialogResult.OK) {
                var allFiles = Directory.GetFiles(open.SelectedPath, "*.wz");
                if (allFiles.Length == 0) {
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
                    LinkTypeComboBox.SelectedItem = LinkType.Hard;
                }

            }
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