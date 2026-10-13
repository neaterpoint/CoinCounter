using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CoinCounter
{
    public partial class Form1 : Form
    {
        Bitmap image;
        public Form1()
        {
            InitializeComponent();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveFileDialog1.ShowDialog();
        }

        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            image = new Bitmap(openFileDialog1.FileName);
            pictureBox1.Image = image;
        }

        private void saveFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            //TODO: Implement save functionality
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if (image == null) return;

            // Disable button during scan to prevent concurrent runs
            button1.Enabled = false;

            // Clear previous static data
            CoinFinder.Reset();

            // Create a copy of the original image for visualization in pictureBox2
            Bitmap visualizationBitmap = new Bitmap(image);
            pictureBox2.Image = visualizationBitmap;

            // Define the callback that CoinFinder will use to update the PictureBox
            Action<Bitmap> updateUI = (bmp) =>
            {
                pictureBox2.Image = bmp;
                pictureBox2.Refresh(); // Forces the UI to redraw immediately
            };

            // Await the processing task
            await CoinFinder.ProcessImageAsync(image, visualizationBitmap, updateUI);

            // Update labels only after the entire image finishes scanning
            Lbl5_centavo.Text = CoinFinder.CoinCounts["5 Centavo"].ToString();
            Lbl10_centavo.Text = CoinFinder.CoinCounts["10 Centavo"].ToString();
            Lbl25_centavo.Text = CoinFinder.CoinCounts["25 Centavo"].ToString();
            Lbl1_peso.Text = CoinFinder.CoinCounts["1 Peso"].ToString();
            Lbl5_peso.Text = CoinFinder.CoinCounts["5 Peso"].ToString();
            Lbl10_peso.Text = CoinFinder.CoinCounts["10 Peso"].ToString();
            Lbltotal.Text = CoinFinder.TotalValue.ToString("C");

            button1.Enabled = true;
        }

    }
}
