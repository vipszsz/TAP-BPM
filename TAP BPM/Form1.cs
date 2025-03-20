using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Diagnostics;



namespace TapBPMApp
{
    public partial class Form1 : Form
    {


        // app  
        private DateTime lastTapTime = DateTime.MinValue; // Stores the time of the last tap
        private List<double> intervals = new List<double>(); // Stores time intervals between taps

        public Form1()
        {
            InitializeComponent();

            // Enable the form to capture key events
            this.KeyPreview = true;

            // Subscribe to the KeyUp event
            this.KeyDown += new KeyEventHandler(Form1_KeyUp);

            // Add random BG color
            SetRandomBackgroundColor();

            // Enable dragging
            this.MouseDown += new MouseEventHandler(Form1_MouseDown);

            // Key Press
            this.KeyDown += Form1_KeyDown;
            this.KeyUp += Form1_KeyUp;
        }


        private void SetRandomBackgroundColor()
        {
            // List of specific HEX color codes
            string[] hexColors = { "#8f9a9c", "#e2765a", "#e3d55a", "#855be1", "#5ebe74" };

            // Create a random number generator
            Random random = new Random();

            // Pick a random color from the array
            string randomHexColor = hexColors[random.Next(hexColors.Length)];

            // Convert the HEX color to a Color object
            this.BackColor = ColorTranslator.FromHtml(randomHexColor);
        }


        // PInvoke to enable dragging
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HTCAPTION = 0x2;
        [DllImport("User32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("User32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void buttonReset_Click(object sender, EventArgs e)
        {
            intervals.Clear();
            labelBPM.Text = "0 BPM";
        }

        private void labelTitle_Click(object sender, EventArgs e)
        {

        }


        private void pictureBoxReset_Click(object sender, EventArgs e)
        {
            intervals.Clear();
            labelBPM.Text = "0 BPM";
        }
        private void PictureBoxClose_MouseEnter(object sender, EventArgs e)
        {
            this.pictureBoxReset.BackColor = Color.Red; // Highlight in red when hovered
        }

        private void PictureBoxClose_MouseLeave(object sender, EventArgs e)
        {
            this.pictureBoxReset.BackColor = Color.Transparent; // Reset to transparent
        }



        private void closeApp_Click(object sender, EventArgs e)
        {
            Application.Exit(); // Closes the application
        }

        private const int MinTapInterval = 200; // Minimum time between taps in milliseconds


        private bool isKeyPressed = false; // Tracks if the key is already being handled

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            // Check if the key is already pressed to prevent multiple triggers
            if (!isKeyPressed && e.KeyCode == Keys.Space) // Replace Keys.Space with your desired key
            {
                isKeyPressed = true; // Mark the key as pressed
                HandleTap(); // Trigger the tap logic
            }
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            // Reset the key state when the key is released
            if (e.KeyCode == Keys.Space) // Replace Keys.Space with your desired key
            {
                isKeyPressed = false; // Mark the key as released
            }
        }

        // Custom method for tap logic (instead of PerformClick)
        private void HandleTap()
        {
            DateTime now = DateTime.Now;

            // If this is not the first tap
            if (lastTapTime != DateTime.MinValue)
            {
                double interval = (now - lastTapTime).TotalMilliseconds; // Time difference in milliseconds

                // Reset intervals if the delay between taps is too long
                if (interval > 2000) // Reset if the gap is more than 2 seconds
                {
                    intervals.Clear();
                }
                else
                {
                    intervals.Add(interval); // Add the interval to the list
                }
            }

            // Update the last tap time
            lastTapTime = now;

            // Calculate BPM if there are enough intervals
            if (intervals.Count > 0)
            {
                double averageInterval = intervals.Average(); // Get the average interval
                double bpm = 60000 / averageInterval; // Calculate BPM (60000ms = 1 minute)

                // Limit BPM to a maximum of 999
                if (bpm > 999)
                {
                    bpm = 999;
                }

                labelBPM.Text = $"{Math.Round(bpm)} BPM"; // Display BPM
            }
            else
            {
                labelBPM.Text = "0 BPM"; // Display "0 BPM" for the first tap
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            // Replace with your desired URL
            string url = "https://open.spotify.com/intl-pt/artist/63F0KeKFXQd5S4b3BKBfAI";

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true // Ensures the URL is opened in the default browser
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to open the link. Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
