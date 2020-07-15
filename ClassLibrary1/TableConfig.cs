using HK.DynamicCompile.Exception;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace HK.DynamicCompile
{
    public partial class TableConfig
    {
        public enum FieldType
        {
            eInt,
            eFloat,
            eString,
            eBool,
            eDateTime,
            eDate
        }

        /// <summary>
        /// 这里为什么不直接引用C#的基础类型
        /// 目的是为了解耦，与抽象与实例分离
        /// </summary>
        private static Hashtable eHt = new Hashtable()
        {
            {"int", FieldType.eInt},
            {"float", FieldType.eFloat},
            {"str", FieldType.eString},
            {"bool", FieldType.eBool},
            {"date", FieldType.eDateTime},
            {"datetime", FieldType.eDate},
            {"i", FieldType.eInt},
            {"f", FieldType.eFloat},
            {"s", FieldType.eString},
            {"b", FieldType.eBool},
            {"d", FieldType.eDateTime},
            {"dt", FieldType.eDate}
        };

        private static Hashtable tHt = new Hashtable()
        {
            { FieldType.eInt, typeof(int) },
            { FieldType.eFloat, typeof(float) },
            { FieldType.eString, typeof(string) },
            { FieldType.eBool, typeof(bool) },
            { FieldType.eDateTime, typeof(DateTime) },
            { FieldType.eDate, typeof(int) }
        };

        public dynamic GetFieldValue(string FieldName, string FieldVlaue)
        {
            FieldType ft = this.ftypes[this.fields.IndexOf(FieldName)];
            return ConvertField(ft, FieldVlaue);
        }

        private dynamic ConvertField(FieldType ft, string FieldValue)
        {
            switch (ft)
            {
                case FieldType.eInt:
                    return Convert.ToInt32(FieldValue);
                case FieldType.eFloat:
                    return Convert.ToSingle(FieldValue);
                case FieldType.eString:
                    return FieldValue;
                case FieldType.eBool:
                    return Convert.ToBoolean(FieldValue);
                case FieldType.eDateTime:
                    return Convert.ToDateTime(FieldValue);
                case FieldType.eDate:
                    return Convert.ToInt32(FieldValue);
                default:
                    return null;
            }
        }
    }


    public partial class TableConfig : IEnumerable
    {
        public string name;
        public string primary;
        public IList<string> fields;
        public IList<FieldType> ftypes;
        private HashSet<string> fieldSet;

        private TableConfig(string name)
        {
            this.name = name;
            this.fields = new List<string>();
            this.ftypes = new List<FieldType>();
            this.fieldSet = new HashSet<string>();
        }

        private Type GetTrueType(FieldType ft)
        {
            if (tHt.ContainsKey(ft))
            {
                return (Type)tHt[ft];
            }
            else
            {
                string err = String.Format("there is no TrueType of {0}", nameof(ft));
                throw new NoTrueTypeInReflectionException(err);
            }
        }

        public bool HasField(string field)
        {
            return fieldSet.Contains(field);
        }

        public static IList<TableConfig> GetMetaTableConfig(IWorkbook workbook, string path)
        {
            ISheet sheet = workbook.GetSheet("meta");

            if (sheet == null)
            {
                throw new NoMetaSheetException(String.Format("no mate sheeet of {0}", path));
            }

            IList<TableConfig> list = TableConfig.GetTableConfig(sheet);
            return list;
        }

        public static IList<TableConfig> GetTableConfig(ISheet sheet)
        {
            IList<TableConfig> list = new List<TableConfig>();
            TableConfig cur = null;
            foreach (IRow item in sheet)
            {
                ICell c = item.GetCell(0);
                if (c.StringCellValue == "Table")
                {
                    if (cur != null) { list.Add(cur); }
                    cur = new TableConfig(item.GetCell(1).StringCellValue);
                }
                else
                {
                    if (cur == null) { continue; }
                    String cs = item.GetCell(0).StringCellValue;
                    cur.fields.Add(cs);
                    cur.fieldSet.Add(cs);

                    String fc = item.GetCell(1).StringCellValue;
                    if (eHt.ContainsKey(fc))
                    {
                        cur.ftypes.Add((FieldType)eHt[fc]);
                    }
                    else if (Enum.IsDefined(typeof(FieldType), fc))
                    {
                        FieldType ft = (FieldType)Enum.Parse(typeof(FieldType), fc);
                        cur.ftypes.Add(ft);
                    }
                    else
                    {
                        throw new NoFieldTypeDefineException(String.Format("no field type of {0} in {1}", cs, cur.name));
                    }

                    if (cur.primary == null && item.GetCell(2) != null)
                    {
                        ICell c2 = item.GetCell(2);
                        if (c2.ToString() == "unique")
                        {
                            cur.primary = cs;
                        }
                    }
                }
            }
            if (cur != null) { list.Add(cur); }
            return list;
        }

        public IDictionary<string, List<string>> GetTableValueSet(ISheet sheet)
        {
            ArrayList field_list = new ArrayList();
            IDictionary<string, List<string>> val_lists = new Dictionary<string, List<string>>();

            IRow meta_row = sheet.GetRow(0);
            foreach (ICell c in meta_row)
            {
                if (this.HasField(c.StringCellValue))
                {
                    field_list.Add(new List<string>());
                }
                else
                {
                    field_list.Add(null);
                }
            }

            foreach (IRow item in sheet)
            {
                if (item.RowNum == 0)
                {
                    continue;
                }

                foreach (ICell c in item)
                {
                    List<string> out_val = (List<string>)field_list[c.ColumnIndex];
                    out_val.Add(c.ToString());
                }
            }

            foreach (ICell c in meta_row)
            {
                List<string> item = (List<string>)field_list[c.ColumnIndex];
                if (item != null)
                {
                    val_lists.Add(c.ToString(), item);
                }
            }
            return val_lists;
        }

        public static List<object> DeserializeBin(string path)
        {
            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Binder = new DynamicClassBuilder.DynamicClassBuilderDeserializationBinder();
                object obj = formatter.Deserialize(stream);
                return obj as List<object>;
            }
        }

        internal class FieldEnumerator : IEnumerator
        {
            private TableConfig config;
            private int _ptr;

            internal FieldEnumerator(TableConfig config)
            {
                this.config = config;
                this._ptr = -1;
            }

            public object Current => new { Field = config.fields[_ptr], FieldType = config.GetTrueType(config.ftypes[_ptr]) };

            public bool MoveNext()
            {
                if (this._ptr + 1 == config.fields.Count) return false;
                else this._ptr += 1;
                return true;
            }

            public void Reset()
            {
                this._ptr = -1;
            }
        }

        public IEnumerator GetEnumerator()
        {
            return new FieldEnumerator(this);
        }
    }
}
