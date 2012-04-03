using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using Terraria;
using System.IO;
using Newtonsoft.Json;

namespace EasyVote
{
    [Serializable]
    public class StandardPoll
    {
        public string PollName;
        public string Question;
        public string DayNight;
        public int Monster;

        public StandardPoll(string p, string i, string dn, int m)
        {
            PollName = p.ToLower();
            Question = i;
            DayNight = dn;
            Monster = m;
        }

        public string getName()
        {
            return PollName;
        }
    }

    public class PollList
    {
        public List<StandardPoll> polls;

        public PollList()
        {
            polls = new List<StandardPoll>();
        }

        public void AddItem(StandardPoll p)
        {
            polls.Add(p);
        }

        public StandardPoll findPoll(string name)
        {
            foreach (StandardPoll p in polls)
            {
                if (p.getName() == name)
                {
                    return p;
                }
            }
            return null;
        }
    }

    class PollReader
    {
        public PollList writeFile(string file)
        {
            TextWriter tw = new StreamWriter(file);

            PollList pollList = new PollList();

            pollList.AddItem(new StandardPoll("Yes/No", "Vote Yes or No.", "none", 0));
            pollList.AddItem(new StandardPoll("Night/Guardian", "Vote for a dungeon guardian and night time.", "Night", 68));
            pollList.AddItem(new StandardPoll("Day", "Vote for day time :]", "Day", 0));

            tw.Write(JsonConvert.SerializeObject(pollList, Formatting.Indented));
            tw.Close();

            return pollList;
        }

        public PollList readFile(string file)
        {
            TextReader tr = new StreamReader(file);
            string raw = tr.ReadToEnd();
            tr.Close();
            PollList pollList = JsonConvert.DeserializeObject<PollList>(raw);
            return pollList;
        }
    }
}