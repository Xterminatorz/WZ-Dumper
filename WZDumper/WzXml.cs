using MapleLib.WzLib;
using MapleLib.WzLib.WzProperties;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Xml;

namespace WzDumper {
    public class WzXml {
        [DllImport("kernel32.dll")]
        public static extern uint GetLastError();
        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode)]
        static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);
        [DllImport("kernel32.dll", EntryPoint = "CreateSymbolicLinkW", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);

        public WzXml(MainForm form, string parentPath, string wzStartingDir, bool extractAll, LinkType type) {
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
        public static XmlWriterSettings XmlSettings { get; set; } = new XmlWriterSettings {
            Indent = true,
            IndentChars = "    ",
            Encoding = Encoding.UTF8
        };
        public CancellationToken Token { get; }
        public static CultureInfo Cul { get; } = CultureInfo.CreateSpecificCulture("en-US");
        public static char[] InvalidFileChars { get; } = Path.GetInvalidFileNameChars();
        public Dictionary<string, string> InvalidDirs { get; set; } = new Dictionary<string, string>();
        public string CurrentImageDir { get; set; }

        public void DumpDir(WzDirectory mainDir) {
            DumpDir(mainDir, WzFolderName);
            InvalidDirs.Clear();
            InvalidDirs = null;
        }

        public void DumpDir(WzDirectory mainDir, string wzDir) {
            foreach (var directory2 in mainDir.WzDirectories) {
                var dirName = Path.Combine(wzDir, directory2.Name);
                CreateDirectory(ref dirName);
                DumpDir(directory2, dirName);
            }
            foreach (var image in mainDir.WzImages) {
                if (Token.IsCancellationRequested)
                    return;
                DumpImage(image, wzDir);
            }
        }

        public void DumpImage(WzImage img, string mainDir) {
            Form.UpdateToolstripStatus("Dumping " + img.Name + ".xml to " + mainDir);
            using (StreamWriter sw = new StreamWriter(ExtractPath + "\\" + mainDir + "\\" + img.Name + ".xml")) {
                using (XmlWriter xmlWriter = XmlWriter.Create(sw, XmlSettings)) {
                    xmlWriter.WriteStartDocument(true);
                    xmlWriter.WriteStartElement("imgdir");
                    xmlWriter.WriteStartAttribute("name");
                    xmlWriter.WriteValue(img.Name);
                    CurrentImageDir = Path.Combine(mainDir, img.Name);
                    DumpData(xmlWriter, img.WzProperties, CurrentImageDir);
                    xmlWriter.WriteEndElement();
                    xmlWriter.WriteEndDocument();
                }
            }
            img.PartialDispose();
        }

        private void DumpData(XmlWriter tw, IEnumerable<AWzImageProperty> props, string wzPath) {
            foreach (var property in props.Where(property => property != null)) {
                switch (property.PropertyType) {
                    case WzPropertyType.ByteFloat:
                        var byteFloatProp = (WzByteFloatProperty)property;
                        tw.WriteStartElement("float");
                        tw.WriteStartAttribute("name");
                        tw.WriteValue(byteFloatProp.Name);
                        tw.WriteStartAttribute("value");
                        tw.WriteValue(byteFloatProp.Value.ToString("0.0######", Cul));
                        tw.WriteEndElement();
                        break;
                    case WzPropertyType.Canvas:
                        var canvasProp = (WzCanvasProperty)property;
                        if (IncludePngMp3) {
                            DumpCanvasProp(wzPath, canvasProp, null, false);
                        }
                        tw.WriteStartElement("canvas");
                        tw.WriteStartAttribute("name");
                        tw.WriteValue(canvasProp.Name);
                        tw.WriteStartAttribute("width");
                        tw.WriteValue(canvasProp.PngProperty.Width);
                        tw.WriteStartAttribute("height");
                        tw.WriteValue(canvasProp.PngProperty.Height);
                        DumpData(tw, canvasProp.WzProperties, wzPath);
                        tw.WriteEndElement();
                        break;
                    case WzPropertyType.CompressedInt:
                        var compressedIntProp = (WzCompressedIntProperty)property;
                        tw.WriteStartElement("int");
                        tw.WriteStartAttribute("name");
                        tw.WriteValue(compressedIntProp.Name);
                        tw.WriteStartAttribute("value");
                        tw.WriteValue(compressedIntProp.Value);
                        tw.WriteEndElement();
                        break;
                    case WzPropertyType.CompressedLong:
                        var compressedLongProp = (WzCompressedLongProperty)property;
                        tw.WriteStartElement("int");
                        tw.WriteStartAttribute("name");
                        tw.WriteValue(compressedLongProp.Name);
                        tw.WriteStartAttribute("value");
                        tw.WriteValue(compressedLongProp.Value);
                        tw.WriteEndElement();
                        break;
                    case WzPropertyType.Convex:
                        var convexProp = (WzConvexProperty)property;
                        tw.WriteStartElement("extended");
                        tw.WriteStartAttribute("name");
                        tw.WriteValue(convexProp.Name);
                        DumpData(tw, convexProp.WzProperties, wzPath);
                        tw.WriteEndElement();
                        break;
                    case WzPropertyType.Double:
                        var doubleProp = (WzDoubleProperty)property;
                        tw.WriteStartElement("double");
                        tw.WriteStartAttribute("name");
                        tw.WriteValue(doubleProp.Name);
                        tw.WriteStartAttribute("value");
                        tw.WriteValue(doubleProp.Value.ToString("0.0###############", Cul));
                        tw.WriteEndElement();
                        break;
                    case WzPropertyType.Null:
                        var nullProp = (WzNullProperty)property;
                        tw.WriteStartElement("null");
                        tw.WriteStartAttribute("name");
                        tw.WriteValue(nullProp.Name);
                        tw.WriteEndElement();
                        break;
                    case WzPropertyType.Short:
                        var shortProp = (WzShortProperty)property;
                        tw.WriteStartElement("short");
                        tw.WriteStartAttribute("name");
                        tw.WriteValue(shortProp.Name);
                        tw.WriteStartAttribute("value");
                        tw.WriteValue(shortProp.Value);
                        tw.WriteEndElement();
                        break;
                    case WzPropertyType.Sound:
                        var soundProp = (WzSoundProperty)property;
                        tw.WriteStartElement("sound");
                        tw.WriteStartAttribute("name");
                        tw.WriteValue(soundProp.Name);
                        tw.WriteEndElement();
                        if (IncludePngMp3) {
                            WriteSoundProp(wzPath, soundProp, null, false, null);
                        }
                        break;
                    case WzPropertyType.String:
                        var stringProp = (WzStringProperty)property;
                        tw.WriteStartElement("string");
                        tw.WriteStartAttribute("name");
                        tw.WriteValue(stringProp.Name);
                        tw.WriteStartAttribute("value");
                        tw.WriteValue(stringProp.Value);
                        tw.WriteEndElement();
                        break;
                    case WzPropertyType.SubProperty:
                        var subProp = (WzSubProperty)property;
                        tw.WriteStartElement("imgdir");
                        tw.WriteStartAttribute("name");
                        tw.WriteValue(subProp.Name);
                        DumpData(tw, subProp.WzProperties, Path.Combine(wzPath, subProp.Name));
                        tw.WriteEndElement();
                        break;
                    case WzPropertyType.UOL:
                        var uolProp = (WzUOLProperty)property;
                        tw.WriteStartElement("uol");
                        tw.WriteStartAttribute("name");
                        tw.WriteValue(uolProp.Name);
                        tw.WriteStartAttribute("value");
                        tw.WriteValue(uolProp.Value);
                        tw.WriteEndElement();
                        if (IncludePngMp3) {
                            var obj = uolProp.LinkValue;
                            if (obj != null) {
                                DumpFromUOL(uolProp, obj, wzPath);
                            }
                        }
                        break;
                    case WzPropertyType.Vector:
                        var vectorProp = (WzVectorProperty)property;
                        tw.WriteStartElement("vector");
                        tw.WriteStartAttribute("name");
                        tw.WriteValue(vectorProp.Name);
                        tw.WriteStartAttribute("x");
                        tw.WriteValue(vectorProp.X.Value);
                        tw.WriteStartAttribute("y");
                        tw.WriteValue(vectorProp.Y.Value);
                        tw.WriteEndElement();
                        break;
                }
            }
        }

        private void DumpFromUOL(AWzObject uolProp, AWzImageProperty obj, string wzPath, bool copyName = false) {
            var name = copyName ? obj.Name : uolProp.Name;
            switch (obj.PropertyType) {
                case WzPropertyType.Canvas:
                    var uolPngProp = (WzCanvasProperty)obj;
                    DumpCanvasProp(wzPath, uolPngProp, uolProp, copyName);
                    break;
                case WzPropertyType.SubProperty:
                    var uolSubProp = (WzSubProperty)obj;
                    var subDir = Path.Combine(wzPath, CleanFileName(name));
                    if (LinkType == LinkType.Symbolic) {
                        string linkPath = Path.Combine(ExtractPath, subDir);
                        string targetDir = Path.Combine(WzFolderName, uolSubProp.FullPath.Substring(uolSubProp.FullPath.IndexOf("\\", StringComparison.OrdinalIgnoreCase) + 1));
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
                        foreach (var file in uolSubProp.WzProperties) {
                            DumpFromUOL(uolProp, file, subDir, true);
                        }
                    }
                    break;
                case WzPropertyType.Sound:
                    name = CleanFileName(name);
                    var uolSoundProp = (WzSoundProperty)obj;
                    CreateDirectory(ref wzPath);
                    if (LinkType != LinkType.Copy) {
                        string linkPath = Path.Combine(ExtractPath, wzPath, name + ".mp3");
                        int lastIndex = uolSoundProp.FullPath.LastIndexOf("\\");
                        int startIndex = uolSoundProp.FullPath.IndexOf("\\", StringComparison.OrdinalIgnoreCase) + 1;
                        string targetPath = Path.Combine(WzFolderName, uolSoundProp.FullPath.Substring(startIndex, uolSoundProp.FullPath.Length - uolSoundProp.FullPath.Substring(lastIndex).Length - startIndex));
                        string targetFile = uolSoundProp.FullPath.Substring(lastIndex + 1) + ".mp3";
                        string fullPath = SanitizeTargetPath(targetPath, targetFile);
                        FileInfo file = new FileInfo(fullPath);
                        if (!File.Exists(fullPath)) {
                            file.Directory.Create();
                            WriteSoundProp(wzPath, uolSoundProp, uolProp, copyName, fullPath);
                        }
                        bool res = LinkType == LinkType.Symbolic ? CreateSymbolicLink(linkPath, fullPath, 0) : CreateHardLink(linkPath, fullPath, IntPtr.Zero);
                        if (!res) {
                            uint error = GetLastError();
                            if (error == 1142)
                                WriteSoundProp(wzPath, uolSoundProp, uolProp, copyName, null);
                            else if (error == 183) {
                                File.Delete(linkPath);
                                _ = LinkType == LinkType.Symbolic ? CreateSymbolicLink(linkPath, fullPath, 0) : CreateHardLink(linkPath, fullPath, IntPtr.Zero);
                            }  else
                                Form.UpdateTextBoxInfo(Form.InfoTextBox, "Error creating link: " + error + " - " + linkPath + " -> " + fullPath, true);
                        }
                    } else {
                        WriteSoundProp(wzPath, uolSoundProp, uolProp, copyName, null);
                    }
                    break;
                case WzPropertyType.UOL:
                    var subUOL = (WzUOLProperty)obj;
                    var uolVal = subUOL.LinkValue;
                    if (uolVal != null) {
                        DumpFromUOL(subUOL, uolVal, wzPath, false);
                    }
                    break;
            }
        }

        private void CreateDirectory(ref string directory) {
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

        private void SanitizePath(ref string directory) {
            string[] splitPath = directory.Trim().Split(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
            string newDirectory = String.Empty;
            foreach (string partPath in splitPath) {
                newDirectory += CleanFileName(partPath) + Path.DirectorySeparatorChar;
            }
            InvalidDirs.Add(directory, newDirectory);
            directory = newDirectory;
        }

        private string SanitizeTargetPath(string path, string name) {
            path = path.Replace("/", "\\");
            CreateDirectory(ref path);
            return Path.Combine(ExtractPath, path, CleanFileName(name));
        }

        private void DumpCanvasProp(string wzPath, WzCanvasProperty canvasProp, AWzObject uol, bool uolDirCopy) {
            string fileName = CleanFileName(uol != null && !uolDirCopy ? uol.Name : canvasProp.Name);
            if (LinkType != LinkType.Copy && !(string.IsNullOrEmpty(canvasProp.Outlink) && string.IsNullOrEmpty(canvasProp.Inlink) && uol == null)) {
                string targetPath, targetFile;
                if (!string.IsNullOrEmpty(canvasProp.Inlink)) {
                    int lastIndex = canvasProp.Inlink.LastIndexOf("/");
                    targetPath = Path.Combine(CurrentImageDir, canvasProp.Inlink.Substring(0, lastIndex));
                    targetFile = canvasProp.Inlink.Substring(lastIndex + 1) + ".png";
                } else if (!string.IsNullOrEmpty(canvasProp.Outlink)) {
                    int lastIndex = canvasProp.Outlink.LastIndexOf("/");
                    int startIndex = canvasProp.Outlink.IndexOf("/", StringComparison.OrdinalIgnoreCase) + 1;
                    targetPath = Path.Combine(WzFolderName, canvasProp.Outlink.Substring(startIndex, canvasProp.Outlink.Length - canvasProp.Outlink.Substring(lastIndex).Length - startIndex));
                    targetFile = canvasProp.Outlink.Substring(lastIndex + 1) + ".png";
                } else {
                    int lastIndex = canvasProp.FullPath.LastIndexOf("\\");
                    int startIndex = canvasProp.FullPath.IndexOf("\\", StringComparison.OrdinalIgnoreCase) + 1;
                    targetPath = Path.Combine(WzFolderName, canvasProp.FullPath.Substring(startIndex, canvasProp.FullPath.Length - canvasProp.FullPath.Substring(lastIndex).Length - startIndex));
                    targetFile = canvasProp.FullPath.Substring(lastIndex + 1) + ".png";
                }
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

        private void WriteSoundProp(string wzPath, WzSoundProperty soundProp, AWzObject uol, bool uolDirCopy, string overridePath) {
            string fileName = CleanFileName(uol != null && !uolDirCopy ? uol.Name : soundProp.Name);
            if (overridePath == null)
                CreateDirectory(ref wzPath);
            string ext = soundProp.GetExtension();
            string newFilePath = overridePath ?? Path.Combine(ExtractPath, wzPath, fileName + ext);
            Form.UpdateToolstripStatus("Dumping " + soundProp.Name + ext + " to " + newFilePath);
            using (var stream = new FileStream(newFilePath, FileMode.Create, FileAccess.Write)) {
                stream.Write(soundProp.GetBytes(), 0, soundProp.GetBytes().Length);
            }
        }

        private bool WritePng(string wzPath, string fileName, string filePath, WzCanvasProperty canvasProp, FileInfo overrideFile = null) {
            Form.UpdateToolstripStatus("Dumping " + fileName + ".png to " + wzPath);
            while (!string.IsNullOrEmpty(canvasProp.Inlink) || !string.IsNullOrEmpty(canvasProp.Outlink)) {
                if (!string.IsNullOrEmpty(canvasProp.Inlink)) {
                    if (canvasProp.InlinkValue == null)
                        return false;
                    canvasProp = canvasProp.InlinkValue;
                } else if (!string.IsNullOrEmpty(canvasProp.Outlink)) {
                    if (canvasProp.OutlinkValue == null)
                        return false;
                    canvasProp = canvasProp.OutlinkValue;
                }
            }
            if (canvasProp != null) {
                CreateDirectory(ref wzPath);
                overrideFile?.Directory.Create();
                if (filePath == null)
                    filePath = Path.Combine(ExtractPath, wzPath, fileName + ".png");
                using (var myFileOut = new FileStream(filePath, FileMode.Create)) {
                    if (canvasProp.PngProperty.GetPNG() == null)
                        Form.UpdateTextBoxInfo(Form.InfoTextBox, "Error Dumping " + fileName + ".png to " + wzPath, true);
                    else
                        canvasProp.PngProperty.GetPNG().Save(myFileOut, ImageFormat.Png);
                }
                return true;
            }
            return false;
        }
    }
}