using System;
using System.Collections.Generic;
using System.Reflection;
using System.Drawing;
using Terraria;
using Hooks;
using TShockAPI;
using TShockAPI.DB;
using System.ComponentModel;
using System.IO;

namespace EasyVote
{
    #region Plugin Stuff
    [APIVersion(1, 11)]
    public class EasyVote : TerrariaPlugin
    {
        public static PollConfig getConfig { get; set; }
        internal static string getConfigPath { get { return Path.Combine(TShock.SavePath, "PollConfig.json"); } }
        public static List<Player> Players = new List<Player>();
        public override string Name
        {
            get { return "Easy Vote"; }
        }

        public override string Author
        {
            get { return "Spectrewiz"; }
        }

        public override string Description
        {
            get { return "Allows users to create polls and other users to vote"; }
        }

        public override Version Version
        {
            get { return new Version(0, 0, 1); }
        }

        public override void Initialize()
        {
            GameHooks.Update += OnUpdate;
            GameHooks.Initialize += OnInitialize;
            NetHooks.GreetPlayer += OnGreetPlayer;
            ServerHooks.Leave += OnLeave;
            ServerHooks.Chat += OnChat;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                GameHooks.Update -= OnUpdate;
                GameHooks.Initialize -= OnInitialize;
                NetHooks.GreetPlayer -= OnGreetPlayer;
                ServerHooks.Leave -= OnLeave;
                ServerHooks.Chat -= OnChat;
            }
            base.Dispose(disposing);
        }

        public EasyVote(Main game)
            : base(game)
        {
            Order = -1;
            getConfig = new PollConfig();
        }

        public void OnInitialize()
        {
            bool poll = false;

            foreach (Group group in TShock.Groups.groups)
            {
                if (group.Name != "superadmin")
                {
                    if (group.HasPermission("Poll"))
                        poll = true;
                }
            }

            List<string> permlist = new List<string>();
            if (!poll)
                permlist.Add("Poll");
            TShock.Groups.AddPermissions("trustedadmin", permlist);

            Commands.ChatCommands.Add(new Command("Poll", GetResults, "getvotes", "getresults", "findvotes", "findresults"));
            Commands.ChatCommands.Add(new Command("Poll", StartPoll, "startpoll"));
            Commands.ChatCommands.Add(new Command(Vote, "vote"));
            SetupConfig();
        }

        public void OnUpdate()
        {
        }

        public void OnGreetPlayer(int who, HandledEventArgs e)
        {
            lock (Players)
                Players.Add(new Player(who));
        }

