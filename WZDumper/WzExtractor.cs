using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using WzComparerR2.WzLib;

namespace WzDumper {
    public abstract class WzExtractor {
        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();
        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);
        [DllImport("kernel32.dll", EntryPoint = "CreateSymbolicLinkW", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);

        public WzExtractor(MainForm form, string parentPath, string wzStartingDir, bool extractAll, LinkType type) {
            Form = form;
            ExtractPath = parentPath;
            WzFolderName = wzStartingDir;
            IncludePngMp3 = extractAll;
            LinkType = type;
            Token = form.CancelSource.Token;
        }

        public MainForm Form { get; }
        public string ExtractPath { get; }
        public string WzFolderName { get; }
        public bool IncludePngMp3 { get; }
        public LinkType LinkType { get; }

        public CancellationToken Token { get; }
        public static CultureInfo Cul { get; } = CultureInfo.CreateSpecificCulture("en-US");
        public static char[] InvalidFileChars { get; } = Path.GetInvalidFileNameChars();
        public Dictionary<string, string> InvalidDirs { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> ProcessedPaths { get; set; } = new Dictionary<string, string>();
        public string CurrentImageDir { get; set; }

        internal readonly List<Wz_Image> linkedImages = new List<Wz_Image>();

        private readonly StringBuilder sb = new StringBuilder(500);

        public void DumpDir(Wz_Structure mainDir) {
            DumpDir(mainDir, mainDir.WzNode, WzFolderName);
            InvalidDirs.Clear();
            InvalidDirs = null;
            ProcessedPaths.Clear();
            ProcessedPaths = null;
            Dispose();
        }

        protected virtual void Dispose() { }

        public virtual void DumpDir(Wz_Structure mainDir, Wz_Node mainNode, string wzDir) {
            string mainNodeName = mainNode.FullPathToFile;
            foreach (var directory in mainNode.Nodes) {
                if (directory.Value == null && directory.Text.Equals(mainNodeName, StringComparison.OrdinalIgnoreCase)) { // Ignore MS file base
                    DumpDir(mainDir, directory, wzDir);
                } else if (directory.Value is Wz_File || directory.Value == null) {
                    if (directory.Nodes.Count != 0) {
                        var dirName = Path.Combine(wzDir, directory.Text);
                        CreateDirectory(ref dirName);
                        DumpDir(mainDir, directory, dirName);
                    }
                } else if (directory.Value is Wz_Image img) {
                    if (Token.IsCancellationRequested)
                        return;
                    if (img.Name.EndsWith("lua", StringComparison.OrdinalIgnoreCase)) {
                        DumpLua(img, wzDir);
                    } else {
                        DumpImage(img, wzDir);
                    }
                }
            }
        }

        public abstract void DumpImage(Wz_Image img, string mainDir);

        protected void DumpLua(Wz_Image img, string mainDir) {
            Form.UpdateToolstripStatus("Dumping " + img.Name + " to " + mainDir);
            img.TryExtract();
            using (TextWriter writer = new StreamWriter(ExtractPath + "\\" + mainDir + "\\" + img.Name)) {
                writer.Write(img.Node.Value);
            }
            img.Unextract();
        }

        protected void DumpFromUOL(Wz_Node uolNode, Wz_Node resolvedNode, string wzPath, bool copyName = false) {
            var name = copyName ? resolvedNode.Text.Trim() : uolNode.Text.Trim();
            object value = resolvedNode.Value;
            if (value is Wz_Png) {
                DumpCanvasProp(wzPath, resolvedNode, uolNode, copyName);
            } else if (value == null || value is Wz_Image) {
                var subDir = Path.Combine(wzPath, CleanFileName(name));
                if (LinkType == LinkType.Symbolic) {
                    string linkPath = Path.Combine(ExtractPath, subDir);
                    string targetDir = Path.Combine(WzFolderName, resolvedNode.FullPath);
                    Directory.CreateDirectory(Directory.GetParent(linkPath).FullName);
                    CreateDirectory(ref targetDir);
                    bool res = CreateSymbolicLink(linkPath, Path.Combine(ExtractPath, targetDir), 1);
                    if (!res) {
                        uint error = GetLastError();
                        if (error == 183) {
                            File.Delete(linkPath);
                            CreateSymbolicLink(linkPath, Path.Combine(ExtractPath, targetDir), 1);
                        } else
                            Form.UpdateTextBoxInfo(Form.InfoTextBox, "Error creating link: " + GetLastError() + " - " + linkPath + " -> " + targetDir, true);
                    }
                } else {
                    foreach (var file in resolvedNode.Nodes) {
                        DumpFromUOL(uolNode, file, subDir, true);
                    }
                }
            } else if (value is Wz_Sound) {
                name = CleanFileName(name);
                if (LinkType != LinkType.Copy) {
                    CreateDirectory(ref wzPath);
                    string ext = GetSoundExtension(resolvedNode, resolvedNode.GetValue<Wz_Sound>());
                    string linkPath = Path.Combine(ExtractPath, wzPath, name + ext);
                    string targetPath = Path.Combine(WzFolderName, resolvedNode.ParentNode.FullPath);
                    string targetFile = resolvedNode.Text + ext;
                    string fullPath = SanitizeTargetPath(targetPath, targetFile);
                    FileInfo file = new FileInfo(fullPath);
                    if (!File.Exists(fullPath)) {
                        file.Directory.Create();
                        WriteSoundProp(wzPath, resolvedNode, uolNode, copyName, fullPath);
                    }
                    bool res = LinkType == LinkType.Symbolic ? CreateSymbolicLink(linkPath, fullPath, 0) : CreateHardLink(linkPath, fullPath, IntPtr.Zero);
                    if (!res) {
                        uint error = GetLastError();
                        if (error == 1142)
                            WriteSoundProp(wzPath, resolvedNode, uolNode, copyName, null);
                        else if (error == 183) {
                            File.Delete(linkPath);
                            _ = LinkType == LinkType.Symbolic ? CreateSymbolicLink(linkPath, fullPath, 0) : CreateHardLink(linkPath, fullPath, IntPtr.Zero);
                        } else
                            Form.UpdateTextBoxInfo(Form.InfoTextBox, "Error creating link: " + error + " - " + linkPath + " -> " + fullPath, true);
                    }
                } else {
                    WriteSoundProp(wzPath, resolvedNode, uolNode, copyName, null);
                }
            } else if (value is Wz_Uol) {
                var subUOL = resolvedNode;
                var uolVal = subUOL.ResolveUol();
                if (uolVal != null) {
                    DumpFromUOL(subUOL, uolVal, wzPath);
                }
            }
        }

        protected void CreateDirectory(ref string directory) {
            if (InvalidDirs.ContainsKey(directory))
                InvalidDirs.TryGetValue(directory, out directory);
            string folderPath = Path.Combine(ExtractPath, directory);
            if (!Directory.Exists(folderPath)) {
                try {
                    Directory.CreateDirectory(folderPath);
                } catch (DirectoryNotFoundException) {
                    InvalidDirs.Add(directory, directory += "_");
                    folderPath = Path.Combine(ExtractPath, directory);
                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);
                } catch (NotSupportedException) {
                    SanitizePath(ref directory);
                    folderPath = Path.Combine(ExtractPath, directory);
                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);
                } catch (ArgumentException) {
                    SanitizePath(ref directory);
                    folderPath = Path.Combine(ExtractPath, directory);
                    if (!Directory.Exists(folderPath))
                        Directory.CreateDirectory(folderPath);
                }
            }
        }

