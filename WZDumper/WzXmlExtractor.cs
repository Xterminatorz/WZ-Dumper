using System;
using System.IO;
using System.Text;
using System.Xml;
using WzComparerR2.WzLib;

namespace WzDumper.WZDumper {
    internal class WzXmlExtractor : WzExtractor {

        public WzXmlExtractor(MainForm form, string parentPath, string wzStartingDir, bool extractAll, LinkType type) : base(form, parentPath, wzStartingDir, extractAll, type) {
        }

        private static XmlWriterSettings XmlSettings { get; set; } = new XmlWriterSettings {
            Indent = true,
            IndentChars = "    ",
            Encoding = Encoding.UTF8
        };

        public override void DumpImage(Wz_Image img, string mainDir) {
            Form.UpdateToolstripStatus("Dumping " + img.Name + ".xml to " + mainDir);
            img.TryExtract();
            using (StreamWriter sw = new StreamWriter(ExtractPath + "\\" + mainDir + "\\" + img.Name + ".xml")) {
                using (XmlWriter xmlWriter = XmlWriter.Create(sw, XmlSettings)) {
                    xmlWriter.WriteStartDocument(true);
                    CurrentImageDir = Path.Combine(mainDir, img.Name);
                    DumpData(xmlWriter, img.Node, CurrentImageDir, mainDir.EndsWith("_Canvas"));
                    xmlWriter.WriteEndDocument();
                }
            }
            img.Unextract();
            foreach (var img2 in base.linkedImages)
                img2.Unextract();
        }

        private void DumpData(XmlWriter writer, Wz_Node node, string wzPath, bool isCanvas) {
            object value = node.Value;
            string nodeName = node.Text.Trim();
            if (node.IsProperty) {
                writer.WriteStartElement("imgdir");
                writer.WriteAttributeString("name", nodeName);
            } else if (value == null) {
                writer.WriteStartElement("null");
                writer.WriteAttributeString("name", nodeName);
            } else if (value is Wz_Png png) {
                writer.WriteStartElement("canvas");
                writer.WriteAttributeString("name", nodeName);
                writer.WriteAttributeString("width", png.Width.ToString());
                writer.WriteAttributeString("height", png.Height.ToString());
                //writer.WriteAttributeString("format", ((int)png.Format).ToString());
                //writer.WriteAttributeString("scale", png.Scale.ToString());
                //writer.WriteAttributeString("pages", png.Pages.ToString());
                if (IncludePngMp3 && !(LinkType == LinkType.Copy && isCanvas)) {
                    if (!node.ParentNode.IsProperty)
                        wzPath = Path.Combine(wzPath, node.ParentNode.Text);
                    DumpCanvasProp(wzPath, node, null, false);
                }
            } else if (value is Wz_Uol uol) {
                writer.WriteStartElement("uol");
                writer.WriteAttributeString("name", nodeName);
                writer.WriteAttributeString("value", uol.Uol);
                if (IncludePngMp3) {
                    var obj = uol.HandleUol(node);
                    if (obj != null) {
                        DumpFromUOL(node, obj, wzPath);
                    }
                }
            } else if (value is Wz_Vector vector) {
                writer.WriteStartElement("vector");
                writer.WriteAttributeString("name", nodeName);
                writer.WriteAttributeString("x", vector.X.ToString());
                writer.WriteAttributeString("y", vector.Y.ToString());
            } else if (value is Wz_Sound) {
                writer.WriteStartElement("sound");
                writer.WriteAttributeString("name", nodeName);
                if (IncludePngMp3) {
                    WriteSoundProp(wzPath, node, null, false, null);
                }
            } else if (value is Wz_Convex convex) {
                writer.WriteStartElement("convex");
                writer.WriteAttributeString("name", nodeName);
                foreach (var point in convex.Points) {
                    writer.WriteStartElement("vector");
                    writer.WriteAttributeString("x", point.X.ToString());
                    writer.WriteAttributeString("y", point.Y.ToString());
                    writer.WriteEndElement();
                }
            } else if (value is Wz_RawData) {
                writer.WriteStartElement("raw");
                writer.WriteAttributeString("name", nodeName);
            } else if (value is Wz_Video) {
                writer.WriteStartElement("video");
                writer.WriteAttributeString("name", nodeName);
            } else {
                var tag = value.GetType().Name.ToLower();
                tag = TypeToName(tag);
                writer.WriteStartElement(tag);
                writer.WriteAttributeString("name", nodeName);
                if (tag.Equals("float"))
                    writer.WriteAttributeString("value", node.GetValue<float>().ToString("0.0########", Cul));
                else if (tag.Equals("double"))
                    writer.WriteAttributeString("value", node.GetValue<double>().ToString("0.0################", Cul));
                else
                    writer.WriteAttributeString("value", value.ToString());
            }
            foreach (var child in node.Nodes) {
                var path = child.IsProperty ? Path.Combine(wzPath, child.Text.Trim()) : wzPath;
                DumpData(writer, child, path, isCanvas);
            }
            writer.WriteEndElement();
        }
    }
}
