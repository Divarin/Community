using miniBBS.Extensions_String;
using miniBBS.TextFiles.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace miniBBS.TextFiles.Extensions
{
    public static class LinkExtensions
    {
        /// <summary>
        /// Returns true if the link is under the CommunityUsers directory, otherwise false.
        /// </summary>
        public static bool IsUserGeneratedContent(this Link link)
        {
            var parent = link;
            var previousParent = link;
            while (parent.Parent != null)
            {
                previousParent = parent;
                parent = parent.Parent;
            }
            
            if ("users/index.html".Equals(previousParent.ActualFilename, StringComparison.CurrentCultureIgnoreCase))
                return true;

            return false;
        }

        /// <summary>
        /// For links under the CommunityUsers directory, gets the owner of the file/directory
        /// </summary>
        public static string GetOwningUser(this Link link)
        {
            var parent = link;
            while (parent.Parent != null)
            {
                if ("users/index.html".Equals(parent.Parent.ActualFilename, StringComparison.CurrentCultureIgnoreCase))
                {
                    return parent.DisplayedFilename;
                }
                parent = parent.Parent;
            }
            return string.Empty;
        }

        public static bool IsOwnedByUser(this Link link, Core.Models.Data.User user)
        {
            return user.Name.Equals(GetOwningUser(link), StringComparison.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// Gets whether this user may edit the file (does not check if they are the owner)
        /// </summary>
        public static bool IsEditor(this Link link, Core.Models.Data.User user)
        {
            bool allowAll = true == link.Editors?.Any(c => "*".Equals(c, StringComparison.CurrentCultureIgnoreCase));
            bool allowThisUserSpecifically = true == link.Editors?.Any(c => user.Name.Equals(c, StringComparison.CurrentCultureIgnoreCase));
            bool rejectThisUserSpecifically = true == link.Editors?.Any(c => $"-{user.Name}".Equals(c, StringComparison.CurrentCultureIgnoreCase));
            bool result = allowThisUserSpecifically || (allowAll && !rejectThisUserSpecifically);
            return result;
        }

        /// <summary>
        /// Finds a link in the <paramref name="links"/> with the given <paramref name="filenameOrNumber"/>.  
        /// If <paramref name="requireExactMatch"/> is false then will return the first file that matches the name 
        /// regardless of extension.
        /// </summary>
        public static Link GetLink(this IList<Link> links, string filenameOrNumber, bool requireExactMatch = true)
        {
            Link link = null;
            if (int.TryParse(filenameOrNumber, out int n) && n >= 1 && n <= links.Count)
                link = links[n - 1];
            else
                link = links.FirstOrDefault(l => l.DisplayedFilename.Equals(filenameOrNumber, StringComparison.CurrentCultureIgnoreCase));
            
            if (link == null && !requireExactMatch)
                link = links.FirstOrDefault(l => l.DisplayedFilename.WithoutExtension().Equals(filenameOrNumber.WithoutExtension(), StringComparison.CurrentCultureIgnoreCase));

            return link;
        }
    }
}
