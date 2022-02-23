using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TilesUnzip_Winform.UserControls
{
    public partial class ProgressControl : UserControl
    {
        private long startIndex = 0;
        private long endIndex = 0;
        private string connStr = "";

        public ProgressControl(long start, long end, string connStr)
        {
            InitializeComponent();
            this.startIndex = start;
            this.endIndex = end;
            this.connStr = connStr;
        }

        public void start()
        {

        }
    }
}
