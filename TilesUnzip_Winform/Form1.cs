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
using System.Windows.Forms.VisualStyles;
using LocaSpace.iDesktop;

namespace TilesUnzip_Winform
{
    public partial class Form1 : Form
    {
        private SQLiteDataReader reader;
        private Boolean isClose = false;
        private MBTilesUnZip unZip;

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
            MBTilesUnZip unzip = new MBTilesUnZip(dataPath, 3);
            unzip.Progrress += Unzip_Progrress;
            unzip.Run();
        }

        private void Unzip_Progrress(object sender, ProgressEventArgs e)
        {
            this.Invoke(new Action(() =>
            {
                label1.Text = $"{e.Info.Level}/{e.Info.X}/{e.Info.Y}.png";
                label2.Text = $"({e.Current}/{e.Total})";
                progressBar1.Maximum = 1000;
                progressBar1.Value = Convert.ToInt32((e.Current * 1.0 / e.Total) * 1000);
            }));
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
                //unZip = new MBTilesUnZip(openFile.FileName);
                //var zoomTaskInfos = unZip.GetZoomTaskInfo();
            }
        }
    }
}
