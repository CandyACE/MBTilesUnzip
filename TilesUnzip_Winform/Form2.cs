using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TilesUnzip_Winform
{
    public partial class Form2 : Form
    {
        private readonly IList<ZoomTaskInfo> _taskInfos;
        //public Queue<ZoomTaskInfo>

        public Form2(IList<ZoomTaskInfo> taskInfos)
        {
            InitializeComponent();
            _taskInfos = taskInfos;
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            foreach (var zoomTaskInfo in _taskInfos)
            {
                var add = checkedListBox1.Items.Add(zoomTaskInfo);
                checkedListBox1.SetItemChecked(add, true);
            }
        }

        private void btn_Start_Click(object sender, EventArgs e)
        {
            int taskNum = Convert.ToInt32(numTaskNum.Value);

            foreach (var zoomTaskInfo in _taskInfos)
            {
                if (zoomTaskInfo.Count <= taskNum * 100)
                {

                }
            }
        }
    }
}
