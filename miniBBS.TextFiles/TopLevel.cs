using miniBBS.TextFiles.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace miniBBS.TextFiles
{
    public static class TopLevel
    {
        public static IEnumerable<Link> GetLinks()
        {
            yield return new Link
            {
                ActualFilename = "100/index.html",
                Description = "Jason Scott's favorite 100",
                DisplayedFilename = "100"
            };

            yield return new Link
            {
                ActualFilename = "adventure/index.html",
                Description = "Walkthroughs and Hints for Text Adventures",
                DisplayedFilename = "Adventure"
            };

            yield return new Link
            {
                ActualFilename = "anarchy/index.html",
                Description = "Files that YOU SHOULD NOT FOLLOW",
                DisplayedFilename = "Anarchy"
            };

            yield return new Link
            {
                ActualFilename = "apple/index.html",
                Description = "Apple II Technical and Lore",
                DisplayedFilename = "Apple2s"
            };

            yield return new Link
            {
                ActualFilename = "art/index.html",
                Description = "Various ASCII Artwork/Illustrations",
                DisplayedFilename = "Artwork"
            };

            yield return new Link
            {
                ActualFilename = "bbs/index.html",
                Description = "Running and Using Bulletin Boards",
                DisplayedFilename = "BBS"
            };

            yield return new Link
            {
                ActualFilename = "computers/index.html",
                Description = "Generals Computer-Related Files",
                DisplayedFilename = "Computers"
            };

            yield return new Link
            {
                ActualFilename = "conspiracy/index.html",
                Description = "They're All Out to Get You!",
                DisplayedFilename = "Conspiracy"
            };

            yield return new Link
            {
                ActualFilename = "drugs/index.html",
                Description = "An unnecessary amount of Drug information",
                DisplayedFilename = "Drugs"
            };

            yield return new Link
            {
                ActualFilename = "etext/index.html",
                Description = "The Classics Meet ASCII",
                DisplayedFilename = "ETexts"
            };

            yield return new Link
            {
                ActualFilename = "food/index.html",
                Description = "Food and Eating",
                DisplayedFilename = "Food"
            };

            yield return new Link
            {
                ActualFilename = "fun/index.html",
                Description = "A Weird Grab Bag of Oddness",
                DisplayedFilename = "Fun"
            };

            yield return new Link
            {
                ActualFilename = "games/index.html",
                Description = "Information Files on Home and Arcade Games",
                DisplayedFilename = "Games"
            };

            yield return new Link
            {
                ActualFilename = "groups/index.html",
                Description = "Textfile Writer Collectives",
                DisplayedFilename = "Groups"
            };

            yield return new Link
            {
                ActualFilename = "hacking/index.html",
                Description = "The seamy underside of, well, Everything",
                DisplayedFilename = "Hack"
            };

            yield return new Link
            {
                ActualFilename = "hamradio/index.html",
                Description = "Ham Radio Operation Information, sort of",
                DisplayedFilename = "Ham Radio"
            };

            yield return new Link
            {
                ActualFilename = "holiday/index.html",
                Description = "Files evoking a Holiday Spirit",
                DisplayedFilename = "Holiday"
            };

            yield return new Link
            {
                ActualFilename = "humor/index.html",
                Description = "Many, many attempts at being Funny",
                DisplayedFilename = "Humor"
            };

            yield return new Link
            {
                ActualFilename = "internet/index.html",
                Description = "Text about the new fangled inter-network network",
                DisplayedFilename = "Internet"
            };

            yield return new Link
            {
                ActualFilename = "law/index.html",
                Description = "Text files recounting laws and their application",
                DisplayedFilename = "Law"
            };

            yield return new Link
            {
                ActualFilename = "magazines/index.html",
                Description = "Collections of 'E-Zines', including Phrack",
                DisplayedFilename = "Magazines"
            };

            yield return new Link
            {
                ActualFilename = "media/index.html",
                Description = "Television and Movie Minutae",
                DisplayedFilename = "MassMedia"
            };

            yield return new Link
            {
                ActualFilename = "messages/index.html",
                Description = "Samples of message bases from different BBSs",
                DisplayedFilename = "Messages"
            };

            yield return new Link
            {
                ActualFilename = "music/index.html",
                Description = "Files about Music or for Musicians",
                DisplayedFilename = "Music"
            };

            yield return new Link
            {
                ActualFilename = "news/index.html",
                Description = "Often poorly transcribed News Stories",
                DisplayedFilename = "News"
            };

            yield return new Link
            {
                ActualFilename = "Occult/index.html",
                Description = "Text files dealing with religions",
                DisplayedFilename = "Occult"
            };

            yield return new Link
            {
                ActualFilename = "phreak/index.html",
                Description = "Files about, from, and against the Phone Company.",
                DisplayedFilename = "Phreak"
            };

            yield return new Link
            {
                ActualFilename = "piracy/index.html",
                Description = "All Hail the Warez",
                DisplayedFilename = "Piracy"
            };

            yield return new Link
            {
                ActualFilename = "politics/index.html",
                Description = "Files of a Political Nature",
                DisplayedFilename = "Politics"
            };

            yield return new Link
            {
                ActualFilename = "programming/index.html",
                Description = "All of the Deep Geek Stuff",
                DisplayedFilename = "Programming"
            };

            yield return new Link
            {
                ActualFilename = "reports/index.html",
                Description = "Sheets for the Cheats",
                DisplayedFilename = "SchoolReports"
            };

            yield return new Link
            {
                ActualFilename = "rpg/index.html",
                Description = "Role Playing Games - Act like Someone else for Fun",
                DisplayedFilename = "RPGs"
            };

            yield return new Link
            {
                ActualFilename = "science/index.html",
                Description = "And not quite Science",
                DisplayedFilename = "Science"
            };
            
            yield return new Link
            {
                ActualFilename = "sf/index.html",
                Description = "Science Fiction reviews and lits",
                DisplayedFilename = "SciFi"
            };

            yield return new Link
            {
                ActualFilename = "sex/index.html",
                Description = "Files about trying to Make More of You",
                DisplayedFilename = "Sex"
            };

            yield return new Link
            {
                ActualFilename = "stories/index.html",
                Description = "BBS User-Written Fiction",
                DisplayedFilename = "Stories"
            };

            yield return new Link
            {
                ActualFilename = "survival/index.html",
                Description = "Be Suspicious, Be Worried, Be Prepared",
                DisplayedFilename = "Survival"
            };

            yield return new Link
            {
                ActualFilename = "ufo/index.html",
                Description = "Files indicating We Are Not Alone, or that We Are",
                DisplayedFilename = "UFO"
            };

            yield return new Link
            {
                ActualFilename = "uploads/index.html",
                Description = "(somewhat) more recent text files",
                DisplayedFilename = "Uploads"
            };

            yield return new Link
            {
                ActualFilename = "virus/index.html",
                Description = "Computer Viruses, Trojan Horses, and Worms",
                DisplayedFilename = "Viruses"
            };

            yield return new Link
            {
                ActualFilename = "users/index.html",
                Description = "Mutiny Community User Area",
                DisplayedFilename = "CommunityUsers"
            };
        }
    }
}
