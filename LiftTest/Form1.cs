using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LiftControl;

namespace LiftTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Wise_DoorStateChanged(object sender, bool IsOpened)
        {
            MessageBox.Show(IsOpened + "");
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }
       
    }
}
