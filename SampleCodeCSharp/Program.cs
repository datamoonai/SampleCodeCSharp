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
        static List<Character> characters = new List<Character>();
        static void Main(string[] args)
        {
            // برای اتصال و دریافت دیتا به صورت بلادرنگ و در لحضه از وب سوکت استفاده می کنیم
            //TestWebSocket();

            // اگر بخواهیم حداقل دیتای مربوط به پلاک را دریافت کنیم می توانیم از سوکت نیز استفاده کنیم
            //TestSocket();

            Console.WriteLine("https://datamoon.ir");
        }

        private static void TestWebSocket()
        {
            // آی دی دوربین هایی که میخواهیم دیتای آن ها را دریافت کنیم وارد میکنیم.
            // این آی دی همان شناسه ای است که در قسمت مدیریت دوربین ها نمایش داده می شود
            List<string> ids = new List<string>();
            ids.Add("24");

            // آی پی سرور یا سیستمی که نرم افزار روی آن نصب است
            string serverIp = "172.16.11.10";

            // به صورت پیش فرض دیتا روی پورت 9003 ارسال می شود و این پورت باید روی سرور باز باشد
            int websocketPort = 9003;

            // همچنین برای دریافت تصاویر خودرو و پلاک لازم است این پورت نیز باز باشد
            int imagePort = 9002;

            DMReader.MainPlateHelper dmr = new MainPlateHelper(ids, serverIp, websocketPort, imagePort);
            dmr.PlateReaderEvent += Dmr_PlateReaderEvent;

            while (true)
            {
                Thread.Sleep(1000);
            }
        }

        private static void Dmr_PlateReaderEvent(object sender, TotalPlatePacket e)
        {
            for (int i = 0; i < e.data.Length; i++)
            {
                // اطلاعات مربوط به آی دی دوربینی که دیتا را ارسال کرده است و آی دی پلاک مربوطه
                // به طور کلی برنامه به این صورت عمل میکند که مادامی که پلاک جلوی دوربین هست پلاک خوانی انجام می شود
                // و دیتای آن ارسال میگردد ولی آی دی آن تغییر نمیکند
                // در صورت نیاز می توان تنظیماتی را انجام داد که فقط یک بار برای هر پلاک این دیتا ارسال شود
                // برای این کار با واحد فنی تماس بگیرید
                Console.WriteLine("Camera id:" + e.camera_id + ", " + e.data[i].id);

                // اطلاعات مربوط به پلاک
                Console.WriteLine(MainPlateHelper.GetPersianPlate(e.data[i].plate));

                //  عکس بریده پلاک با کیفیت پایین در دیتای وب سوکت ارسال می شود که میتوان آن را ذخیره کرد
                // این عکس base64 هست و میتوان بدون ذخیره سازی هم در مموری آن را تبدیل به بیت مپ کرد
                CommonHelpers.save_image(e.data[i].image, e.camera_id + "plate.jpg");

                // برای دریافت تصاویر با کیفیت بالاتر پلاک و همچنین تصویر خودرو می توان از این متد استفاده کرد
                string result = MainPlateHelper.GetVehicleImage(e.camera_id, e.data[i], "car.jpg");
                JObject obj = JObject.Parse(result);
                string base64 = obj["base64"].ToString();
                CommonHelpers.save_image(base64, e.camera_id + "_car.jpg");

                // تصویر با کیفیت اصلی پلاک
                result = MainPlateHelper.GetVehicleImage(e.camera_id, e.data[i], "plate.jpg");
                obj = JObject.Parse(result);
                base64 = obj["base64"].ToString();
                CommonHelpers.save_image(base64, e.camera_id + "_plate.jpg");
            }
        }

        private static void MainSocket()
        {
            // آی پی سرور
            string host = "172.16.11.10";

            // پورت پیش فرض دیتای سوکت پلاک خوان
            int port = 9006;

            // پورت پیش فرض دیتای سوکت تشخیص چهره
            //int port = 8006;

            TcpClient client = new TcpClient();

            client.Connect(host, port);
            Thread receiveThread = new Thread(() => ReceiveThread(client));
            receiveThread.Start();

            NetworkStream stream = client.GetStream();

            while (true)
            {
                Console.Write("\nMessage (y to break) :");
                string message = Console.ReadLine();
                byte[] data = Encoding.ASCII.GetBytes(message);
                stream.Write(data, 0, data.Length);

                if (message != "y")
                    continue;
                else
                    break;
            }

            client.Close();
        }

        public static string GetPersianPlate(PlateItem pi)
        {
            Character character = null;
            foreach (Character character2 in characters)
            {
                if (character2.english == pi.letter)
                {
                    character = character2;
                    break;
                }
            }

            return pi.first + character.persian + pi.second + pi.city_code;
        }

        static void InitCharacters()
        {
            if (characters.Count <= 0)
            {
                characters.Add(new Character("alef", "الف"));
                characters.Add(new Character("b", "ب"));
                characters.Add(new Character("j", "ج"));
                characters.Add(new Character("l", "ل"));
                characters.Add(new Character("m", "م"));
                characters.Add(new Character("n", "ن"));
                characters.Add(new Character("q", "ق"));
                characters.Add(new Character("v", "و"));
                characters.Add(new Character("h", "ه"));
                characters.Add(new Character("y", "ی"));
                characters.Add(new Character("d", "د"));
                characters.Add(new Character("s", "س"));
                characters.Add(new Character("sad", "ص"));
                characters.Add(new Character("malol", "معلول"));
                characters.Add(new Character("t", "ت"));
                characters.Add(new Character("ta", "ط"));
                characters.Add(new Character("ein", "ع"));
                characters.Add(new Character("diplomat", "D"));
                characters.Add(new Character("siyasi", "S"));
                characters.Add(new Character("p", "پ"));
                characters.Add(new Character("tashrifat", "تشریفات"));
                characters.Add(new Character("the", "ث"));
                characters.Add(new Character("ze", "ز"));
                characters.Add(new Character("she", "ش"));
                characters.Add(new Character("fe", "ف"));
                characters.Add(new Character("kaf", "ک"));
                characters.Add(new Character("gaf", "گ"));
            }
        }

        private static void ReceiveThread(TcpClient client)
        {
            try
            {
                NetworkStream stream = client.GetStream();
                byte[] data = new byte[100240];

                while (true)
                {
                    int bytes = stream.Read(data, 0, data.Length);

                    if (bytes > 0)
                    {
                        // می توان به فرمت های متفاوت دیتا را ارسال نمود به صورت پیش فرض فرمت زیر است
                        // camera_id,plate_id,plate\n\r
                        // 24,34,45b12345\n\r
                        string message = Encoding.UTF8.GetString(data, 0, bytes);
                        Console.WriteLine("Received from the server : {0} {1}", bytes, message);

                        if (Regex.Matches(message, ",").Count == 2)
                        {
                            message = message.Trim();
                            string[] parts = message.Split(',');
                            Console.WriteLine("Camera ID:" + parts[0]);
                            Console.WriteLine("Plate ID:" + parts[1]);

                            PlateItem item = new PlateItem();
                            item.plate = parts[2];
                            item.first = item.plate.Substring(0, 2);
                            item.second = item.plate.Substring(item.plate.Length - 5, 3);
                            item.city_code = item.plate.Substring(item.plate.Length - 2, 2);
                            item.letter = item.plate.Substring(2, item.plate.Length - 7);

                            Console.WriteLine("Persian Plate:" +GetPersianPlate(item));
                        }
                    }

                    Thread.Sleep(100);
                }
            }
            catch
            {
                // pass
            }

            client.Close();
        }

        private static void TestSocket()
        {
            InitCharacters();
            while (true)
            {
                try
                {
                    MainSocket();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                Thread.Sleep(5000);
            }
        }
    }
}