        public void OnLeave(int ply)
        {
            lock (Players)
            {
                for (int i = 0; i < Players.Count; i++)
                {
                    if (Players[i].Index == ply)
                    {
                        Players.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        public void OnChat(messageBuffer msg, int ply, string text, HandledEventArgs e)
        {
        }

        public static void SetupConfig()
        {
            try
            {
                if (!File.Exists(getConfigPath))
                    NewConfig();
                getConfig = PollConfig.Read(getConfigPath);
                getConfig.Write(getConfigPath);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error in EasyVote config file");
                Console.ForegroundColor = ConsoleColor.Gray;
                Log.Error("Config Exception in EasyVote Config file");
                Log.Error(ex.ToString());
            }
        }

        public static void NewConfig()
        {
            File.WriteAllText(getConfigPath,
            "{" + Environment.NewLine +
            "  \"Polls\": [" + Environment.NewLine +
            "    {" + Environment.NewLine +
            "      \"Name\": \"Yes/No\"," + Environment.NewLine +
            "      \"Question\": \"Vote Yes or No.\"," + Environment.NewLine +
            "      \"TimeOfDay\": \"none\"," + Environment.NewLine +
            "      \"Monster\": \"none\"" + Environment.NewLine +
            "    }," + Environment.NewLine +
            "    {" + Environment.NewLine +
            "      \"Name\": \"Monster\"," + Environment.NewLine +
            "      \"Question\": \"Vote Yes or No for a dungeon guardian to spawn.\"," + Environment.NewLine +
            "      \"TimeOfDay\": \"none\"," + Environment.NewLine +
            "      \"Monster\": \"68\"" + Environment.NewLine +
            "    }," + Environment.NewLine +
            "    {" + Environment.NewLine +
            "      \"Name\": \"Day/Night\"," + Environment.NewLine +
            "      \"Question\": \"Vote Yes or No for it to be night.\"," + Environment.NewLine +
            "      \"TimeOfDay\": \"Night\"," + Environment.NewLine +
            "      \"Monster\": \"none\"" + Environment.NewLine +
            "    }" + Environment.NewLine +
            "  ]" + Environment.NewLine +
            "}");
        }
    #endregion
    #region Commands
        public static bool OpenPoll = false;
        public static string CurrentPoll;
        public static void StartPoll(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {

            }
            else
            {
                string name = args.Parameters[0].ToLower();
                foreach (var Poll in getConfig.Polls)
                {
                    if (Poll.Name.ToLower() == name)
                    {
                        OpenPoll = true;
                        CurrentPoll = Poll.Name;
                    }
                }
            }
        }

        public static void GetResults(CommandArgs args)
        {
            if ((args.Parameters[0] == "-l") || (args.Parameters[0] == "-long"))
            {
                int i = 0;
                int x = 0;
                foreach (Player player in EasyVote.Players)
                {
                    if (player.GetVoteResult() != Player.VoteResults.novote)
                    {
                        if (player.GetVoteResult() == Player.VoteResults.yes)
                        {
                            args.Player.SendMessage(string.Format("{0} voted Yes", player.TSPlayer.Name), Color.Cyan);
                            i++;
                        }
                        else
                        {
                            args.Player.SendMessage(string.Format("{0} voted No", player.TSPlayer.Name), Color.Cyan);
                            x++;
                        }
                    }
                }
                if (i > x)
                {
                    foreach (Player ply in EasyVote.Players)
                    {
                        if (ply.TSPlayer.Group.HasPermission("Poll"))
                        {
                            int Monster;
                            string TimeOfDay = null;
                            foreach (var Poll in getConfig.Polls)
                            {
                                if (Poll.Name == CurrentPoll)
                                {
                                    int.TryParse(Poll.Monster, out Monster);
                                    switch (Poll.TimeOfDay.ToLower())
                                    {
                                        case "day":
                                            TimeOfDay = "day";
                                            break;
                                        case "night":
                                            TimeOfDay = "night";
                                            break;
                                    }
                                    if (Monster != 0)
                                    {
                                        Commands.HandleCommand(ply.TSPlayer, string.Format("/spawnmob {0}", Monster));
                                    }
                                    if (TimeOfDay != null)
                                    {
                                        Commands.HandleCommand(ply.TSPlayer, string.Format("/time {0}", TimeOfDay));
                                    }
                                }
                            }
                            break;
                        }
                    }
                }
                CurrentPoll = null;
                OpenPoll = false;
            }
            else
            {
                int i = 0;
                int x = 0;
                foreach (Player player in EasyVote.Players)
                {
                    if (player.GetVoteResult() != Player.VoteResults.novote)
                    {
                        if (player.GetVoteResult() == Player.VoteResults.yes)
                        {
                            i++;
                        }
                        else
                        {
                            x++;
                        }
                    }
                }
                args.Player.SendMessage(i + " players voted Yes, and " + x + " players voted No.");
                if (i > x)
                {
                    foreach (Player ply in EasyVote.Players)
                    {
                        if (ply.TSPlayer.Group.HasPermission("Poll"))
                        {
                            int Monster;
                            string TimeOfDay = null;
                            foreach (var Poll in getConfig.Polls)
                            {
                                if (Poll.Name == CurrentPoll)
                                {
                                    int.TryParse(Poll.Monster, out Monster);
                                    switch (Poll.TimeOfDay.ToLower())
                                    {
                                        case "day":
                                            TimeOfDay = "day";
                                            break;
                                        case "night":
                                            TimeOfDay = "night";
                                            break;
                                    }
                                    if (Monster != 0)
                                    {
                                        Commands.HandleCommand(ply.TSPlayer, string.Format("/spawnmob {0}", Monster));
                                    }
                                    if (TimeOfDay != null)
                                    {
                                        Commands.HandleCommand(ply.TSPlayer, string.Format("/time {0}", TimeOfDay));
                                    }
                                }
                            }
                            break;
                        }
                    }
                }
                CurrentPoll = null;
                OpenPoll = false;
            }
        }

        public static void Vote(CommandArgs args)
        {
            string PollQuestion;
            if (args.Parameters.Count < 1)
            {
                args.Player.SendMessage("Current Poll: " + CurrentPoll, Color.DarkCyan);
                foreach (var Poll in getConfig.Polls)
                {
                    if (CurrentPoll == Poll.Name)
                    {
                        PollQuestion = Poll.Question;
                        args.Player.SendMessage(PollQuestion, Color.DarkCyan);
                        args.Player.SendMessage("Vote Yes or No", Color.DarkCyan);
                    }
                }
            }
            else if (OpenPoll == true)
            {
                var ListedPlayer = Player.GetPlayerByName(args.Player.Name);
                switch (args.Parameters[0].ToLower())
                {
                    case "yes":
                        args.Player.SendMessage("You voted Yes", Color.DarkCyan);
                        ListedPlayer.SetVote(Player.VoteResults.yes);
                        break;
                    case "no":
                        args.Player.SendMessage("You voted No", Color.DarkCyan);
                        ListedPlayer.SetVote(Player.VoteResults.no);
                        break;
                    default:
                        args.Player.SendMessage(string.Format("Invalid vote (your vote {0} did not match the possible votes: yes or no)", args.Parameters[0]), Color.Red);
                        break;
                }
            }
        }
    }
    #endregion
    public class Poll
    {
        public string Name;
        public string Question;
        public string TimeOfDay;
        public string Monster;

        public Poll(string Name, string Question, string TimeOfDay, string Monster)
        {
            this.Name = Name;
            this.Question = Question;
            this.TimeOfDay = TimeOfDay;
            this.Monster = Monster;
        }
    }
}