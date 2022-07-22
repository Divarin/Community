using System;
using System.Collections.Generic;

namespace miniBBS.TextFiles.Models
{
    public class Link
    {
        public string ActualFilename { get; set; }
        public string DisplayedFilename { get; set; }
        public bool IsDirectory => true == ActualFilename?.EndsWith(".html", StringComparison.CurrentCultureIgnoreCase);
        public string Path
        {
            get
            {
                if (string.IsNullOrWhiteSpace(ActualFilename))
                    return string.Empty;
                else
                {
                    int p = ActualFilename.LastIndexOf('/');
                    if (p < 0)
                        return ActualFilename;
                    string result = ActualFilename.Substring(0, p+1);
                    if (Parent != null)
                        result = Parent.Path + result;
                    return result;
                }
            }
        }

        /// <summary>
        /// Users who can edit this document (besides the owner)
        /// </summary>
        public ICollection<string> Editors { get; set; }
        public string Description { get; set; }
        public Link Parent { get; set; }

    }

    public class LinkComparer : IComparer<Link>
    {
        public int Compare(Link x, Link y)
        {
            return x.DisplayedFilename.CompareTo(y.DisplayedFilename);
        }
    }
}
