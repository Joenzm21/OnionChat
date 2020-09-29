using Onion.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.ListView;

namespace Onion.Server
{
    public partial class Main : Form
    {
        public bool Running = false;
        public OnionManager manager;
        public System.Timers.Timer timer = new System.Timers.Timer();
        public string[][] lastpeerup, lastuserup;

        public Main()
        {
            InitializeComponent();
            timer.Elapsed += Timer_Elapsed;
            timer.Interval = 2000;
            timer.Start();
        }

        private void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            UpdatePeers();
            UpdateUsers();
            UpdateStatus();
        }
        private void UpdateStatus()
        {
            if (statusStrip.InvokeRequired)
                statusStrip.Invoke(new MethodInvoker(UpdateStatus));
            else
            {
                peerCountText.Text = "Peers: " + lastpeerup.Length;
                userCountText.Text = "Users: " + lastuserup.Length;
            }
        }
        private void UpdatePeers()
        {
            if (peerslistv.InvokeRequired)
                peerslistv.Invoke(new MethodInvoker(UpdatePeers));
            else if (manager != null && manager.PeerList != null)
            {
                if (manager.PeerList.Length == 0) return;
                bool needapply = false;
                string[][] text = new string[manager.PeerList.Length][];
                if (lastpeerup == null || text.Length != lastpeerup.Length)
                {
                    for (int i = 0; i < text.Length; i++)
                        text[i] = new string[] { "",
                        new IPAddress(manager.PeerList[i].IP).ToString() + ":" + manager.PeerList[i].Port.ToString(),
                        Convert.ToBase64String(manager.PeerList[i].Ed25519PUK) };
                    needapply = true;
                }
                else
                {
                    for (int i = 0; i < text.Length; i++)
                    {
                        text[i] = new string[] { "",
                        new IPAddress(manager.PeerList[i].IP).ToString() + ":" + manager.PeerList[i].Port.ToString(),
                        Convert.ToBase64String(manager.PeerList[i].Ed25519PUK) };
                        if (!needapply && text[i].SequenceEqual(lastpeerup[i]))
                            needapply = true;
                             
                    }
                }
                if (!needapply) return;
                lastpeerup = text;
                peerslistv.Items.Clear();
                foreach (string[] s in text)
                    peerslistv.Items.Add(new ListViewItem(s));
                peerslistv.Update();
                peerslistv.Refresh();
            }
        }

        private void UpdateUsers()
        {
            if (userslistv.InvokeRequired)
                userslistv.Invoke(new MethodInvoker(UpdateUsers));
            else if (manager != null && manager.PeerList != null)
            {
                if (manager.UserList.Count == 0) return;
                bool needapply = false;
                string[][] text = new string[manager.UserList.Count][];
                if (lastuserup == null || text.Length != lastuserup.Length)
                {
                    for (int i = 0; i < text.Length; i++)
                        text[i] = new string[] { "", manager.UserList[i].ID, Convert.ToBase64String(manager.UserList[i].Curve25519PUK) };
                    needapply = true;
                }
                else
                {
                    for (int i = 0; i < text.Length; i++)
                    {
                        text[i] = new string[] { "", manager.UserList[i].ID, Convert.ToBase64String(manager.UserList[i].Curve25519PUK) };
                        if (!needapply && text[i].SequenceEqual(lastuserup[i]))
                            needapply = true;

                    }
                }
                if (!needapply) return;
                lastuserup = text;
                userslistv.Items.Clear();
                foreach (string[] s in text)
                    userslistv.Items.Add(new ListViewItem(s));
                userslistv.Update();
                userslistv.Refresh();
            }
        }

        private void startastopb_Click(object sender, EventArgs e)
        {
            if (!Running)
            {
                manager = new OnionManager("config.ini", (ushort)portnum.Value, !localserver.Checked, upnp.Checked);
                startastopb.Text = "Stop";
                portnum.Enabled = false;
                Running = true;
                ipaddr.Text = "IP: " + manager.ExternalIP.ToString();
            }
            else if (manager != null)
            {
                manager.Dispose();
                startastopb.Text = "Start";
                portnum.Enabled = true;
                Running = false;
            }
        }

        private void localserver_CheckedChanged(object sender, EventArgs e)
        {
            upnp.Enabled = !localserver.Checked;
            if (!upnp.Enabled)
                upnp.Checked = false;
        }

        private void Main_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (manager != null)
                manager.Dispose();
        }

        private void connectb_Click(object sender, EventArgs e)
        {
            if (manager != null && Running)
                manager.Join(addresstb.Text);
            addresstb.Clear();
        }
    }
}
