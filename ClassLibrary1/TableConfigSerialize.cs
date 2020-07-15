using HK.DynamicCompile.Exception;
using ICSharpCode.SharpZipLib.GZip;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace HK.DynamicCompile
{
    public partial class TableConfig
    {
        public byte[] SerializeToBytes(List<dynamic> valList)
        {
            MemoryStream stream;
            using (stream = new MemoryStream(1024))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, valList);
            }
            return stream.ToArray();
        }

        public bool SerializeToFile(List<dynamic> valList, bool commpress)
        {
            MemoryStream outStream = new MemoryStream(1024);
            List<byte> outBytes = SerializeToBytes(valList).ToList();
            CommpressType ct = CommpressType.eNone;
            if (commpress)
            {
                GZipOutputStream cpsStream = new GZipOutputStream(outStream);
                using (cpsStream)
                {
                    cpsStream.Write(outBytes.ToArray(), 0, outBytes.Count);
                }
                outBytes = outStream.ToArray().ToList();
                ct = CommpressType.eZip;
            }
            try
            {
                string FileName = String.Format("{0}.bin", this.name);
                using (FileStream stream = new FileStream(@".\" + FileName, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    byte[] magic = DynamicClassBuilder.GetDynamicMagicNumber(ct);
                    stream.Write(magic, 0, magic.Length);
                    stream.Write(outBytes.ToArray(), 0, outBytes.Count);
                }
            }
            catch (System.Exception)
            {
                return false;
            }
            return true;
        }

        public bool SerializeBin(List<dynamic> valList, bool commpress = true)
        {
            return SerializeToFile(valList, commpress);
        }

        public static void SerializeXlsx(string path)
        {
            IWorkbook workbook;
            try
            {
                workbook = new XSSFWorkbook(path);
            }
            catch (System.Exception)
            {
                throw new OpenXlsxFileFailedException();
            }

            IList<TableConfig> list = TableConfig.GetMetaTableConfig(workbook, path);
            DynamicClassBuilder.CompileDynamicTypes(list);

            foreach (TableConfig item in list)
            {
                DynamicClassBuilder.CompileDynamicType(item);
                ISheet sheet = workbook.GetSheet(item.name);
                IDictionary<string, List<string>> valueSet = item.GetTableValueSet(sheet);
                List<dynamic> dyList = DynamicClassBuilder.NewDynamics(item, valueSet);
                item.SerializeBin(dyList);
            }

            workbook.Close();
        }
    }
}
