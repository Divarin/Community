using miniBBS.Core.Models.Control;

namespace miniBBS.TextFiles
{
    public static class Help
    {
        private const string _help =
@"*** Mutiny Community's Text Files Browser Help ***

What is this?

Archivist Jason Scott put together a large archive of text files from bulletin boards and internet of the 80's and 90's.  This archive was once browse-able through Mutiny BBS by way of a BBS door that utilized a gopher server, which was acting as a proxy to the web site textfiles.com.

In the summer of 2022 this web site went down.  Therefore the gopher server failed to deliver any content from textfiles.com, therefore the BBS door on Mutiny failed, as well, to deliver any content.

This system is a rebirth of the textfiles.com browsability on Mutiny (and now Community).  First off this is not a proxy to some other server which may or may not go down.  I have taken the time to use one of the mirrors (still operating at the time of this writing) of textfiles.com to obtain all of the texts and keep them accessible here on this board.

How do I use this?

This system is modelled after a DOS/*nix type command line interface (CLI).  It's not going to include all of the commands and you can't use fancy options like 'dir /w' and 'ls -la' or anything but it should be familiar enough to help get people going.

To change to a directory or to read a file you can simply type the name or number.There are edge cases where you will not be able to use the name but you will always be able to use the number.

Upon entering the name/number of the directory/file you will be shown the full description and asked if you want to either a) change to the directory or b) read the file.


Commands:
NOTE: Unlike the chat channels, commands issued in the textfiles browser should *NOT* be preceeded by a slash.

dir - Lists all of the [Directories] and Files in the current directory.Directories are [bracketed] whereas files are not.  The 'dir' command shows a snippet of the description of the directory/file.Before each entry is a number.  You can refer to directories and files by name or number, however there are edge cases where you will need to use the number such as the directory/filename is a number or it contains spaces.

ls - Similar to 'dir', lists the [Directories] and Files in the current directory but without the description snippets.  Also this is similar to 'dir /w' (in dos lingo) as it tries to squeeze multiple entries on one line.

grep - Exactly the same as 'dir' except when it comes to handing searches.

¿¿¿Searching: 'dir', 'ls', and 'grep' can be used to do searching. This is covered later, in the next section.

cd(directory name/number) - Changes to a directory.Unlike dos/*nix you can't change multiple levels, for example you can't use 'cd humor/jokes' instead you need to issue two separate 'cd' statements (one to get into 'humor' and another to get into 'jokes' which is under 'humor').
cd .. - goes up one directory to the parent directory
cd /, cd \ - goes back to the root directory

¿¿¿all 'cd' commands will bypass the description of the directory and just change to it.

read, type, cat, more, less (filename/number) - All of these commands do the same thing, reads a file.  This bypasses the description and just shows the file's contents.  

nonstop, ns - Similar to the commands above except the file is read out non-stop without 'more?' prompts.  This is useful if you're writing the file to a buffer file or sending it directly to a printer and don't want the 'more?' prompt to show up in the buffer or on the printout.

quit, exit - Exits this system

dnd - When you enter the Text Files browser from Community 'Do Not Disturb' mode is turned on automatically so that chats and other notifications from community don't interfere with your text files browsing.  However you can use the 'dnd' command to toggle Do Not Distrub mode on and off.  This way if you're just hanging out waiting for someone to chat with you can go into the text files browser, turn off DND mode, and read stuff while you wait.

chat - If you have DND mode off and you see someone chatting to you, and you want to send a quick response without actually leaving the text files browser then you can use the 'chat' command to send a chat to the channel.  You can't do everything that you can do in community for example you can't use the 'chat' command to change channels or send emotes but if you just want to type a message to the channel then the 'chat' command will let you do that:

¿¿¿CHAT Example
¿¿¿[TEXTS] />
¿¿¿Jimbob waves to Frankie

¿¿¿[TEXTS] /> chat Hi Frankie, just reading some texts!
¿¿¿Message 1234 posted to General.

¿¿¿[TEXTS] />


User Content - Only available in your user area under '/CommunityUsers/insertusernamehere' :

md, mkdir (directory name) - Creates a new sub-directory

edit, nano (filename) - Edits or creates a new file

del, rm (filename or number) - Deletes a file

deltree, rd, rmdir (directory name or number) - Deletes a sub-directory and anything in it.


Searching:
You can search the current directory using 'dir', 'ls', and 'grep' by passing search terms to these commands.  This will show you directories and/or files where their names, descriptions, or (in the case of files) content contain the keyword(s).

DIR  (search term(s)) - searches only filenames and descriptions
LS   (search term(s)) - searches only filenames
GREP (search term(s)) - searches only file contents

All searches only search the current directory, subdirectories are not searched.

You can use both OR and AND type searches and can combine them.  In general, keywords separated by spaces are treated as OR:
'dir apple banana'  - will list directories and files that have either 'apple' OR 'banana' in their names or descriptions.
'ls apple banana'   - will list directories and files that have either 'apple' OR 'banana' in their names.
'grep apple banana' - will list files that have either 'apple' OR 'banana' in the file's content.

An ampersand (&) character can be used to perform AND type searches.  You do this by separating keywords with the 'and' symbol (&) with no spaces between the words and the ampersand:
'dir apple&banana' - will list directories and files that have both 'apple' AND 'banana' in their names or descriptions.

You can also mix ORs and ANDs:
'grep apple banana cherry&strawberry' - well list files where the contents contain:
either 'apple' or 'banana' or BOTH 'cherry' and 'strawberry'";

        public static void Show(BbsSession session)
        {
            session.Io.OutputLine(_help.Replace("insertusernamehere", session.User.Name));
        }
    }
}
