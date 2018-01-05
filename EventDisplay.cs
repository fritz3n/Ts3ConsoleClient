using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TS3Client
{
    public partial class EventDisplay : Form
    {
        public EventDisplay()
        {
            InitializeComponent();
        }
        

        public void Write(string text, Color? color = null)
        {
            consoleBox.SuspendLayout();
            consoleBox.SelectionColor = color ?? Color.White;
            consoleBox.AppendText(text);
            consoleBox.ScrollToCaret();
            consoleBox.ResumeLayout();
        }
    }
}
