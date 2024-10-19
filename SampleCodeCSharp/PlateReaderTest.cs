using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DMReader;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Net.Sockets;
using System.Text.RegularExpressions;


namespace SampleCodeCSharp
{


    public class PlateReaderTest
    {
        // Dictionary to track processed plates with their data
        private static Dictionary<string, ProcessedPlateData> _processedPlates = new Dictionary<string, ProcessedPlateData>();
        private static readonly object _lock = new object();
        private static readonly TimeSpan _expirationTime = TimeSpan.FromMinutes(2);
        static List<Character> characters = new List<Character>();

        public static void Dmr_PlateReaderEvent(object sender, TotalPlatePacket e)
        {
            // Clean up expired entries first
            CleanupExpiredEntries();

            for (int i = 0; i < e.data.Length; i++)
            {
                // Create a unique key for this plate
                string plateKey = $"{e.camera_id}_{e.data[i].first_time}_{e.data[i].id}";

                lock (_lock)
                {
                    if (_processedPlates.TryGetValue(plateKey, out var existingData))
                    {
                        bool isNewPacketBetter = false; // در عمل بهتر است همیشه آخرین بسته را به عنوان بهترین بسته نگه داریم
                        // اگر پلاک تغییر کرده باشد پلاک جدید بهتر است
                        // اگر مثلا برای ثبت سرعت میخواهیم استفاده کنیم در بسته ی اول سرعت مشخص نیست و در بسته های بعدی مشخص میشود
                        // همچنین بسته آخر بهترین پلاک و بهترین دقت سرعت سنجی را دارد
                        // بهتر است وقت بسته می رسد آن را در دیتابیس درج کنید ولی بعدا اطلاعات آن را با آخرین بسته بروز رسانی کنید
                        Console.WriteLine("New packet is better "+ e.data[i].vision_speed);

                        if (e.data[i].plate.plate != existingData.PlatePacket.plate.plate || e.data[i].vision_speed != existingData.PlatePacket.vision_speed)
                            isNewPacketBetter = true;

                        if (isNewPacketBetter)
                        {

                            _processedPlates[plateKey] = new ProcessedPlateData
                            {
                                Timestamp = DateTime.Now,
                                PlatePacket = e.data[i]
                            };

                            // Process this better quality packet
                            ProcessPlateData(plateKey, e.camera_id, e.data[i], true);
                            
                        }
                        else
                        {
                            // Update timestamp but keep the existing (better) data
                            existingData.Timestamp = DateTime.Now;
                            Console.WriteLine($"Keeping existing higher accuracy data for plate: {e.data[i].id}");
                        }

                        continue;
                    }

                    Console.WriteLine("New packet recieved");

                    _processedPlates[plateKey] = new ProcessedPlateData
                    {
                        Timestamp = DateTime.Now,
                        PlatePacket = e.data[i]
                    };

                    ProcessPlateData(plateKey, e.camera_id, e.data[i], false);
                }
            }
        }

