using System;
using System.Net;
using System.Data.SqlClient;
using System.Data;

namespace SQL
{
    class SQL
    {

        public static SqlConnection myConn = new SqlConnection("Password=123456;Persist Security Info=True;User ID=SQL-user;Initial Catalog=MyDB;Data Source=127.0.0.1");
        public static string responceSQL;
        public static DateTime nowSQL;
        [STAThread]
        static void Main(string[] args)
        {
            responceSQL = sqlGetTime();
            CreateListener();
        }

        public static string sqlGetTime()
        {
            try
            {
                string myCommand = "DECLARE @dt AS Datetime SET @dt = '2001-01-01 01:01:01' IF (SELECT max (Downtime_start) FROM [MyDB].[dbo].[work_down] ";
                myCommand += " where (Downtime_stop) IS NULL) is null begin SELECT @dt end else begin SELECT max(Downtime_start) FROM [MyDB].[dbo].[work_down] ";
                myCommand += " where(Downtime_stop) IS NULL end";
                SqlCommand cmd = new SqlCommand(myCommand, myConn);
                myConn.Open();
                DateTime dt = (DateTime)cmd.ExecuteScalar();
                nowSQL = DateTime.Now;
                var z = (nowSQL - dt).Duration();
                myConn.Close();
                string result = "{\"tag0\":\"" + dt + "\"}";
                return result;
            }
            catch (Exception ex)
            {
                //Console.WriteLine("SQL исключение: " + ex.ToString() + "\n " + ex.Message);
                System.IO.File.WriteAllText("C:\\logs\\err_sql_" + DateTime.Now.ToString("HHmmss") + ".txt", "SQL исключение: " + ex.ToString() + "\n " + ex.Message);
                return "{\"tag0\":\"error\"}";
            }
        }
        public static void CreateListener()
        {
            HttpListener listener = new HttpListener();
            // Add the prefixes.
            string url = "http://*";
            string port = "8880";
            string prefix = String.Format("{0}:{1}/", url, port);
            listener.Prefixes.Add(prefix);
            listener.Start();
            Console.WriteLine("Listening...");
            while (true)
            {
                // Note: The GetContext method blocks while waiting for a request.
                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;
                // Obtain a response object.
                HttpListenerResponse response = context.Response;
                context.Response.AddHeader("Access-Control-Allow-Origin", "*");
               //если со времени последнего запроса прошло больше 5 секунд, то делаем новый запрос
                DateTime nowTime = DateTime.Now;
                var z = (nowTime - nowSQL).Duration();
                if (z.Seconds >= 5) responceSQL = sqlGetTime(); 
                // Get a response stream and write the response to it.
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responceSQL);
                response.ContentLength64 = buffer.Length;
                System.IO.Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
            }
        }
    }
}
