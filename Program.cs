using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JobGetApiUTCC
{
    class Program
    {
        public static string _Connection = ConfigurationSettings.AppSettings["ConnectionString"];
        public static string _LogFile = ConfigurationSettings.AppSettings["LogFile"];
        public static void Log(String iText)
        {
            string pathlog = _LogFile;
            String logFolderPath = System.IO.Path.Combine(pathlog, DateTime.Now.ToString("yyyyMMdd"));

            if (!System.IO.Directory.Exists(logFolderPath))
            {
                System.IO.Directory.CreateDirectory(logFolderPath);
            }
            String logFilePath = System.IO.Path.Combine(logFolderPath, DateTime.Now.ToString("yyyyMMdd") + ".txt");

            try
            {
                using (System.IO.StreamWriter outfile = new System.IO.StreamWriter(logFilePath, true))
                {
                    System.Text.StringBuilder sbLog = new System.Text.StringBuilder();

                    String[] listText = iText.Split('|').ToArray();

                    foreach (String s in listText)
                    {
                        sbLog.AppendLine($"[{DateTime.Now:HH:mm:ss}] {s}");
                    }

                    outfile.WriteLine(sbLog.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing log file: {ex.Message}");
            }
        }
        static async Task Main(string[] args)
        { 
            try
            {
                Log("====== Start Process ====== : " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                Log(string.Format("Run batch as :{0}", System.Security.Principal.WindowsIdentity.GetCurrent().Name));
                DataconDataContext db = new DataconDataContext(_Connection);
                if (db.Connection.State == ConnectionState.Open)
                {
                    db.Connection.Close();
                    db.Connection.Open();
                }
                db.Connection.Open();
                db.CommandTimeout = 0;
                using (HttpClient client = new HttpClient())
                {
                    try
                    {
                        client.DefaultRequestHeaders.Clear();
                        string url = "https://utcc-apis.utcc.ac.th/api/assets";
                        HttpResponseMessage response = await client.GetAsync(url, CancellationToken.None);
                        response.EnsureSuccessStatusCode();
                        string responseBody = await response.Content.ReadAsStringAsync();
                        var Resultapi = JsonConvert.DeserializeObject<Asset>(responseBody);
                        if (Resultapi != null)
                        {
                            InsertData(db, Resultapi.data);
                        }
                    }
                    catch (HttpRequestException e)
                    {

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR");
                Console.WriteLine("Exit ERROR");
                Log("ERROR");
                Log("message: " + ex.Message);
                Log("Exit ERROR");
            }
            finally
            {
                Log("====== End Process Process ====== : " + DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
            }
        }
        public static void InsertData(DataconDataContext db, List<AssetData> Resultapi)
        {
            Log("Count data : " + Resultapi.Count());
            Console.WriteLine("Count data : " + Resultapi.Count());
            db.ExecuteCommand("TRUNCATE TABLE AssetData");
            db.Transaction = db.Connection.BeginTransaction();
            try
            {
                foreach (var item in Resultapi)
                {
                    AssetData iassetData = new AssetData
                    {
                        asset_no = item.asset_no,
                        asset_desc1 = item.asset_desc1,
                        asset_desc2 = item.asset_desc2,
                        class_no = item.class_no,
                        inventory_no = item.inventory_no
                    };
                    db.AssetDatas.InsertOnSubmit(iassetData);
                    db.SubmitChanges();
                }
                db.Transaction.Commit(); // ยืนยันการเปลี่ยนแปลงหากทั้งหมดเป็นไปโดยสมบูรณ์
            }
            catch (Exception ex)
            {
                db.Transaction.Rollback(); // ยกเลิกการเปลี่ยนแปลงในกรณีที่เกิดข้อผิดพลาด
                Log("Error while inserting data: " + ex.Message); // ส่งข้อผิดพลาดต่อไปยังผู้เรียก
                Console.WriteLine("Error while inserting data: " + ex.Message);
            }
            finally
            {
                Log("Insert Completed : " + Resultapi.Count());
                Console.WriteLine("Insert Completed : " + Resultapi.Count());
                db.Connection.Close(); // ปิดการเชื่อมต่อ
            }
        }
    }
}
