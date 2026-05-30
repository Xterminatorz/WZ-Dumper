using System;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using WzComparerR2.WzLib;

namespace WzDumper.WZDumper {
    internal class WzJsonExtractor : WzExtractor {

        private readonly MemoryStream memoryStream = new MemoryStream();
        private readonly Utf8JsonWriter writer;
        private static readonly JsonEncodedText encWidth = JsonEncodedText.Encode("width");
        private static readonly JsonEncodedText encHeight = JsonEncodedText.Encode("height");
        //private static readonly JsonEncodedText encFormat = JsonEncodedText.Encode("format");
        //private static readonly JsonEncodedText encScale = JsonEncodedText.Encode("scale");
        //private static readonly JsonEncodedText encPages = JsonEncodedText.Encode("pages");
        private static readonly JsonEncodedText encX = JsonEncodedText.Encode("x");
        private static readonly JsonEncodedText encY = JsonEncodedText.Encode("y");

        public WzJsonExtractor(MainForm form, string parentPath, string wzStartingDir, bool extractAll, LinkType type) : base(form, parentPath, wzStartingDir, extractAll, type) {
            writer = new Utf8JsonWriter(memoryStream, options);
        }

        private JsonWriterOptions options = new JsonWriterOptions {
            Indented = true, // Set to false for better performance/smaller size
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public override void DumpImage(Wz_Image img, string mainDir) {
            Form.UpdateToolstripStatus("Dumping " + img.Name + ".json to " + mainDir);
            img.TryExtract();
            CurrentImageDir = Path.Combine(mainDir, img.Name);
            writer.WriteStartObject();
            DumpData(writer, img.Node, CurrentImageDir, mainDir.EndsWith("_Canvas"));
            writer.WriteEndObject();
            writer.Flush();
            File.WriteAllBytes(ExtractPath + "\\" + mainDir + "\\" + img.Name + ".json", memoryStream.ToArray());
            memoryStream.SetLength(0);
            writer.Reset(memoryStream);
            img.Unextract();
            foreach (var img2 in base.linkedImages)
                img2.Unextract();
        }

        private void DumpData(Utf8JsonWriter writer, Wz_Node node, string wzPath, bool isCanvas) {
            object value = node.Value;
            string nodeName = node.Text.Trim();
            bool closeObject = true;
            if (node.IsProperty) {
                writer.WriteStartObject(nodeName);
            } else if (value == null) {
                closeObject = false;
                writer.WriteNull(nodeName);
            } else if (value is Wz_Png png) {
                writer.WriteStartObject(nodeName);
                writer.WriteNumber(encWidth, png.Width);
                writer.WriteNumber(encHeight, png.Height);
                //writer.WriteNumber(encFormat, (int)png.Format);
                //writer.WriteNumber(encScale, png.Scale);
                //writer.WriteNumber(encPages, png.Pages);
                if (IncludePngMp3 && !(LinkType == LinkType.Copy && isCanvas)) {
                    if (!node.ParentNode.IsProperty)
                        wzPath = Path.Combine(wzPath, node.ParentNode.Text);
                    DumpCanvasProp(wzPath, node, null, false);
                }
            } else if (value is Wz_Uol uol) {
                closeObject = false;
                writer.WriteString(nodeName, uol.Uol);
                if (IncludePngMp3) {
                    var obj = uol.HandleUol(node);
                    if (obj != null) {
                        DumpFromUOL(node, obj, wzPath);
                    }
                }
            } else if (value is Wz_Vector vector) {
                writer.WriteStartObject(nodeName);
                writer.WriteNumber(encX, vector.X);
                writer.WriteNumber(encY, vector.Y);
            } else if (value is Wz_Sound) {
                writer.WriteStartObject(nodeName);
                if (IncludePngMp3) {
                    WriteSoundProp(wzPath, node, null, false, null);
                }
            } else if (value is Wz_Convex convex) {
                closeObject = false;
                writer.WriteStartArray(nodeName);
                foreach (var point in convex.Points) {
                    writer.WriteStartObject();
                    writer.WriteNumber(encX, point.X);
                    writer.WriteNumber(encY, point.Y);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            } else if (value is Wz_RawData) {
                writer.WriteStartObject(nodeName);
            } else if (value is Wz_Video) {
                writer.WriteStartObject(nodeName);
            } else {
                closeObject = false;
                switch (value) {
                    case float f:
                        writer.WriteString(nodeName, f.ToString("0.0########", Cul));
                        break;
                    case double d:
                        writer.WriteString(nodeName, d.ToString("0.0################", Cul));
                        break;
                    case short s:
                        writer.WriteNumber(nodeName, s);
                        break;
                    case int i:
                        writer.WriteNumber(nodeName, i);
                        break;
                    case long l:
                        writer.WriteNumber(nodeName, l);
                        break;
                    default:
                        writer.WriteString(nodeName, value.ToString());
                        break;
                }
            }
            foreach (var child in node.Nodes) {
                var path = child.IsProperty ? Path.Combine(wzPath, child.Text.Trim()) : wzPath;
                DumpData(writer, child, path, isCanvas);
            }
            if (closeObject)
                writer.WriteEndObject();
        }

        protected override void Dispose() {
            memoryStream.Dispose();
            writer.Dispose();
        }
    }
}
