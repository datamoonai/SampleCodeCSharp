using DMReader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace SampleCodeCSharp
{
    internal class Program
    {



        static void Main(string[] args)
        {
            // برای اتصال و دریافت دیتا به صورت بلادرنگ و در لحضه از وب سوکت استفاده می کنیم
            PlateReaderTest.TestWebSocketPlate();

            // تست چهره
            //FaceReaderTest.TestWebSocketFace();

            // اگر بخواهیم حداقل دیتای مربوط به پلاک را دریافت کنیم می توانیم از سوکت نیز استفاده کنیم
            //TestSocket();

            Console.WriteLine("https://datamoon.ir");
        }

        


    }
}
