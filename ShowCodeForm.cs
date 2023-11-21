using System;
using System.IO;
using System.Text;
using System.Windows.Forms;


public class ShowCodeForm: Form
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        this.CopyButton = new System.Windows.Forms.Button();
        this.CodeBox = new System.Windows.Forms.RichTextBox();
        this.SaveButton = new System.Windows.Forms.Button();
        this.SuspendLayout();
        //
        // CopyButton
        //
        this.CopyButton.AutoSize =  true;
        this.CopyButton.Text =  "复制";
        this.CopyButton.Location = new System.Drawing.Point(16,568);
        this.CopyButton.Size = new System.Drawing.Size(77,30);
        this.CopyButton.TabIndex = 3;
        this.CopyButton.Click += new System.EventHandler(CopyButton_Click);
        //
        // CodeBox
        //
        this.CodeBox.SelectionBackColor = System.Drawing.SystemColors.Window;
        this.CodeBox.Location = new System.Drawing.Point(4,8);
        this.CodeBox.Size = new System.Drawing.Size(848,552);
        this.CodeBox.TabIndex = 4;
        //
        // SaveButton
        //
        this.SaveButton.AutoSize =  true;
        this.SaveButton.Text =  "保存";
        this.SaveButton.Location = new System.Drawing.Point(104,568);
        this.SaveButton.Size = new System.Drawing.Size(77,30);
        this.SaveButton.TabIndex = 5;
        this.SaveButton.Click += new System.EventHandler(SaveButton_Click);
        //
        // form
        //
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
        this.MaximizeBox =  false;
        this.Size = new System.Drawing.Size(876,652);
        this.Text =  "代码窗口";
        this.Controls.Add(this.CopyButton);
        this.Controls.Add(this.CodeBox);
        this.Controls.Add(this.SaveButton);
        this.ResumeLayout(false);
    } 

    #endregion 

    private System.Windows.Forms.Button CopyButton;
    private System.Windows.Forms.RichTextBox CodeBox;
    private System.Windows.Forms.Button SaveButton;

    private void CopyButton_Click(object sender, EventArgs e) {
        Clipboard.SetText(CodeBox.Text);
        MessageBox.Show("已复制至剪切板", "提示");
    }

    private void SaveButton_Click(object sender, EventArgs e) {
        var dialog = new OpenFileDialog();
        dialog.Multiselect = false;
        dialog.Title = "选择需要保存的文件";
        dialog.Filter = "cs文件(*.cs)|*.*";
        if (dialog.ShowDialog() == DialogResult.OK) {
            var path = dialog.FileName;
            using var fs = new FileStream(path, FileMode.Create);
            var content = Encoding.UTF8.GetBytes(CodeBox.Text);
            fs.Write(content, 0, content.Length);
        }
    }

    public ShowCodeForm(string code) {
        InitializeComponent();
        CodeBox.Text = code;
    }
}
