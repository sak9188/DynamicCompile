using System;
using System.Runtime.InteropServices;

namespace HK.DynamicCompile
{
    public enum CommpressType
    {
        eNone,
        eZip    
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct sDynamicBin
    {
        /// <summary>
        /// 这个主要用来识别是否是被压缩过，压缩版本，压缩方式, 压缩密码
        /// Dynamic编译的版本 占4个字节
        /// 压缩过 占1个字节
        /// 压缩版本 占4个字节
        /// 压缩方式 占1个字节
        /// 压缩密码长度 占1个字节
        /// 压缩密码    占N个字节
        /// </summary>

        public Int32 DynamicVersion;

        public Byte CommpressType;

        public Byte Unknow0;
        public Byte Unknow1;
        public Byte Unknow2;
        public Byte Unknow3;
        public Byte Unknow4;
        public Byte Unknow5;
        public Byte Unknow6;
        public Byte Unknow7;
        public Byte Unknow8;
        public Byte Unknow9;
        public Byte Unknow10;

        // public Byte PasswordLength; 这个先不加，1.0.1在加进去
    }

    public static partial class DynamicClassBuilder
    {
        public static byte ver_1_major = 1;
        public static byte ver_2_second = 0;
        public static byte ver_3_third = 0;
        public static byte ver_4_minmal = 2;

        public static byte[] version = { ver_1_major, ver_2_second, ver_3_third, ver_4_minmal };

        public static Int32 GetDynamicVersion(byte[] ver=null)
        {
            if(ver==null)
                return BitConverter.ToInt32(version, 0);
            else
                return BitConverter.ToInt32(ver, 0);
        }

        public static byte[] GetDynamicMagicNumber(CommpressType commpressType = 0)
        {
            sDynamicBin bin = new sDynamicBin()
            {
                DynamicVersion = GetDynamicVersion(),
                CommpressType = (byte)commpressType
            };
            return StructToBytes(bin);
        }

        public static byte[] StructToBytes(object structObj)
        {
            int size = Marshal.SizeOf(structObj);
            IntPtr buffer = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(structObj, buffer, false);
                byte[] bytes = new byte[size];
                Marshal.Copy(buffer, bytes, 0, size);
                return bytes;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        public static object BytesToStruct(byte[] bytes, Type strcutType)
        {
            int size = Marshal.SizeOf(strcutType);
            IntPtr buffer = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.Copy(bytes, 0, buffer, size);
                return Marshal.PtrToStructure(buffer, strcutType);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }
    }

}
