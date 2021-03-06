using Microsoft.Office.InfoPath;
using System;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;
using mshtml;

namespace InfoPath
{
    public partial class FormCode
    {
        public void InternalStartup()
        {
            EventManager.FormEvents.Submit += new SubmitEventHandler(FormEvents_Submit);
        }

        public void FormEvents_Submit(object sender, SubmitEventArgs e)
        {
            if (New)
                SaveAs(new Uri(Template.Uri, "./../" + Guid.NewGuid().ToString() + ".xml").AbsoluteUri);
            else
                Save();
            e.CancelableArgs.Cancel = Dirty;
        }
    }
}
