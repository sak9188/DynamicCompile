using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HK.DynamicCompile.Exception
{
    public class NoFieldInDynamicClassException : System.Exception
    {
        public NoFieldInDynamicClassException(string message="") : base(message) { }
    }

    public class NoTrueTypeInReflectionException : System.Exception
    {
        public NoTrueTypeInReflectionException(string message="") : base(message) { }
    }

    public class NoMetaSheetException : System.Exception
    {
        public NoMetaSheetException(string message = "") : base(message) { }
    }

    public class NoFieldTypeDefineException : System.Exception
    {
        public NoFieldTypeDefineException(string message = "") : base(message) { }
    }

    public class OpenXlsxFileFailedException : System.Exception
    {
        public OpenXlsxFileFailedException(string message = "") : base(message) { }
    }
}
