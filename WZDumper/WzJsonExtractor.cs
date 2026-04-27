using MapleLib.WzLib;
using MapleLib.WzLib.WzProperties;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;


namespace WzDumper.WZDumper {
    internal class WzJsonExtractor : WzExtractor {

        private JsonWriterOptions options = new JsonWriterOptions {
            Indented = true, // Set to false for better performance/smaller size
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private static readonly JsonEncodedText encType = JsonEncodedText.Encode("type");
        private static readonly JsonEncodedText encCanvas = JsonEncodedText.Encode("canvas");
        private static readonly JsonEncodedText encWidth = JsonEncodedText.Encode("width");
        private static readonly JsonEncodedText encHeight = JsonEncodedText.Encode("height");
        private static readonly JsonEncodedText encConvex = JsonEncodedText.Encode("convex");
        private static readonly JsonEncodedText encUol = JsonEncodedText.Encode("uol");
        private static readonly JsonEncodedText encRaw = JsonEncodedText.Encode("raw");
        private static readonly JsonEncodedText encValue = JsonEncodedText.Encode("value");
        private static readonly JsonEncodedText encX = JsonEncodedText.Encode("x");
        private static readonly JsonEncodedText encY = JsonEncodedText.Encode("y");
        private static readonly JsonEncodedText encVector = JsonEncodedText.Encode("vector");
        private static readonly JsonEncodedText encVideo = JsonEncodedText.Encode("video");

        private readonly MemoryStream memoryStream = new MemoryStream();
        private readonly Utf8JsonWriter writer;

        public WzJsonExtractor(MainForm form, string parentPath, string wzStartingDir, bool extractAll, LinkType type) : base(form, parentPath, wzStartingDir, extractAll, type) {
            writer = new Utf8JsonWriter(memoryStream, options);
        }

        public override void DumpDir(WzDirectory mainDir, string wzDir) {
            base.DumpDir(mainDir, wzDir);
        }

        public override void DumpImage(WzImage img, string mainDir) {
            Form.UpdateToolstripStatus("Dumping " + img.Name + ".json to " + mainDir);
            writer.WriteStartObject();
            writer.WriteStartObject(img.Name);
            CurrentImageDir = Path.Combine(mainDir, img.Name);
            DumpData(writer, img.WzProperties, CurrentImageDir);
            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.Flush();
            File.WriteAllBytes(ExtractPath + "\\" + mainDir + "\\" + img.Name + ".json", memoryStream.ToArray());
            memoryStream.SetLength(0);
            writer.Reset(memoryStream);
            img.PartialDispose();
        }

        private void DumpData(Utf8JsonWriter tw, IEnumerable<AWzImageProperty> props, string wzPath) {
            foreach (var property in props.Where(property => property != null)) {
                switch (property.PropertyType) {
                    case WzPropertyType.ByteFloat:
                        var byteFloatProp = (WzByteFloatProperty)property;
                        tw.WriteString(byteFloatProp.Name, byteFloatProp.Value.ToString("0.0######", Cul));
                        break;
                    case WzPropertyType.Canvas:
                        var canvasProp = (WzCanvasProperty)property;
                        if (IncludePngMp3) {
                            DumpCanvasProp(wzPath, canvasProp, null, false);
                        }
                        tw.WriteStartObject(canvasProp.Name);
                        tw.WriteString(encType, encCanvas);
                        tw.WriteNumber(encWidth, canvasProp.PngProperty.Width);
                        tw.WriteNumber(encHeight, canvasProp.PngProperty.Height);
                        DumpData(tw, canvasProp.WzProperties, wzPath);
                        tw.WriteEndObject();
                        break;
                    case WzPropertyType.CompressedInt:
                        var compressedIntProp = (WzCompressedIntProperty)property;
                        tw.WriteNumber(compressedIntProp.Name, compressedIntProp.Value);
                        break;
                    case WzPropertyType.CompressedLong:
                        var compressedLongProp = (WzCompressedLongProperty)property;
                        tw.WriteNumber(compressedLongProp.Name, compressedLongProp.Value);
                        break;
                    case WzPropertyType.Convex:
                        var convexProp = (WzConvexProperty)property;
                        tw.WriteStartObject(convexProp.Name);
                        tw.WriteString(encType, encConvex);
                        DumpData(tw, convexProp.WzProperties, wzPath);
                        tw.WriteEndObject();
                        break;
                    case WzPropertyType.Double:
                        var doubleProp = (WzDoubleProperty)property;
                        tw.WriteString(doubleProp.Name, doubleProp.Value.ToString("0.0###############", Cul));
                        break;
                    case WzPropertyType.Null:
                        var nullProp = (WzNullProperty)property;
                        tw.WriteNull(nullProp.Name);
                        break;
                    case WzPropertyType.RawData:
                        var rawDataProp = (WzRawDataProperty)property;
                        tw.WriteStartObject(rawDataProp.Name);
                        tw.WriteString(encType, encRaw);
                        DumpData(tw, rawDataProp.WzProperties, wzPath);
                        tw.WriteEndObject();
                        /*if (IncludePngMp3) {
                            WriteRawData(wzPath, rawDataProp, null, false, null);
                        }*/
                        break;
                    case WzPropertyType.Short:
                        var shortProp = (WzShortProperty)property;
                        tw.WriteNumber(shortProp.Name, shortProp.Value);
                        break;
                    case WzPropertyType.Sound:
                        var soundProp = (WzSoundProperty)property;
                        tw.WriteStartObject(soundProp.Name);
                        DumpData(tw, soundProp.WzProperties, wzPath);
                        tw.WriteEndObject();
                        if (IncludePngMp3) {
                            WriteSoundProp(wzPath, soundProp, null, false, null);
                        }
                        break;
                    case WzPropertyType.String:
                        var stringProp = (WzStringProperty)property;
                        tw.WriteString(stringProp.Name, stringProp.Value);
                        break;
                    case WzPropertyType.SubProperty:
                        var subProp = (WzSubProperty)property;
                        tw.WriteStartObject(subProp.Name);
                        DumpData(tw, subProp.WzProperties, Path.Combine(wzPath, subProp.Name));
                        tw.WriteEndObject();
                        break;
                    case WzPropertyType.UOL:
                        var uolProp = (WzUOLProperty)property;
                        tw.WriteStartObject(uolProp.Name);
                        tw.WriteString(encType, encUol);
                        tw.WriteString(encValue, uolProp.Value);
                        tw.WriteEndObject();
                        if (IncludePngMp3) {
                            var obj = uolProp.LinkValue;
                            if (obj != null) {
                                DumpFromUOL(uolProp, obj, wzPath);
                            }
                        }
                        break;
                    case WzPropertyType.Vector:
                        var vectorProp = (WzVectorProperty)property;
                        tw.WriteStartObject(vectorProp.Name);
                        tw.WriteString(encType, encVector);
                        tw.WriteNumber(encX, vectorProp.X.Value);
                        tw.WriteNumber(encY, vectorProp.Y.Value);
                        tw.WriteEndObject();
                        break;
                    case WzPropertyType.Video:
                        var videoProp = (WzVideoProperty)property;
                        tw.WriteStartObject(videoProp.Name);
                        tw.WriteString(encType, encVideo);
                        DumpData(tw, videoProp.WzProperties, wzPath);
                        tw.WriteEndObject();
                        break;
                }
            }
        }

        protected override void Dispose() {
            memoryStream.Dispose();
            writer.Dispose();
        }
    }
}
