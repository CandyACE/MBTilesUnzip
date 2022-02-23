using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LocaSpace.iDesktop;

namespace TilesUnzip_Winform
{
    public partial class Form1 : Form
    {
        private SQLiteDataReader reader;
        private Boolean isClose = false;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private Boolean CheckData(string filepath)
        {
            return File.Exists(filepath);
        }

        private void Form1_Shown(object sender, EventArgs e)
        {

        }

        private void Unzip(string dataPath)
        {
            if (!CheckData(dataPath))
            {
                MessageBox.Show("未找到数据，无法解压！！" + dataPath, "瓦片解压工具", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
                return;
            }

            SQLiteHelper sql = new SQLiteHelper();
            sql.SetConnectionString(dataPath, "");
            long length = long.Parse(sql.ExecuteScalar("select count(*) from tiles").ToString());

            reader = sql.ExecuteReader("select * from tiles");

            int bufferSize = 100;
            byte[] outbyte = new byte[bufferSize];
            long retval;
            long startIndex = 0;
            FileStream fs;
            BinaryWriter bw;
            long i = 0;

            while (!reader.IsClosed && reader.Read())
            {
                var level = reader.GetInt32(0);
                var x = reader.GetInt32(1);
                var y = reader.GetInt32(2);

                string outputFilePath = Path.Combine(
                    Environment.CurrentDirectory,
                    Path.GetFileNameWithoutExtension(dataPath),
                    $"{level}/{x}/{y}.png"
                );

                if (!Directory.Exists(Path.GetDirectoryName(outputFilePath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(outputFilePath));
                }

                fs = new FileStream(outputFilePath, FileMode.OpenOrCreate, FileAccess.Write);
                bw = new BinaryWriter(fs);

                startIndex = 0;

                retval = reader.GetBytes(3, startIndex, outbyte, 0, bufferSize);

                while (retval == bufferSize)
                {
                    bw.Write(outbyte);
                    bw.Flush();

                    startIndex += bufferSize;
                    retval = reader.GetBytes(3, startIndex, outbyte, 0, bufferSize);
                }

                bw.Write(outbyte, 0, (int)retval);
                bw.Flush();

                bw.Close();
                fs.Close();
                ++i;
                this.Invoke(new Action(() =>
                {
                    label1.Text = $"{level}/{x}/{y}.png";
                    label2.Text = $"({i}/{length})";
                    progressBar1.Maximum = 1000;
                    progressBar1.Value = Convert.ToInt32((i * 1.0 / length) * 1000);
                }));
            }

            reader.Close();
            MessageBox.Show("解压完成！", "瓦片解压工具", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (reader != null)
            {
                reader.Close();
                reader.Dispose();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Filter = "*.mbtiles|*.mbtiles";
            openFile.Multiselect = false;
            if (openFile.ShowDialog(this) == DialogResult.OK)
            {
                Task.Run(() =>
                {
                    Unzip(openFile.FileName);
                });
            }
        }
    }
}