        // Extract the processing logic to a separate method
        public static void ProcessPlateData(string key, int cameraId, PlatePacket plateData, bool isUpdated)
        {

            // اطلاعات مربوط به آی دی دوربینی که دیتا را ارسال کرده است و آی دی پلاک مربوطه
            // به طور کلی برنامه به این صورت عمل میکند که مادامی که پلاک جلوی دوربین هست پلاک خوانی انجام می شود
            // و دیتای آن ارسال میگردد ولی آی دی آن تغییر نمیکند
            // در صورت نیاز می توان تنظیماتی را انجام داد که فقط یک بار برای هر پلاک این دیتا ارسال شود
            // برای این کار با واحد فنی تماس بگیرید
            if (isUpdated)
            {
                // update the plate
            }
            else
            {
                // insert the plate
                Console.WriteLine("Camera id:" + cameraId + ", " + plateData.id);

                // اطلاعات مربوط به پلاک
                Console.WriteLine(MainPlateHelper.GetPersianPlate(plateData.plate));

                //  عکس بریده پلاک با کیفیت پایین در دیتای وب سوکت ارسال می شود که میتوان آن را ذخیره کرد
                // این عکس base64 هست و میتوان بدون ذخیره سازی هم در مموری آن را تبدیل به بیت مپ کرد
                CommonHelpers.save_image(plateData.image, cameraId + "plate.jpg");

                // برای دریافت تصاویر با کیفیت بالاتر پلاک و همچنین تصویر خودرو می توان از این متد استفاده کرد
                string result = MainPlateHelper.GetVehicleImage(cameraId, plateData, "car.jpg");
                JObject obj = JObject.Parse(result);
                string base64 = obj["base64"].ToString();
                CommonHelpers.save_image(base64, cameraId + "_car.jpg");

                // تصویر با کیفیت اصلی پلاک
                result = MainPlateHelper.GetVehicleImage(cameraId, plateData, "plate.jpg");
                obj = JObject.Parse(result);
                base64 = obj["base64"].ToString();
                CommonHelpers.save_image(base64, cameraId + "_plate.jpg");
            }
            
        }

        // Clean up expired entries
        public static void CleanupExpiredEntries()
        {
            lock (_lock)
            {
                var now = DateTime.Now;
                var keysToRemove = _processedPlates
                    .Where(kvp => now - kvp.Value.Timestamp > _expirationTime)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    // 
                    _processedPlates.Remove(key);
                }

                if (keysToRemove.Count > 0)
                {
                    Console.WriteLine($"Cleaned up {keysToRemove.Count} expired entries");
                }
            }
        }

        // Optional: Method to get the best data for a specific plate (if needed elsewhere)
        public static PlatePacket GetBestPlateData(int cameraId, long firstTime, string id)
        {
            string key = $"{cameraId}_{firstTime}_{id}";

            lock (_lock)
            {
                if (_processedPlates.TryGetValue(key, out var data))
                {
                    return data.PlatePacket;
                }
            }

            return null;
        }

        public static void TestWebSocketPlate()
        {
            // آی دی دوربین هایی که میخواهیم دیتای آن ها را دریافت کنیم وارد میکنیم.
            // این آی دی همان شناسه ای است که در قسمت مدیریت دوربین ها نمایش داده می شود
            List<string> ids = new List<string>();
            ids.Add("2");

            // آی پی سرور یا سیستمی که نرم افزار روی آن نصب است
            string serverIp = "127.0.0.1";

            // به صورت پیش فرض دیتا روی پورت 9003 ارسال می شود و این پورت باید روی سرور باز باشد
            int websocketPort = 9003;

            // همچنین برای دریافت تصاویر خودرو و پلاک لازم است این پورت نیز باز باشد
            int imagePort = 9002;

            DMReader.MainPlateHelper dmr = new MainPlateHelper(ids, serverIp, websocketPort, imagePort);
            // در این ایونت شما به دیتای اصلی دسترسی خواهید داشت
            dmr.PlateReaderEvent += Dmr_PlateReaderEvent;

            // در این ایونت شما به کل پکتی که در وب سوکت ارسال می شود به صورت یک آبجکت دسترسی دارید
            dmr.OnJsonDataReceived += Dmr_OnJsonDataReceived;

            while (true)
            {
                Thread.Sleep(1000);
            }
        }

        private static void Dmr_OnJsonDataReceived(object sender, JObject e)
        {
            Console.WriteLine("\nJson packet recieved " + e["data"].Count());
        }


        public static void MainSocket()
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

        public static void InitCharacters()
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

        public static void ReceiveThread(TcpClient client)
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

                            Console.WriteLine("Persian Plate:" + GetPersianPlate(item));
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

        public static void TestSocket()
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

    public class ProcessedPlateData
    {
        public DateTime Timestamp { get; set; }
        public PlatePacket PlatePacket { get; set; }
    }

}
