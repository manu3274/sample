using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Start();
        }

        MySocket recv_socket = new MySocket(null, 1100);
        MySocket send_socket = new MySocket(null, 1101);

        public void Start()
        {
            var options = new JsonSerializerOptions
            {
                // 日本語をエスケープしない
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                // コメントを許可
                ReadCommentHandling = JsonCommentHandling.Skip,
                // 末尾のコンマを許可
                AllowTrailingCommas = true
            };

            Dictionary<string, Dictionary<string, string>> conf = null;
            using (FileStream fs = new FileStream(System.AppDomain.CurrentDomain.BaseDirectory + "config\\config.json", FileMode.Open, FileAccess.Read))
            {
                conf =JsonSerializer.Deserialize<Dictionary<string, Dictionary<string,string>>>(fs, options);
            };



            var param = conf["test"];
            Console.WriteLine(param["4"]);
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            send_socket.Connect();
        }

        private void btnListen_Click(object sender, EventArgs e)
        {
            recv_socket.Listen();
        }
    }
}
