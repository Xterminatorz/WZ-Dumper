using MapleLib.WzLib;
using MapleLib.WzLib.WzProperties;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace WzDumper.WZDumper {
    internal class WzXmlExtractor : WzExtractor {

        public WzXmlExtractor(MainForm form, string parentPath, string wzStartingDir, bool extractAll, LinkType type) : base(form, parentPath, wzStartingDir, extractAll, type) {
        }

        private static XmlWriterSettings XmlSettings { get; set; } = new XmlWriterSettings {
            Indent = true,
            IndentChars = "    ",
            Encoding = Encoding.UTF8
        };

        public override void DumpImage(WzImage img, string mainDir) {
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
                    case WzPropertyType.RawData:
                        var rawDataProp = (WzRawDataProperty)property;
                        tw.WriteStartElement("raw");
                        tw.WriteStartAttribute("name");
                        tw.WriteValue(rawDataProp.Name);
                        DumpData(tw, rawDataProp.WzProperties, wzPath);
                        tw.WriteEndElement();
                        /*if (IncludePngMp3) {
                            WriteRawData(wzPath, rawDataProp, null, false, null);
                        }*/
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
                        DumpData(tw, soundProp.WzProperties, wzPath);
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
                    case WzPropertyType.Video:
                        var videoProp = (WzVideoProperty)property;
                        tw.WriteStartElement("video");
                        tw.WriteStartAttribute("name");
                        tw.WriteValue(videoProp.Name);
                        DumpData(tw, videoProp.WzProperties, wzPath);
                        tw.WriteEndElement();
                        break;
                }
            }
        }

    }
}
