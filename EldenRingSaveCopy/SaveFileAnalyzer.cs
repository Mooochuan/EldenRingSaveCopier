using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace EldenRingSaveCopy
{
    public class SaveFileAnalyzer : Form
    {
        private TextBox hexViewer;
        private TextBox decodedViewer;
        private Button loadButton;
        private Button scanButton;
        private Label offsetLabel;
        private TrackBar offsetSlider;
        private byte[] fileData;

        public SaveFileAnalyzer()
        {
            this.Text = "Save File Analyzer";
            this.Size = new Size(1200, 800);

            // Create controls
            loadButton = new Button
            {
                Text = "Load Save File",
                Location = new Point(10, 10),
                Size = new Size(100, 30)
            };
            loadButton.Click += LoadButton_Click;

            scanButton = new Button
            {
                Text = "Scan for Names",
                Location = new Point(120, 10),
                Size = new Size(100, 30)
            };
            scanButton.Click += ScanButton_Click;

            offsetLabel = new Label
            {
                Text = "Offset: 0x0",
                Location = new Point(230, 15),
                Size = new Size(200, 20)
            };

            offsetSlider = new TrackBar
            {
                Location = new Point(440, 10),
                Size = new Size(400, 45),
                Minimum = 0,
                Maximum = 1000,
                Value = 0
            };
            offsetSlider.ValueChanged += OffsetSlider_ValueChanged;

            hexViewer = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Location = new Point(10, 50),
                Size = new Size(580, 700),
                Font = new Font("Consolas", 10),
                WordWrap = false
            };

            decodedViewer = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Both,
                Location = new Point(600, 50),
                Size = new Size(580, 700),
                Font = new Font("Consolas", 10),
                WordWrap = false
            };

            // Add controls to form
            this.Controls.AddRange(new Control[] { loadButton, scanButton, offsetLabel, offsetSlider, hexViewer, decodedViewer });
        }

        private void LoadButton_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Save Files |*.sl2";
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        fileData = File.ReadAllBytes(openFileDialog.FileName);
                        offsetSlider.Maximum = fileData.Length - 1;
                        UpdateView();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading file: {ex.Message}");
                    }
                }
            }
        }

        private void ScanButton_Click(object sender, EventArgs e)
        {
            if (fileData == null) return;

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < fileData.Length - 2; i++)
            {
                if (fileData[i] != 0 && fileData[i + 1] == 0)
                {
                    // Found potential start of a Unicode string
                    byte[] potentialName = new byte[0x22];
                    if (i + 0x22 <= fileData.Length)
                    {
                        Array.Copy(fileData, i, potentialName, 0, 0x22);
                        string name = Encoding.Unicode.GetString(potentialName).TrimEnd('\0');
                        
                        // Only show if it looks like a real name
                        if (name.Length >= 2 && name.All(c => char.IsLetterOrDigit(c) || char.IsPunctuation(c) || char.IsWhiteSpace(c)))
                        {
                            sb.AppendLine($"\nPotential character at offset 0x{i:X}:");
                            sb.AppendLine($"Name: {name}");
                            sb.AppendLine($"Raw bytes: {BitConverter.ToString(potentialName)}");
                            
                            // Show surrounding bytes
                            int start = Math.Max(0, i - 16);
                            int length = Math.Min(48, fileData.Length - start);
                            byte[] surrounding = new byte[length];
                            Array.Copy(fileData, start, surrounding, 0, length);
                            sb.AppendLine($"Surrounding bytes: {BitConverter.ToString(surrounding)}");
                        }
                    }
                }
            }
            decodedViewer.Text = sb.ToString();
        }

        private void OffsetSlider_ValueChanged(object sender, EventArgs e)
        {
            UpdateView();
        }

        private void UpdateView()
        {
            if (fileData == null) return;

            int offset = offsetSlider.Value;
            offsetLabel.Text = $"Offset: 0x{offset:X}";

            // Show hex view
            StringBuilder hexSb = new StringBuilder();
            for (int i = 0; i < 32; i++)
            {
                if (offset + i < fileData.Length)
                {
                    hexSb.Append($"{fileData[offset + i]:X2} ");
                }
            }
            hexViewer.Text = hexSb.ToString();

            // Show decoded view
            StringBuilder decodedSb = new StringBuilder();
            for (int i = 0; i < 32; i++)
            {
                if (offset + i < fileData.Length)
                {
                    byte b = fileData[offset + i];
                    decodedSb.Append(b >= 32 && b <= 126 ? (char)b : '.');
                }
            }
            decodedViewer.Text = decodedSb.ToString();
        }
    }
} 