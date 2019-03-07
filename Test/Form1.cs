using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using AgvControlSystem;
using SocketCommunication;

namespace Test
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            AgvDispatcher dispatcher = new AgvDispatcher(AgvIpTextBox.Text);
            try
            {
                dispatcher.Start();
                listBox1.Items.Add("服务器已启动");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            AgvDispatcher.AgvOne.ConnectionChanged += BackgroundChange;
            AgvDispatcher.AgvOne.ConnectionChanged += AgvConnectionChanged;
            AgvDispatcher.AgvOne.ResponseNotified += ReportCommandState;
            AgvDispatcher.SocketServerWarehouse.ConnectionChanged += HostConnectionChanged;
        }

        #region report
        /// <summary>
        /// report command excute state
        /// </summary>
        private void ReportCommandState(object sender, AgvResponse agvResponse)
        {
            listBox1.Items.Add(agvResponse.ResponseMsg);
        }

        /// <summary>
        /// if host connection state changed,report
        /// </summary>
        private void HostConnectionChanged(object sender, bool connected)
        {
            if (connected == true)
            {
                listBox1.Items.Add("客户端：" + AgvDispatcher.SocketServerWarehouse.EndPoint + "连接成功");
            }
            else
            {
                listBox1.Items.Add("客户端：" + AgvDispatcher.SocketServerWarehouse.EndPoint + "已断开连接");
            }
        }

        /// <summary>
        /// if AGV connection state changed,report
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="connected"></param>
        private void AgvConnectionChanged(object sender, bool connected)
        {
            if (connected == true)
            {
                listBox1.Items.Add("AGV连接成功");
                button1.Text = "启动成功";
            }
            else
            {
                listBox1.Items.Add("AGV已断开连接");
                button1.Text = "启动";
            }
        }

        private void BackgroundChange(object sender, bool connected)
        {
            if (connected == true)
            {
                button1.BackColor = SystemColors.HotTrack;
            }
            else button1.BackColor = SystemColors.Control;
        }
        #endregion

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                AgvDispatcher.AgvOne.Start();
            }
            catch (Exception)
            {

                MessageBox.Show("与小车通讯失败，请检查电脑与小车的网络连接情况");
            }
            //LiftControl.Lift lift = new LiftControl.Lift();            
            //lift.ResetOutput();
            //lift.CloseDoor();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            AgvCommand agvCommand = new AgvCommand();
            agvCommand.CommandType = AgvControlSystem.CommandType.RESET;
            agvCommand.RawMessage = "1,RESET,1";
            //AgvDispatcher.AgvOne.AddCommand(agvCommand);
            AgvDispatcher.AgvOne.Reset(agvCommand);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                AgvDispatcher.AgvOne.ResetError();
            }
            catch (Exception)
            {
                MessageBox.Show("与小车通讯失败，请检查电脑与小车的网络连接情况");
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                AgvCommand agvCommand = new AgvCommand();
                agvCommand.CommandType = AgvControlSystem.CommandType.MOVE;
                agvCommand.RawMessage = "1,MOVE,1,1,101";
                agvCommand.Arg = new string[2] { "1", "101" };
                try
                {
                    AgvDispatcher.AgvOne.AddCommand(agvCommand);
                }
                catch (Exception)
                {
                    MessageBox.Show("与小车通讯失败，请检查电脑与小车的网络连接情况");
                }
            });
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Task.Factory.StartNew(()=> {
                AgvCommand agvCommand = new AgvCommand();
                agvCommand.CommandType = AgvControlSystem.CommandType.MOVE;
                agvCommand.RawMessage = "1,MOVE,1,1,0";
                agvCommand.Arg = new string[2] { "1", "0" };
                try
                {
                    AgvDispatcher.AgvOne.AddCommand(agvCommand);
                }
                catch (Exception)
                {
                    MessageBox.Show("与小车通讯失败，请检查电脑与小车的网络连接情况");
                }
            });
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Environment.Exit(0);
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndices.Count > 0)
            {
                toolTip1.Active = true;

                toolTip1.SetToolTip(listBox1, listBox1.Items[listBox1.SelectedIndex].ToString());
            }
            else
            {
                toolTip1.Active = false;
            }
        }

    }
}
