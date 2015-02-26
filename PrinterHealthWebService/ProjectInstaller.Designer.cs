using System.Configuration.Install;
using System.ServiceProcess;

namespace PrinterHealthWebService
{
    partial class ProjectInstaller
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

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.PrinterHealthWebServiceProcessInstaller = new ServiceProcessInstaller();
            this.PrinterHealthWebServiceInstaller = new ServiceInstaller();
            //
            // PrinterHealthWebServiceProcessInstaller
            //
            this.PrinterHealthWebServiceProcessInstaller.Account = ServiceAccount.User;
            this.PrinterHealthWebServiceProcessInstaller.Password = null;
            this.PrinterHealthWebServiceProcessInstaller.Username = null;
            //
            // PrinterHealthWebServiceInstaller
            //
            this.PrinterHealthWebServiceInstaller.Description =
                "Monitors printers and consolidates their status information in a web frontend.";
            this.PrinterHealthWebServiceInstaller.DisplayName = "Printer Health Web";
            this.PrinterHealthWebServiceInstaller.ServiceName = "PrinterHealthWebService";
            this.PrinterHealthWebServiceInstaller.StartType = ServiceStartMode.Automatic;
            //
            // ProjectInstaller
            this.Installers.AddRange(new Installer[]
            {
                this.PrinterHealthWebServiceProcessInstaller,
                this.PrinterHealthWebServiceInstaller
            });
        }

        #endregion

        private ServiceProcessInstaller PrinterHealthWebServiceProcessInstaller;
        private ServiceInstaller PrinterHealthWebServiceInstaller;
    }
}