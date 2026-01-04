using System;
using System.Drawing;
using System.Windows.Forms;

class UpdateDemo
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        ShowUpdateAvailableDialog();
    }
    
    static void ShowUpdateAvailableDialog()
    {
        var dialog = new Form
        {
            Text = "Update Available",
            Size = new Size(380, 280),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            StartPosition = FormStartPosition.CenterScreen,
            BackColor = Color.White,
            Font = new Font("Segoe UI", 9)
        };
        
        var titleLabel = new Label
        {
            Text = "New version available",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            Location = new Point(20, 20),
            AutoSize = true,
            ForeColor = Color.FromArgb(60, 60, 60)
        };
        
        var versionLabel = new Label
        {
            Text = "v2025.12.27.1  →  v2025.12.28.1",
            Font = new Font("Segoe UI", 10),
            Location = new Point(20, 50),
            AutoSize = true,
            ForeColor = Color.FromArgb(25, 118, 210)
        };
        
        var sizeLabel = new Label
        {
            Text = "Size: 5.4 MB",
            Font = new Font("Segoe UI", 8),
            Location = new Point(20, 75),
            AutoSize = true,
            ForeColor = Color.Gray
        };
        
        var notesBox = new TextBox
        {
            Text = "• Enhanced volume controls\n• Bug fixes and improvements\n• Performance optimizations",
            Font = new Font("Segoe UI", 9),
            Location = new Point(20, 105),
            Size = new Size(320, 80),
            Multiline = true,
            ReadOnly = true,
            BorderStyle = BorderStyle.None,
            BackColor = Color.FromArgb(248, 248, 248)
        };
        
        var downloadBtn = new Button
        {
            Text = "Download & Install",
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Location = new Point(20, 200),
            Size = new Size(150, 35),
            BackColor = Color.FromArgb(25, 118, 210),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        downloadBtn.FlatAppearance.BorderSize = 0;
        downloadBtn.Click += (s, e) => { dialog.Hide(); ShowProgressDialog(); };
        
        var laterBtn = new Button
        {
            Text = "Later",
            Font = new Font("Segoe UI", 9),
            Location = new Point(185, 200),
            Size = new Size(75, 35),
            BackColor = Color.FromArgb(240, 240, 240),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        laterBtn.FlatAppearance.BorderSize = 0;
        laterBtn.Click += (s, e) => Application.Exit();
        
        dialog.Controls.AddRange(new Control[] { titleLabel, versionLabel, sizeLabel, notesBox, downloadBtn, laterBtn });
        dialog.ShowDialog();
    }
    
    static void ShowProgressDialog()
    {
        var dialog = new Form
        {
            Text = "Downloading...",
            Size = new Size(380, 180),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            StartPosition = FormStartPosition.CenterScreen,
            BackColor = Color.White,
            Font = new Font("Segoe UI", 9)
        };
        
        var titleLabel = new Label
        {
            Text = "Downloading update",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Location = new Point(20, 20),
            AutoSize = true
        };
        
        var fileLabel = new Label
        {
            Text = "SofaRemote-Setup-v2025.12.28.1.exe",
            Font = new Font("Segoe UI", 8),
            Location = new Point(20, 50),
            AutoSize = true,
            ForeColor = Color.Gray
        };
        
        var progressBar = new ProgressBar
        {
            Location = new Point(20, 75),
            Size = new Size(320, 20),
            Style = ProgressBarStyle.Continuous,
            Value = 75
        };
        
        var progressLabel = new Label
        {
            Text = "75%  •  4.1 / 5.4 MB",
            Font = new Font("Segoe UI", 8),
            Location = new Point(20, 100),
            AutoSize = true,
            ForeColor = Color.FromArgb(100, 100, 100)
        };
        
        var cancelBtn = new Button
        {
            Text = "Cancel",
            Font = new Font("Segoe UI", 9),
            Location = new Point(140, 125),
            Size = new Size(80, 30),
            BackColor = Color.FromArgb(240, 240, 240),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        cancelBtn.FlatAppearance.BorderSize = 0;
        cancelBtn.Click += (s, e) => { dialog.Hide(); ShowCompleteDialog(); };
        
        dialog.Controls.AddRange(new Control[] { titleLabel, fileLabel, progressBar, progressLabel, cancelBtn });
        dialog.ShowDialog();
    }
    
    static void ShowCompleteDialog()
    {
        var dialog = new Form
        {
            Text = "Ready to Install",
            Size = new Size(380, 200),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            StartPosition = FormStartPosition.CenterScreen,
            BackColor = Color.White,
            Font = new Font("Segoe UI", 9)
        };
        
        var titleLabel = new Label
        {
            Text = "✓  Download complete",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            Location = new Point(20, 20),
            AutoSize = true,
            ForeColor = Color.FromArgb(76, 175, 80)
        };
        
        var infoLabel = new Label
        {
            Text = "v2025.12.28.1 is ready to install",
            Font = new Font("Segoe UI", 9),
            Location = new Point(20, 55),
            AutoSize = true,
            ForeColor = Color.FromArgb(100, 100, 100)
        };
        
        var noteLabel = new Label
        {
            Text = "App will close after clicking Install",
            Font = new Font("Segoe UI", 8),
            Location = new Point(20, 80),
            AutoSize = true,
            ForeColor = Color.Gray
        };
        
        var installBtn = new Button
        {
            Text = "Install Now",
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Location = new Point(20, 120),
            Size = new Size(110, 35),
            BackColor = Color.FromArgb(76, 175, 80),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        installBtn.FlatAppearance.BorderSize = 0;
        installBtn.Click += (s, e) => { MessageBox.Show("Would launch installer and close app!", "Demo", MessageBoxButtons.OK, MessageBoxIcon.Information); Application.Exit(); };
        
        var laterBtn = new Button
        {
            Text = "Later",
            Font = new Font("Segoe UI", 9),
            Location = new Point(145, 120),
            Size = new Size(70, 35),
            BackColor = Color.FromArgb(240, 240, 240),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        laterBtn.FlatAppearance.BorderSize = 0;
        laterBtn.Click += (s, e) => Application.Exit();
        
        var folderBtn = new Button
        {
            Text = "Open Folder",
            Font = new Font("Segoe UI", 8),
            Location = new Point(230, 120),
            Size = new Size(90, 35),
            BackColor = Color.FromArgb(240, 240, 240),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        folderBtn.FlatAppearance.BorderSize = 0;
        folderBtn.Click += (s, e) => MessageBox.Show("Would open Downloads folder", "Demo");
        
        dialog.Controls.AddRange(new Control[] { titleLabel, infoLabel, noteLabel, installBtn, laterBtn, folderBtn });
        dialog.ShowDialog();
    }
}
