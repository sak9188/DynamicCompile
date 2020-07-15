using HK.DynamicCompile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            string fName = @".\code.xlsx";
            TableConfig.SerializeXlsx(fName);
        }
    }
}