        private static string CleanFileName(string fileName) {
            return InvalidFileChars.Aggregate(fileName, (current, c) => current.Replace(c.ToString(), "_"));
        }

        private void SanitizePath(ref string directory, bool cacheAsDir = true) {
            if (ProcessedPaths.ContainsKey(directory)) {
                ProcessedPaths.TryGetValue(directory, out directory);
                return;
            }
            string[] splitPath = directory.Split(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
            foreach (string partPath in splitPath) {
                sb.Append(CleanFileName(partPath.Trim())).Append(Path.DirectorySeparatorChar);
            }
            var newDirectory = sb.ToString();
            if (cacheAsDir)
                InvalidDirs.Add(directory, newDirectory);
            else
                ProcessedPaths.Add(directory, newDirectory);
            directory = newDirectory;
            sb.Clear();
        }

        private string SanitizeTargetPath(string path, string name) {
            path = path.Replace("/", "\\");
            CreateDirectory(ref path);
            return Path.Combine(ExtractPath, path, CleanFileName(name));
        }

        protected void DumpCanvasProp(string wzPath, Wz_Node canvasProp, Wz_Node uol, bool uolDirCopy) {
            string fileName = CleanFileName(uol != null && !uolDirCopy ? uol.Text.Trim() : canvasProp.Text.Trim());
            var inlink = canvasProp.Nodes["_inlink"];
            var outlink = canvasProp.Nodes["_outlink"];
            if (LinkType != LinkType.Copy && !(inlink == null && outlink == null && uol == null)) {
                string targetPath, targetFile;
                if (inlink != null) {
                    var val = inlink.GetValue<string>();
                    int lastIndex = val.LastIndexOf("/", StringComparison.OrdinalIgnoreCase);
                    targetPath = CurrentImageDir + "/" + val.Substring(0, lastIndex);
                    targetFile = val.Substring(lastIndex + 1).Trim() + ".png";
                } else if (outlink != null) {
                    var val = outlink.GetValue<string>();
                    int lastIndex = val.LastIndexOf("/", StringComparison.OrdinalIgnoreCase);
                    int startIndex = val.IndexOf("/", StringComparison.OrdinalIgnoreCase) + 1;
                    targetPath = WzFolderName + "/" + val.Substring(startIndex, val.Length - val.Substring(lastIndex).Length - startIndex);
                    targetFile = val.Substring(lastIndex + 1).Trim() + ".png";
                } else {
                    targetPath = WzFolderName + "/" + canvasProp.ParentNode.FullPath;
                    targetFile = canvasProp.Text.Trim() + ".png";
                }
                SanitizePath(ref targetPath, false);
                string fullTargetPath = SanitizeTargetPath(targetPath, targetFile);
                FileInfo file = new FileInfo(fullTargetPath);
                bool createLink = true;
                if (!File.Exists(fullTargetPath)) {
                    createLink = WritePng(wzPath, fileName, fullTargetPath, canvasProp, file);
                }
                if (createLink) {
                    CreateDirectory(ref wzPath);
                    string newFilePath = Path.Combine(ExtractPath, wzPath, fileName + ".png");
                    bool res = LinkType == LinkType.Symbolic ? CreateSymbolicLink(newFilePath, fullTargetPath, 0) : CreateHardLink(newFilePath, fullTargetPath, IntPtr.Zero);
                    if (!res) {
                        uint error = GetLastError();
                        if (error == 1142) // max links reached for file, fallback to copy mode
                            WritePng(wzPath, fileName, newFilePath, canvasProp);
                        else if (error == 183) { // link already exists, recreate in case old link was diff
                            File.Delete(newFilePath);
                            _ = LinkType == LinkType.Symbolic ? CreateSymbolicLink(newFilePath, fullTargetPath, 0) : CreateHardLink(newFilePath, fullTargetPath, IntPtr.Zero);
                        } else
                            Form.UpdateTextBoxInfo(Form.InfoTextBox, "Error creating link: " + error + " - " + newFilePath + " -> " + fullTargetPath, true);
                    }
                }
            } else {
                WritePng(wzPath, fileName, null, canvasProp);
            }
        }

        protected void WriteSoundProp(string wzPath, Wz_Node soundProp, Wz_Node uol, bool uolDirCopy, string overridePath) {
            string fileName = CleanFileName(uol != null && !uolDirCopy ? uol.Text.Trim() : soundProp.Text.Trim());
            if (overridePath == null)
                CreateDirectory(ref wzPath);
            Wz_Sound sound = soundProp.GetValue<Wz_Sound>();
            string ext = GetSoundExtension(soundProp, sound);
            string newFilePath = overridePath ?? Path.Combine(ExtractPath, wzPath, fileName + ext);
            Form.UpdateToolstripStatus("Dumping " + soundProp.Text + ext + " to " + newFilePath);
            using (var stream = new FileStream(newFilePath, FileMode.Create, FileAccess.Write)) {
                byte[] soundBytes = sound.ExtractSound();
                stream.Write(soundBytes, 0, soundBytes.Length);
            }
        }

        public string GetSoundExtension(Wz_Node soundProp, Wz_Sound sound) {
            if (sound.SoundType == Wz_SoundType.Mp3 || soundProp.Text.Contains("sound"))
                return ".mp3";
            if (sound.SoundType == Wz_SoundType.Binary && soundProp.Text.Equals("FONT_DATA", StringComparison.OrdinalIgnoreCase))
                return ".ttf";
            if (sound.SoundType == Wz_SoundType.Pcm)
                return ".wav";
            return "";
        }

        private bool WritePng(string wzPath, string fileName, string filePath, Wz_Node canvasProp, FileInfo overrideFile = null) {
            Form.UpdateToolstripStatus("Dumping " + fileName + ".png to " + wzPath);
            var inlink = canvasProp.Nodes["_inlink"];
            var outlink = canvasProp.Nodes["_outlink"];
            Wz_Png png;
            if (inlink != null || outlink != null) {
                png = GetLinkedPng(canvasProp);
                if (png == null)
                    return false;
            } else {
                png = canvasProp.GetValue<Wz_Png>();
            }
            Bitmap bmp;
            if (png.ActualPages > 1) {
                if (filePath == null)
                    filePath = Path.Combine(ExtractPath, wzPath, fileName);
                CreateDirectory(ref filePath);
                for (int i = 0; i < png.ActualPages; i++) {
                    using (bmp = png.ExtractPng(i)) {
                        if (bmp == null) {
                            Form.UpdateTextBoxInfo(Form.InfoTextBox, "Error Dumping " + i + ".png to " + filePath, true);
                            return false;
                        }
                        using (var fs = new FileStream(Path.Combine(filePath, i + ".png"), FileMode.Create)) {
                            bmp.Save(fs, ImageFormat.Png);
                        }
                    }
                }
                return true;
            } else {
                bmp = png.ExtractPng();
                if (bmp != null) {
                    CreateDirectory(ref wzPath);
                    overrideFile?.Directory.Create();
                    if (filePath == null)
                        filePath = Path.Combine(ExtractPath, wzPath, fileName + ".png");
                    using (var fs = new FileStream(filePath, FileMode.Create)) {
                        bmp.Save(fs, ImageFormat.Png);
                    }
                    bmp.Dispose();
                    return true;
                } else {
                    Form.UpdateTextBoxInfo(Form.InfoTextBox, "Error Dumping " + fileName + ".png to " + wzPath, true);
                }
            }
            return false;
        }

        private Wz_Png GetLinkedPng(Wz_Node node) {
            var wzFile = node.GetNodeWzFile();
            if (wzFile != null) {
                var linkNode = GetLinkedSourceNode(node);
                if (linkNode != null) {
                    var linkImg = linkNode.GetNodeWzImage();
                    if (!node.GetNodeWzImage().Name.Equals(linkImg.Name, StringComparison.OrdinalIgnoreCase))
                        linkedImages.Add(linkImg);
                }
                return linkNode.GetValueEx<Wz_Png>(null);
            }
            return null;
        }

        private static Wz_Node GetLinkedSourceNode(Wz_Node node) {
            string path;
            while (true) { // loop until no links
                if (node == null) return null;
                if (!string.IsNullOrEmpty(path = node.Nodes["source"].GetValueEx<string>(null))) {
                    node = FindNode(path, node.GetNodeWzFile());
                } else if (!string.IsNullOrEmpty(path = node.Nodes["_inlink"].GetValueEx<string>(null))) {
                    var img = node.GetNodeWzImage();
                    node = img?.Node.FindNodeByPath(false, path.Split('/'));
                } else if (!string.IsNullOrEmpty(path = node.Nodes["_outlink"].GetValueEx<string>(null))) {
                    node = FindNode(path, node.GetNodeWzFile());
                } else {
                    return node;
                }
            }
        }

        private static Wz_Node FindNode(string fullPath, Wz_File sourceWzFile) {
            var mainFile = sourceWzFile.Node.FullPathToFile + '/';
            if (fullPath.StartsWith(mainFile, StringComparison.OrdinalIgnoreCase))
                fullPath = fullPath.Substring(mainFile.Length);
            return sourceWzFile.Node.FindNodeByPath(true, fullPath.Split('/'));
        }

        internal static String TypeToName(String type) {
            if (type.EndsWith("32", StringComparison.OrdinalIgnoreCase))
                return "int";
            else if (type.EndsWith("16", StringComparison.OrdinalIgnoreCase))
                return "short";
            else if (type.EndsWith("64", StringComparison.OrdinalIgnoreCase))
                return "long";
            else if (type.Equals("single", StringComparison.OrdinalIgnoreCase))
                return "float";
            return type;
        }
    }
}