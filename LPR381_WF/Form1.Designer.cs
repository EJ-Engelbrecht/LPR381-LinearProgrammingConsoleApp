namespace LPR381_WF
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnSolve = new System.Windows.Forms.Button();
            this.btnClear = new System.Windows.Forms.Button();
            this.cbxAlgo = new System.Windows.Forms.ComboBox();
            this.btnExit = new System.Windows.Forms.Button();
            this.rtbOutput = new System.Windows.Forms.RichTextBox();
            this.lblAlgorithm = new System.Windows.Forms.Label();
            this.btnTextFile = new System.Windows.Forms.Button();
            this.btnSensitivity = new System.Windows.Forms.Button();
            this.btnExport = new System.Windows.Forms.Button();
            this.pgbShow = new System.Windows.Forms.ProgressBar();
            this.btnClearFile = new System.Windows.Forms.Button();
            this.cbxSensitivity = new System.Windows.Forms.ComboBox();
            this.lblSensitivity = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnSolve
            // 
            this.btnSolve.Location = new System.Drawing.Point(125, 409);
            this.btnSolve.Name = "btnSolve";
            this.btnSolve.Size = new System.Drawing.Size(82, 27);
            this.btnSolve.TabIndex = 0;
            this.btnSolve.Text = "Solve";
            this.btnSolve.UseVisualStyleBackColor = true;
            this.btnSolve.Click += new System.EventHandler(this.btnSolve_Click);
            // 
            // btnClear
            // 
            this.btnClear.Location = new System.Drawing.Point(213, 409);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(80, 27);
            this.btnClear.TabIndex = 1;
            this.btnClear.Text = "Clear Output";
            this.btnClear.UseVisualStyleBackColor = true;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // cbxAlgo
            // 
            this.cbxAlgo.FormattingEnabled = true;
            this.cbxAlgo.Items.AddRange(new object[] {
            "Primal Simplex",
            "Revised Primal Simplex",
            "Branch and Bound Simplex",
            "Cutting Plane",
            "Branch and Bound Knapsack"});
            this.cbxAlgo.Location = new System.Drawing.Point(112, 16);
            this.cbxAlgo.Name = "cbxAlgo";
            this.cbxAlgo.Size = new System.Drawing.Size(192, 21);
            this.cbxAlgo.TabIndex = 3;
            // 
            // btnExit
            // 
            this.btnExit.Location = new System.Drawing.Point(655, 411);
            this.btnExit.Name = "btnExit";
            this.btnExit.Size = new System.Drawing.Size(132, 25);
            this.btnExit.TabIndex = 4;
            this.btnExit.Text = "Exit";
            this.btnExit.UseVisualStyleBackColor = true;
            this.btnExit.Click += new System.EventHandler(this.btnExit_Click);
            // 
            // rtbOutput
            // 
            this.rtbOutput.Location = new System.Drawing.Point(27, 69);
            this.rtbOutput.Name = "rtbOutput";
            this.rtbOutput.Size = new System.Drawing.Size(760, 311);
            this.rtbOutput.TabIndex = 5;
            this.rtbOutput.Text = "";
            // 
            // lblAlgorithm
            // 
            this.lblAlgorithm.AutoSize = true;
            this.lblAlgorithm.Location = new System.Drawing.Point(27, 19);
            this.lblAlgorithm.Name = "lblAlgorithm";
            this.lblAlgorithm.Size = new System.Drawing.Size(58, 13);
            this.lblAlgorithm.TabIndex = 6;
            this.lblAlgorithm.Text = "Algorithms:";
            // 
            // btnTextFile
            // 
            this.btnTextFile.Location = new System.Drawing.Point(37, 409);
            this.btnTextFile.Name = "btnTextFile";
            this.btnTextFile.Size = new System.Drawing.Size(82, 27);
            this.btnTextFile.TabIndex = 7;
            this.btnTextFile.Text = "Add Text File";
            this.btnTextFile.UseVisualStyleBackColor = true;
            this.btnTextFile.Click += new System.EventHandler(this.btnTextFile_Click);
            // 
            // btnSensitivity
            // 
            this.btnSensitivity.Location = new System.Drawing.Point(299, 409);
            this.btnSensitivity.Name = "btnSensitivity";
            this.btnSensitivity.Size = new System.Drawing.Size(100, 27);
            this.btnSensitivity.TabIndex = 8;
            this.btnSensitivity.Text = "Sensitivity Analysis";
            this.btnSensitivity.UseVisualStyleBackColor = true;
            this.btnSensitivity.Click += new System.EventHandler(this.btnSensitivity_Click);
            // 
            // btnExport
            // 
            this.btnExport.Location = new System.Drawing.Point(405, 409);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(80, 27);
            this.btnExport.TabIndex = 9;
            this.btnExport.Text = "Export Results";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // pgbShow
            // 
            this.pgbShow.Location = new System.Drawing.Point(27, 48);
            this.pgbShow.Name = "pgbShow";
            this.pgbShow.Size = new System.Drawing.Size(760, 15);
            this.pgbShow.TabIndex = 10;
            this.pgbShow.Visible = false;
            // 
            // btnClearFile
            // 
            this.btnClearFile.Location = new System.Drawing.Point(491, 409);
            this.btnClearFile.Name = "btnClearFile";
            this.btnClearFile.Size = new System.Drawing.Size(80, 27);
            this.btnClearFile.TabIndex = 11;
            this.btnClearFile.Text = "Clear File";
            this.btnClearFile.UseVisualStyleBackColor = true;
            this.btnClearFile.Click += new System.EventHandler(this.btnClearFile_Click);
            // 
            // cbxSensitivity
            // 
            this.cbxSensitivity.FormattingEnabled = true;
            this.cbxSensitivity.Items.AddRange(new object[] {
            "Range of Non-Basic Variable",
            "Change Non-Basic Variable",
            "Range of Basic Variable",
            "Change Basic Variable",
            "Range of Constraint RHS",
            "Change Constraint RHS",
            "Add New Activity",
            "Add New Constraint",
            "Display Shadow Prices",
            "Duality Analysis"});
            this.cbxSensitivity.Location = new System.Drawing.Point(592, 16);
            this.cbxSensitivity.Name = "cbxSensitivity";
            this.cbxSensitivity.Size = new System.Drawing.Size(192, 21);
            this.cbxSensitivity.TabIndex = 12;
            // 
            // lblSensitivity
            // 
            this.lblSensitivity.AutoSize = true;
            this.lblSensitivity.Location = new System.Drawing.Point(473, 19);
            this.lblSensitivity.Name = "lblSensitivity";
            this.lblSensitivity.Size = new System.Drawing.Size(98, 13);
            this.lblSensitivity.TabIndex = 13;
            this.lblSensitivity.Text = "Sensitivity Analysis:";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.lblSensitivity);
            this.Controls.Add(this.cbxSensitivity);
            this.Controls.Add(this.btnClearFile);
            this.Controls.Add(this.pgbShow);
            this.Controls.Add(this.btnExport);
            this.Controls.Add(this.btnSensitivity);
            this.Controls.Add(this.btnTextFile);
            this.Controls.Add(this.lblAlgorithm);
            this.Controls.Add(this.rtbOutput);
            this.Controls.Add(this.btnExit);
            this.Controls.Add(this.cbxAlgo);
            this.Controls.Add(this.btnClear);
            this.Controls.Add(this.btnSolve);
            this.Name = "Form1";
            this.Text = "Linear Programming Solver - LPR381 Project";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnSolve;
        private System.Windows.Forms.Button btnClear;
        private System.Windows.Forms.ComboBox cbxAlgo;
        private System.Windows.Forms.Button btnExit;
        private System.Windows.Forms.RichTextBox rtbOutput;
        private System.Windows.Forms.Label lblAlgorithm;
        private System.Windows.Forms.Button btnTextFile;
        private System.Windows.Forms.Button btnSensitivity;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.ProgressBar pgbShow;
        private System.Windows.Forms.Button btnClearFile;
        private System.Windows.Forms.ComboBox cbxSensitivity;
        private System.Windows.Forms.Label lblSensitivity;
    }
}

