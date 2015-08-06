using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Intel.Manageability.WSManagement;
using Intel.Manageability.Cim.Typed;
using System.Collections.ObjectModel;


namespace Common
{
    public partial class frmConnect : Form
    {

        private bool isConnected = false;
        private string cert_path = string.Empty;
        private IWSManClient wsmanClient = null;

        public frmConnect()
        {
            InitializeComponent();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            bool isSecured = false;
            string clientCert = null;
           
            if (chbTls.Checked)
            {
                clientCert = txtCertPass.Text;
                isSecured = true;
            }

            wsmanClient = new DotNetWSManClient(txtIp.Text, txtUser.Text, txtPassword.Text, isSecured, false, clientCert, null);
            List<KeyValuePair<string, string>> propertiesLst = new List<KeyValuePair<string, string>>();
            propertiesLst.Add(new KeyValuePair<string, string>("InstanceID", "AMT FW Core Version"));

            try
            {
                // Traversing the CIM_SoftwareIdentity instance representing the FW Core Version, using the managed host as a reference
                Collection<CimBase> SoftwareIdentity = AssociationTraversalTypedUtils.EnumerateAssociated(wsmanClient,
                                                       AssociationTraversalTypedUtils.DiscoverManagedHost(wsmanClient),
                                                       typeof(CIM_SoftwareIdentity),
                                                       typeof(CIM_ElementSoftwareIdentity), propertiesLst);
                string amtVersion=((CIM_SoftwareIdentity)SoftwareIdentity[0]).VersionString;
                isConnected = true;
                MessageBox.Show("Sucessful connected to a AMT FW Core Version: " + amtVersion);              
                this.Hide();            
            }
            catch (Exception ex) 
            {
                isConnected = false;
                MessageBox.Show("Unsucesful connection!. Error: " + ex.Message);
            }            
        }

        private void button3_Click(object sender, EventArgs e)
        {
            openFileDialog1.ShowDialog();

            if (openFileDialog1.FileName != string.Empty) cert_path = openFileDialog1.FileName;
        }

        private void chbTls_CheckedChanged(object sender, EventArgs e)
        {
            button3.Enabled = chbTls.Checked;
            txtCertPass.Enabled = chbTls.Checked;
        }

        public string GetUsername()
        {
            return txtUser.Text;
        }

        public string GetUserpass()
        {
            return txtPassword.Text;
        }

        public bool GetIsConnected()
        {
            return isConnected;
        }

        public IWSManClient GetWSManClient()
        {
            return wsmanClient;
        }
        
    }

}