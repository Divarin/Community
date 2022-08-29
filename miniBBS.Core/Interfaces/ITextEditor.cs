using miniBBS.Core.Models.Control;
using System;

namespace miniBBS.Core.Interfaces
{
    public interface ITextEditor
    {
        void EditText(BbsSession session, LineEditorParameters parameters = null);

        /// <summary>
        /// Func that takes the text body, saves it (somehow), and returns a status message such as 
        /// "saved as 'myfile.txt'" or something.
        /// </summary>
        Func<string, string> OnSave { get; set; }
    }
}
