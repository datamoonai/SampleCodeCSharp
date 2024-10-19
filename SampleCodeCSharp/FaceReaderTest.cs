using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DMReader;
using Newtonsoft.Json.Linq;
using System.Threading;
namespace SampleCodeCSharp
{


    public class FaceReaderTest
    {
        // Dictionary to track processed persons with their data
        private static Dictionary<string, ProcessedPersonData> _processedPersons = new Dictionary<string, ProcessedPersonData>();
        private static readonly object _lock = new object();
        private static readonly TimeSpan _expirationTime = TimeSpan.FromMinutes(2);

        public static void Dmr_FaceReaderEvent(object sender, TotalFacePacket e)
        {
            // Clean up expired entries first
            CleanupExpiredEntries();

            for (int i = 0; i < e.data.Length; i++)
            {
                // Create a unique key for this person
                string personKey = $"{e.camera_id}_{e.data[i].first_time}_{e.data[i].id}";

                lock (_lock)
                {
                    if (_processedPersons.TryGetValue(personKey, out var existingData))
                    {
                        bool isNewPacketBetter = false;
                        // شخص بار اول شناسایی نشده و بعدا شناسایی می شود
                        if (e.data[i].similarity.confidence != 0 && existingData.FacePacket.similarity.confidence == 0)
                            isNewPacketBetter = true;
                        // شخص قبلا شناسایی شده و الان با دقت بیشتری شناسایی می شود
                        else if (e.data[i].similarity.confidence != 0 && existingData.FacePacket.similarity.confidence != 0 && 
                            e.data[i].similarity.confidence > existingData.FacePacket.similarity.confidence)
                            isNewPacketBetter = true;

                        if (isNewPacketBetter)
                        {
                            _processedPersons[personKey] = new ProcessedPersonData
                            {
                                Timestamp = DateTime.Now,
                                FacePacket = e.data[i]
                            };

                            // Process this better quality packet
                            ProcessFaceData(personKey, e.camera_id, e.data[i], true);
                            Console.WriteLine("New packet is better");
                        }
                        else
                        {
                            // Update timestamp but keep the existing (better) data
                            existingData.Timestamp = DateTime.Now;
                            Console.WriteLine($"Keeping existing higher accuracy data for person: {e.data[i].id}");
                        }

                        continue;
                    }

                    Console.WriteLine("New packet recieved");

                    _processedPersons[personKey] = new ProcessedPersonData
                    {
                        Timestamp = DateTime.Now,
                        FacePacket = e.data[i]
                    };

                    ProcessFaceData(personKey, e.camera_id, e.data[i], false);
                }
            }
        }

        // Extract the processing logic to a separate method
        public static void ProcessFaceData(string key, int cameraId, FacePacket faceData, bool isUpdated)
        {
            //Console.WriteLine(faceData.id);

            if (isUpdated)
            {
                // update the face
            }
            else
            {
                // insert the face

                CommonHelpers.save_image(faceData.image, "face_thumbnail.jpg");

                // get high quality plate and car images
                string result = MainFaceHelper.GetPersonImage(cameraId, faceData, "face.jpg");
                JObject obj = JObject.Parse(result);
                string base64 = obj["base64"].ToString();
                CommonHelpers.save_image(base64, "face.jpg");

                result = MainFaceHelper.GetPersonImage(cameraId, faceData, "person.jpg");
                obj = JObject.Parse(result);
                base64 = obj["base64"].ToString();
                CommonHelpers.save_image(base64, "person.jpg");
            }
            
        }

        // Clean up expired entries
        public static void CleanupExpiredEntries()
        {
            lock (_lock)
            {
                var now = DateTime.Now;
                var keysToRemove = _processedPersons
                    .Where(kvp => now - kvp.Value.Timestamp > _expirationTime)
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in keysToRemove)
                {
                    _processedPersons.Remove(key);
                }

                if (keysToRemove.Count > 0)
                {
                    Console.WriteLine($"Cleaned up {keysToRemove.Count} expired entries");
                }
            }
        }

        // Optional: Method to get the best data for a specific person (if needed elsewhere)
        public static FacePacket GetBestFaceData(int cameraId, long firstTime, string id)
        {
            string key = $"{cameraId}_{firstTime}_{id}";

            lock (_lock)
            {
                if (_processedPersons.TryGetValue(key, out var data))
                {
                    return data.FacePacket;
                }
            }

            return null;
        }

        public  static void TestWebSocketFace()
        {
            // آی دی دوربین هایی که میخواهیم دیتای آن ها را دریافت کنیم وارد میکنیم.
            // این آی دی همان شناسه ای است که در قسمت مدیریت دوربین ها نمایش داده می شود
            List<string> ids = new List<string>();
            ids.Add("4");

            // آی پی سرور یا سیستمی که نرم افزار روی آن نصب است
            string serverIp = "127.0.0.1";

            // به صورت پیش فرض دیتا روی پورت 9003 ارسال می شود و این پورت باید روی سرور باز باشد
            int websocketPort = 8003;

            // همچنین برای دریافت تصاویر خودرو و پلاک لازم است این پورت نیز باز باشد
            int imagePort = 8002;

            DMReader.MainFaceHelper dmr = new MainFaceHelper(ids, serverIp, websocketPort, imagePort);
            // در این ایونت شما به دیتای اصلی دسترسی خواهید داشت
            dmr.FaceReaderEvent += Dmr_FaceReaderEvent;

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
    }

    public class ProcessedPersonData
    {
        public DateTime Timestamp { get; set; }
        public FacePacket FacePacket { get; set; }
    }

}
