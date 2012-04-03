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
        public static string save = "";
        public static List<Player> Players = new List<Player>();
        public static PollList polls;
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
            Order = 10;
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
            PollReader reader = new PollReader();
            save = Path.Combine(TShockAPI.TShock.SavePath, "Polls.cfg");

            if (File.Exists(save))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(polls.polls.Count + " polls have been loaded.");
                Console.ResetColor();
            }
            else
            {
                polls = reader.writeFile(save);
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("No polls found! Basic poll file being created. 3 example polls loaded.");
                Console.ResetColor();
            }
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
                if (polls.findPoll(name).PollName == name)
                {
                    OpenPoll = true;
                    CurrentPoll = polls.findPoll(name).PollName;
                }
            }
        }

        public static void GetResults(CommandArgs args)
        {

        }

        public static void Vote(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendMessage("Current Poll: " + CurrentPoll, Color.DarkCyan);
                args.Player.SendMessage(polls.findPoll(CurrentPoll).Question, Color.DarkCyan);
                args.Player.SendMessage("Vote Yes or No");
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
}