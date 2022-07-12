using miniBBS.TextFiles.Models;
using System;

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
                    return parent.DisplayedFilename;
                parent = parent.Parent;
            }
            return string.Empty;
        }

        public static bool IsOwnedByUser(this Link link, Core.Models.Data.User user)
        {
            return user.Name.Equals(GetOwningUser(link), StringComparison.CurrentCultureIgnoreCase);
        }
    }
}
