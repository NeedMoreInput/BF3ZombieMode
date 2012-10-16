/*  CZombieMode - Copyright 2012 m4xx

 */

using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;
using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Players;

enum NoticeDisplayType { yell, say };

namespace PRoConEvents
{
	
	public class CZombieMode : PRoConPluginAPI, IPRoConPluginInterface
	{
		#region Constants

		const string HUMAN_TEAM = "1";

		const string ZOMBIE_TEAM = "2";

		const string BLANK_SQUAD = "0";

		const string FORCE_MOVE = "true";

		#endregion

		#region PluginSettings
		
		private int DebugLevel = 3; // 3 while in development, 2 when released

		private string CommandPrefix = "!zombie";

		private int AnnounceDisplayLength = 10;

		private NoticeDisplayType AnnounceDisplayType = NoticeDisplayType.yell;

		private int WarningDisplayLength = 15;

		private List<String> AdminUsers = new List<String>();

		private List<String> PlayerKickQueue = new List<String>();

		private ZombieModeKillTracker KillTracker = new ZombieModeKillTracker();
		
		private bool RematchEnabled = true; // true: round does not end, false: round ends
		
		private int HumanMaxIdleSeconds = 2*60; // aggressively kick idle humans
		
		private int MaxIdleSeconds = 10*60; // maximum idle for any player
		
		private int WarnsBeforeKickForRulesViolations = 1;
		
		private bool NewPlayersJoinHumans = true;

		#endregion


		#region GamePlayVars

		private List<CPlayerInfo> PlayerList = new List<CPlayerInfo>();

		private bool ZombieModeEnabled = true;

		private int MaxPlayers = 32;

		private int MinimumHumans = 3;

		private int MinimumZombies = 1;

		private int DeathsNeededToBeInfected = 1;

		//private int ZombiesKilledToSurvive = 50;

		private bool ZombieKillLimitEnabled = true;

		private bool InfectSuicides = true;
		
		private List<String> TeamHuman = new List<String>();
		
		private List<String> TeamZombie = new List<String>();
		
		private List<String> FreshZombie = new List<String>();
		
		private List<String> PatientZeroes = new List<String>();
			/* PatientZeroes keeps track of all the players that have been selected to
			   be the first zombie, to prevent the same player from being selected
			   over and over again. */
		
		private int KnownPlayerCount = 0;
		
		private int ServerSwitchedCount = 0;
		
		private List<String> Lottery = new List<String>();
			/* Pool of players to select first zombie from */
			
		private string PatientZero = null; // name of first zombie for the round
		
		private ZombieModePlayerState PlayerState = new ZombieModePlayerState();
		
		private SynchronizedNumbers NumRulesThreads  = new SynchronizedNumbers();
		
		private enum GState { 
			Idle,			// No players, no match in progress, or just reset
			Waiting, 		// Waiting for minimum number of players to spawn
			Playing, 		// Playing a match
			CountingDown,	// Match over, counting down to next round/match
			BetweenRounds,	// Between map levels/rounds
			NeedSpawn		// Ready to play next match, waiting for spawn
		};
		
		private GState GameState = GState.Idle;
		
		private GState OldGameState = GState.BetweenRounds;
		
		private DescriptionClass Description = new DescriptionClass();

		#endregion


		#region DamagePercentageVars

		int Against1Or2Zombies = 5;  // 3+ to 1 ratio humans:zombies

		int AgainstAFewZombies = 10; // 3:1 to 3:2 ratio humans:zombies

		int AgainstEqualNumbers = 15; // 3:2 to 2:3 ratio humans:zombies

		int AgainstManyZombies = 30; // 2:3 to 1:4 ratio humans:zombies

		int AgainstCountlessZombies = 100; // 1 to 4+ ratio humans:zombies
		
		int BulletDamage = 100; // Current setting

		#endregion

		#region HumanVictoryVars

		int KillsIf8OrLessPlayers = 20;

		int KillsIf12To9Players = 25;

		int KillsIf16To13Players = 30;

		int KillsIf20To17Players = 40;

		int KillsIf24To21Players = 50;

		int KillsIf28To25Players = 60;
		
		int KillsIf32To29Players = 70;

		#endregion

		private string[] ZombieWeapons = 
	    {
	        "Melee",
	        "Defib",
	        "Knife_RazorBlade",
	        "Knife",
	        "Repair Tool"
	    };

		#region WeaponList
		private List<String> WeaponList = new List<String>(new string[] {
            "870MCS",
            "AEK-971",
            "AKS-74u",
            "AN-94 Abakan",
            "AS Val",
            "DAO-12",
            "Defib",
            "F2000",
            "FAMAS",
            "FGM-148",
            "FIM92",
            "Glock18",
            "HK53",
            "jackhammer",
            "JNG90",
            "Knife_RazorBlade",
            "L96",
            "LSAT",
            "M416",
            "M417",
            "M1014",
            "M15 AT Mine",
            "M16A4",
            "M1911",
            "M240",
            "M249",
            "M26Mass",
            "M27IAR",
            "M320",
            "M39",
            "M40A5",
            "M4A1",
            "M60",
            "M67",
            "M9",
            "M93R",
            "Melee",
            "MG36",
            "Mk11",
            "Model98B",
            "MP7",
            "Pecheneg",
            "PP-19",
            "PP-2000",
            "QBB-95",
            "QBU-88",
            "QBZ-95",
            "Repair Tool",
            "RoadKill",
            "RPG-7",
            "RPK-74M",
            "SCAR-L",
            "SG 553 LB",
            "Siaga20k",
            "SKS",
            "SMAW",
            "SPAS-12",
            "SV98",
            "SVD",
            "Steyr AUG",
            "Taurus .44",
            "Type88",
            "USAS-12",
            "Weapons/A91/A91",
            "Weapons/AK74M/AK74",
            "Weapons/G36C/G36C",
            "Weapons/G3A3/G3A3",
            "Weapons/Gadgets/C4/C4",
            "Weapons/Gadgets/Claymore/Claymore",
            "Weapons/KH2002/KH2002",
            "Weapons/Knife/Knife",
            "Weapons/MagpulPDR/MagpulPDR",
            "Weapons/MP412Rex/MP412REX",
            "Weapons/MP443/MP443",
            "Weapons/MP443/MP443_GM",
            "Weapons/P90/P90",
            "Weapons/P90/P90_GM",
            "Weapons/Sa18IGLA/Sa18IGLA",
            "Weapons/SCAR-H/SCAR-H",
            "Weapons/UMP45/UMP45",
            "Weapons/XP1_L85A2/L85A2",
            "Weapons/XP2_ACR/ACR",
            "Weapons/XP2_L86/L86",
            "Weapons/XP2_MP5K/MP5K",
            "Weapons/XP2_MTAR/MTAR"
        });
		#endregion

		#region ZombieWeaponList
		private List<String> ZombieWeaponsEnabled = new List<String>(new string[] {
			"Repair Tool",
			"Defib",
			"Melee",
			"Knife_RazorBlade",
			"Weapons/Knife/Knife"
		});
		#endregion

		#region HumanWeaponList
		private List<String> HumanWeaponsEnabled = new List<String>(new string[] {
			"870MCS",
            "AEK-971",
            "AKS-74u",
            "AN-94 Abakan",
            "AS Val",
            "DAO-12",
            "Defib",
            "F2000",
            "FAMAS",
            "FGM-148",
            "FIM92",
            "Glock18",
            "HK53",
            "jackhammer",
            "JNG90",
            "Knife_RazorBlade",
            "L96",
            "LSAT",
            "M416",
            "M417",
            "M1014",
            // Off: "M15 AT Mine",
            "M16A4",
            "M1911",
            "M240",
            "M249",
            "M26Mass",
            "M27IAR",
            // Off: "M320",
            "M39",
            "M40A5",
            "M4A1",
            "M60",
            // Off: "M67",
            "M9",
            "M93R",
            "Melee",
            "MG36",
            "Mk11",
            "Model98B",
            "MP7",
            "Pecheneg",
            "PP-19",
            "PP-2000",
            "QBB-95",
            "QBU-88",
            "QBZ-95",
            "Repair Tool",
            "RoadKill",
            // Off: "RPG-7",
            "RPK-74M",
            "SCAR-L",
            "SG 553 LB",
            "Siaga20k",
            "SKS",
            // Off: "SMAW",
            "SPAS-12",
            "SV98",
            "SVD",
            "Steyr AUG",
            "Taurus .44",
            "Type88",
            "USAS-12",
            "Weapons/A91/A91",
            "Weapons/AK74M/AK74",
            "Weapons/G36C/G36C",
            "Weapons/G3A3/G3A3",
            // Off: "Weapons/Gadgets/C4/C4",
            // Off: "Weapons/Gadgets/Claymore/Claymore",
            "Weapons/KH2002/KH2002",
            "Weapons/Knife/Knife",
            "Weapons/MagpulPDR/MagpulPDR",
            "Weapons/MP412Rex/MP412REX",
            "Weapons/MP443/MP443",
            "Weapons/MP443/MP443_GM",
            "Weapons/P90/P90",
            "Weapons/P90/P90_GM",
            "Weapons/Sa18IGLA/Sa18IGLA",
            "Weapons/SCAR-H/SCAR-H",
            "Weapons/UMP45/UMP45",
            "Weapons/XP1_L85A2/L85A2",
            "Weapons/XP2_ACR/ACR",
            "Weapons/XP2_L86/L86",
            "Weapons/XP2_MP5K/MP5K",
            "Weapons/XP2_MTAR/MTAR"
		});
		#endregion

		#region EventHandlers

		/** EVENT HANDLERS **/
		public override void OnPlayerKickedByAdmin(string SoldierName, string reason) 
		{
			if (ZombieModeEnabled == false)
				return;
			
			DebugWrite("OnPlayerKickedByAdmin: " + SoldierName + ", reason: " + reason, 1);

			KillTracker.RemovePlayer(SoldierName);

			for(int i = 0; i < PlayerKickQueue.Count;i++)
			{
				CPlayerInfo Player = PlayerList[i];
				if (Player.SoldierName.Equals(SoldierName))
				{
					PlayerKickQueue.RemoveAt(i);
				}
			}
		}

		public override void OnPlayerJoin(string SoldierName)
		{
			// Comes before OnPlayerAuthenticated
			if (ZombieModeEnabled)
			{
				KillTracker.AddPlayer(SoldierName);
				RequestPlayersList();
			}
			else
			{
				GameState = GState.Idle;
			}
		}
		

		public override void OnPlayerAuthenticated(string SoldierName, string guid)
		{
			// Comes after OnPlayerJoin
			if (ZombieModeEnabled == false)
				return;

			DebugWrite("OnPlayerAuthenticated: " + SoldierName, 4);
			
			if (PlayerList.Count <= MaxPlayers) 
			{
				DebugWrite("OnPlayerAuthenticated: making " + SoldierName + " human", 3);
				MakeHuman(SoldierName);
				
				PlayerState.AddPlayer(SoldierName);
				return;
			}
			
			// Otherwise, we have too many players, kick this one
			
			DebugWrite("OnPlayerAuthenticated: " + PlayerList.Count + " > " + MaxPlayers + ", need to kick " + SoldierName, 2);

			base.OnPlayerAuthenticated(SoldierName, guid);
			
			PlayerKickQueue.Add(SoldierName);
			
			String msg = "Thread (";

			ThreadStart kickPlayer = delegate
			{
				try
				{
					Sleep(3);
					ExecuteCommand("procon.protected.send", "admin.kickPlayer", SoldierName, String.Concat("Zombie mode is full, try again when there are less than ", MaxPlayers.ToString(), " players"));
					int maxTries = 0;
					while (maxTries++ < 5)
					{
						if (!PlayerKickQueue.Contains(SoldierName))
							break;

						ExecuteCommand("procon.protected.send", "admin.kickPlayer", SoldierName, String.Concat("Zombie mode is full, try again when there are less than ", MaxPlayers.ToString(), " players"));
						DebugWrite("OnPlayerAuthenticated: trying to kick " + SoldierName, 4);
						Sleep(3); // Need time to get kick event
					}
				}
				catch (System.Exception e)
				{
					ConsoleException("kickPlayer: " + e.ToString());
				}
				finally
				{
					DebugWrite("OnPlayerAuthenticated: " + msg + " finished", 4);
				}
			};

			Thread t = new Thread(kickPlayer);
			
			msg = msg + t.ToString() + ")";

			DebugWrite("OnPlayerAuthenticated: " + msg + " starting", 4);
			
			t.Start();
			
			Thread.Sleep(1);
		}
		
		public override void OnPlayerKilled(Kill info)
		{
			if (ZombieModeEnabled == false)
				return;

			if (GameState != GState.Idle)
			{
				PlayerState.UpdateSpawnTime(info.Killer.SoldierName);
				PlayerState.UpdateSpawnTime(info.Victim.SoldierName);
				PlayerState.SetSpawned(info.Victim.SoldierName, false);
				if (DebugLevel > 3) ExecuteCommand("procon.protected.send", "vars.bulletDamage");
			}

			if (GameState != GState.Playing)
				return;
			
			// Extract the short weapon name
			Match WeaponMatch = Regex.Match(info.DamageType, @"Weapons/[^/]*/([^/]*)", RegexOptions.IgnoreCase);
			String WeaponName = (WeaponMatch.Success) ? WeaponMatch.Groups[1].Value : info.DamageType;

			DebugWrite("OnPlayerKilled: " + info.Killer.SoldierName + " killed " + info.Victim.SoldierName + " with " + WeaponName, 4);
			
			
			// Killed by admin?
			if (info.DamageType == "Death")
				return;
				
			const String INDIRECT_KILL = "INDIRECT KILL";

			String KillerName = (String.IsNullOrEmpty(info.Killer.SoldierName)) ?  INDIRECT_KILL : info.Killer.SoldierName;

			String KillerTeam = info.Killer.TeamID.ToString();

			String VictimName = info.Victim.SoldierName;
			
			String VictimTeam = info.Victim.TeamID.ToString();

			String DamageType = info.DamageType;
			
			String InfectMessage = null;
			
			int RemainingHumans = 0;
			
			lock (TeamHuman)
			{
				RemainingHumans = TeamHuman.Count - 1;
			}
			
			if (RemainingHumans > 0) 
			{
				InfectMessage = "*** Only " + RemainingHumans + " humans left!"; // $$$ - custom message
			}
			else
			{
				InfectMessage = "*** No humans left!"; // $$$ - custom message
			}

			if (ValidateWeapon(DamageType, KillerTeam) == false)
			{
				DebugWrite(String.Concat(KillerName, " invalid kill with ", WeaponName, "!"), 2);

				if (KillerName == INDIRECT_KILL)
					return;
				
				String msg = "ZOMBIE RULE VIOLATION! " + WeaponName + " can't be used by " + ((KillerTeam == ZOMBIE_TEAM) ? " Zombie!" : " Human!");  // $$$ - custom message
				
				TellAll(KillerName + " => " + msg);
				
				int Count = KillTracker.GetViolations(KillerName);
				
				if (Count < WarnsBeforeKickForRulesViolations)
				{
					// Warning
					KillPlayerAfterDelay(KillerName, 5);
				}
				else if (Count >= WarnsBeforeKickForRulesViolations)
				{
					KickPlayer(KillerName, msg);
				}
				
				KillTracker.SetViolations(KillerName, Count+1);

				return;
			}



			if (KillerTeam == HUMAN_TEAM && VictimTeam == ZOMBIE_TEAM)
			{
				KillTracker.ZombieKilled(KillerName, VictimName);

				DebugWrite(String.Concat("Human ", KillerName, " just killed zombie ", VictimName, " with ", WeaponName), 3);
				
				int TotalCount = 0;
				
				lock(TeamHuman)
				{
					TotalCount = TeamHuman.Count + TeamZombie.Count;
				}
								
				TellAll("*** Humans killed " + KillTracker.GetZombiesKilled() + " of " + GetKillsNeeded(TotalCount) + " zombies needed to win!"); // $$$ - custom message

				// Check for self-infecting kill
				if (Regex.Match(info.DamageType, @"(?:Knife|Melee|Defib|Repair)", RegexOptions.IgnoreCase).Success)
				{
					// Infect player
					Infect("Contact Kill", KillerName);
					// overwrite infect yell
					TellPlayer("You infected yourself with that " + WeaponName + " kill!", KillerName); // $$$ - custom message
				}
			}
			else if (KillerTeam == ZOMBIE_TEAM && VictimTeam == HUMAN_TEAM)
			{
				DebugWrite(String.Concat("Zombie ", KillerName, " just killed human ", VictimName, " with ", WeaponName), 2);

				KillTracker.HumanKilled(KillerName, VictimName);
				
				try
				{
					if (KillTracker.GetPlayerHumanDeathCount(VictimName) == DeathsNeededToBeInfected)
					{
						DebugWrite("^4SUCCESSFUL^0 Infection Test: " + VictimName + " death count = " + KillTracker.GetPlayerHumanDeathCount(VictimName) + " == " + DeathsNeededToBeInfected, 5);
					}
					else
					{
						DebugWrite("^8FAILED^0 Infection Test: " + VictimName + " death count = " + KillTracker.GetPlayerHumanDeathCount(VictimName) + " != " + DeathsNeededToBeInfected, 5);
					}
				}
				catch (Exception e)
				{
					ConsoleException(e.ToString());
				}
				
				if (KillTracker.GetPlayerHumanDeathCount(VictimName) == DeathsNeededToBeInfected)
				{					
					Infect(KillerName, VictimName);
					TellAll(InfectMessage, false); // do not overwrite Infect yell
				}
			}
			else if (KillerName == VictimName)
			{
				if (InfectSuicides)
				{
					DebugWrite("Suicide infected: " + VictimName, 2);
					Infect("Suicide ", VictimName);
					TellAll(InfectMessage, false); // do not overwrite Infect yell
				}
			}
			else if (KillerName == INDIRECT_KILL)
			{
				if (InfectSuicides)
				{
					DebugWrite("Bad luck infect: " + VictimName, 2);
					Infect("Bad luck ", VictimName);
					TellAll(InfectMessage, false); // do not overwrite Infect yell
				}
			}

			lock (TeamHuman)
			{
				DebugWrite("OnPlayerKilled: " + RemainingHumans + " humans vs " + TeamZombie.Count + " zombies with " + KillTracker.GetZombiesKilled() + " of " + GetKillsNeeded(TeamZombie.Count + TeamHuman.Count) + " zombies killed", 2);
			}
			
			CheckVictoryConditions();
		}

		public override void OnListPlayers(List<CPlayerInfo> Players, CPlayerSubset Subset)
		{
			PlayerList = Players;
					
			if (ZombieModeEnabled == false)
				return;

			if (Players.Count > 0) DebugWrite("OnListPlayers: " + Players.Count + " players", 4);
			if (OldGameState != GameState) DebugWrite("OnListPlayers: GameState = " + GameState, 3);
			OldGameState = GameState;
				
			if (CheckIdle(Players))
			{
				// We kicked some idle players, so update the player list again
				RequestPlayersList();
				return;
			}
			
			List<String> HumanCensus = new List<String>();
			List<String> ZombieCensus = new List<String>();
			
			foreach (CPlayerInfo Player in Players)
			{
				KillTracker.AddPlayer(Player.SoldierName.ToString());
				// Team tracking
				if (Player.TeamID == 1) {
					HumanCensus.Add(Player.SoldierName);
					DebugWrite("OnListPlayers: counted " + Player.SoldierName + " as human (" + HumanCensus.Count + ")", 5);
				}
				if (Player.TeamID == 2) {
					ZombieCensus.Add(Player.SoldierName);
					DebugWrite("OnListPlayers: counted " + Player.SoldierName + " as zombie (" + ZombieCensus.Count + ")", 5);
				}					
			}
			
			bool SomeoneMoved = false;
			
			lock (TeamHuman)
			{
				if (Players.Count > 0) DebugWrite("OnListPlayers: human count " + TeamHuman.Count + " vs " + HumanCensus.Count + ", zombie count " + TeamZombie.Count + " vs " + ZombieCensus.Count, 5);
				SomeoneMoved = (TeamHuman.Count != HumanCensus.Count);
				SomeoneMoved |= (TeamZombie.Count != ZombieCensus.Count);
				
				if (SomeoneMoved)
				{
					TeamHuman.Clear();
					TeamHuman.AddRange(HumanCensus);
					TeamZombie.Clear();
					TeamZombie.AddRange(ZombieCensus);
				}
			}
			
			if (GameState == GState.Playing)
			{
				if (SomeoneMoved) DebugWrite("OnListPlayers: some players went missing, TeamHuman & TeamZombie updated", 5);
				CheckVictoryConditions();
			}		

			if (GameState == GState.BetweenRounds)
			{
				// Between rounds, force update
				lock (TeamHuman)
				{
					if (!SomeoneMoved)
					{
						TeamHuman.Clear();
						TeamHuman.AddRange(HumanCensus);
						TeamZombie.Clear();
						TeamZombie.AddRange(ZombieCensus);
					}
					KnownPlayerCount = TeamZombie.Count + TeamHuman.Count;
				}
			}
			else if (GameState != GState.Idle && (HumanCensus.Count+ZombieCensus.Count) == 0)
			{
				Reset();
			}
		}

		public override void OnGlobalChat(string PlayerName, string Message)
		{
			HandleChat(PlayerName, Message, -1, -1);
		}

		public override void OnTeamChat(string PlayerName, string Message, int TeamId)
		{
			HandleChat(PlayerName, Message, TeamId, -1);
		}

		public override void OnSquadChat(string PlayerName, string Message, int TeamId, int SquadId)
		{
			HandleChat(PlayerName, Message, TeamId, SquadId);
		}
		
		public void HandleChat(string PlayerName, string Message, int TeamId, int SquadId)
		{
			String CleanMessage = Message.Trim();

			List<string> MessagePieces = new List<string>(CleanMessage.Split(' '));

			String Command = MessagePieces[0].ToLower();

			if (!Command.StartsWith(CommandPrefix.ToLower()))
			{
				if (Regex.Match(CleanMessage, @"(?:help|zombie|rules|work)", RegexOptions.IgnoreCase).Success)
				{
					TellPlayer("Type: !zombie help", PlayerName, false);
				}
				return;
			}
				
			DebugWrite("Command: '" + Message + "' => '" + CleanMessage + "'", 1);
			
			if (CommandPrefix.Length > 1 && Command == CommandPrefix)
			{
				/*
				If Message is: !zombie command arg1 arg2
				Then remove "!zombie" from the MessagePieces and reset Command
				to be the value of 'command'.
				*/
				MessagePieces.Remove(CommandPrefix);
				if (MessagePieces.Count == 0)
				{
					Command = "help";
				}
				else
				{
					Command = MessagePieces[0].ToLower();
				}
			}
			else
			{
				/*
				If command is: !zcmd arg1 arg2
				Then remove "!z" from Command
				*/
				Match CommandMatch = Regex.Match(Command, "^" + CommandPrefix + @"([^\s]+)", RegexOptions.IgnoreCase);
				if (CommandMatch.Success)
				{
					Command = CommandMatch.Groups[1].Value.ToLower();
				}
			}
			
			if (String.IsNullOrEmpty(Command)) Command = "help";
			
			DebugWrite("Command without prefix: " + Command, 6);

			String Target = null;
			
			switch (Command)
			{
				case "infect":
					if (ZombieModeEnabled == false || GameState == GState.Idle || GameState == GState.Waiting)
						return;

					if (!IsAdmin(PlayerName))
					{
						TellPlayer("Only admins can use that command!", PlayerName);
						return;
					}
					if (MessagePieces.Count != 2) return;
					Target = PlayerNameMatch(MessagePieces[1]);
					TellPlayer("Infecting " + Target, PlayerName, false);
					Infect("Admin", Target); // Does TellAll
					break;
				case "heal":
					if (ZombieModeEnabled == false || GameState == GState.Idle || GameState == GState.Waiting)
						return;

					if (!IsAdmin(PlayerName))
					{
						TellPlayer("Only admins can use that command!", PlayerName);
						return;
					}
					if (MessagePieces.Count != 2) return;
					Target = PlayerNameMatch(MessagePieces[1]);
					TellPlayer("Attempting move of " + Target + " to human team", PlayerName, false);
					MakeHuman(Target);
					break;
				case "rematch":
					if (!IsAdmin(PlayerName))
					{
						TellPlayer("Only admins can use that command!", PlayerName);
						return;
					}
					if (MessagePieces.Count != 2) return;
					if (MessagePieces[1] == "on")
						RematchEnabled = true;
					else if (MessagePieces[1] == "off")
						RematchEnabled = false;
					TellPlayer("RematchEnabled is now " + RematchEnabled, PlayerName, false);
					break;
				case "restart":
					if (!IsAdmin(PlayerName))
					{
						TellPlayer("Only admins can use that command!", PlayerName);
						return;
					}
					RestartRound();
					Reset();
					break;
				case "force":
					// Force a match/round to start
					if (!IsAdmin(PlayerName))
					{
						TellPlayer("Only admins can use that command!", PlayerName);
						return;
					}
					TellAll("Admin has forced the start of a new match ...");
					HaltMatch();
					CountdownNextRound(ZOMBIE_TEAM);
					break;
				case "next":
					if (!IsAdmin(PlayerName))
					{
						TellPlayer("Only admins can use that command!", PlayerName);
						return;
					}
					NextRound();
					Reset();
					break;
				case "mode":
					if (!IsAdmin(PlayerName))
					{
						TellPlayer("Only admins can use that command!", PlayerName);
						return;
					}
					if (MessagePieces.Count != 2) return;
					if (MessagePieces[1] == "on")
						ZombieModeEnabled = true;
					else if (MessagePieces[1] == "off")
						ZombieModeEnabled = false;
					TellPlayer("ZombieModeEnabled is now " + ZombieModeEnabled, PlayerName, false);
					break;
					Reset();
				case "rules":
					TellRules(PlayerName);
					break;
				case "warn":
					if (ZombieModeEnabled == false || GameState == GState.Idle)
						return;
					if (MessagePieces.Count < 3) return;
					string WarningMessage = String.Join(" ", MessagePieces.GetRange(2, MessagePieces.Count - 2).ToArray());
					Target = PlayerNameMatch(MessagePieces[1]);
					DebugWrite("Warning sent by " + PlayerName + " to " + Target + ": " + WarningMessage, 1);
					Warn(Target, WarningMessage);
					TellPlayer("Warning sent to " + Target, PlayerName, false);
					break;

				case "kill":
					if (!IsAdmin(PlayerName))
					{
						TellPlayer("Only admins can use that command!", PlayerName);
						return;
					}
					if (MessagePieces.Count < 2) return;
					string KillMessage = (MessagePieces.Count >= 3) ? String.Join(" ", MessagePieces.GetRange(2, MessagePieces.Count - 2).ToArray()) : "";
					Target = PlayerNameMatch(MessagePieces[1]);
					DebugWrite(PlayerName + " killing " + Target + " for '" + KillMessage + "'", 1);
					TellPlayer(KillMessage, Target);
					KillPlayerAfterDelay(Target, AnnounceDisplayLength);
					TellPlayer("Attempting to kill " + Target + " in " + AnnounceDisplayLength + " seconds", PlayerName, false);
					break;

				case "kick":
					if (!IsAdmin(PlayerName))
					{
						TellPlayer("Only admins can use that command!", PlayerName);
						return;
					}
					if (MessagePieces.Count < 2) return;
					string KickMessage = (MessagePieces.Count >= 3) ? String.Join(" ", MessagePieces.GetRange(2, MessagePieces.Count - 2).ToArray()) : "";
					Target = PlayerNameMatch(MessagePieces[1]);
					DebugWrite(PlayerName + " kicking " + Target + " for '" + KickMessage + "'", 1);
					KickPlayer(Target, KickMessage);
					TellPlayer("Kicking " + Target, PlayerName, false);
					break;
				case "status":
					TellStatus(PlayerName);
					break;
				case "idle":
					{
					double st = PlayerState.GetLastSpawnTime(PlayerName);
					String isw = (PlayerState.GetSpawned(PlayerName)) ? "spawned" : "dead";
					TellPlayer("You are " + isw + " and your last action was " + st.ToString("F0") + " seconds ago", PlayerName, false);
					break;
					}
				case "test":
					DebugWrite("loopz", 2);
					DebugWrite(FrostbitePlayerInfoList.Values.Count.ToString(), 2);
					foreach (CPlayerInfo Player in FrostbitePlayerInfoList.Values)
					{
						DebugWrite("looping", 2);
						String testmessage = Player.SoldierName;
						DebugWrite(testmessage, 2);
					}
					break;
				default: // "help"
					TellPlayer("Try suiciding and respawning", PlayerName);
					if (!IsAdmin(PlayerName))
					{
						TellPlayer("Type !zombie <command>\nCommands: rules, help, status, idle, warn", PlayerName);
					}
					else
					{
						TellPlayer("Type !zombie <command>\nCommands: infect, heal, rematch, restart, next, force, mode, kill, kick, rules, help, status, idle, warn", PlayerName);
					}
					break;
			}

		}

		public override void OnServerInfo(CServerInfo serverInfo)
		{
			// This is just to test debug logging
			DebugWrite("OnServerInfo: Debug level = " + DebugLevel + " ....", 7);
			DebugWrite("GameState = " + GameState, 6);
			
			if (GameState == GState.BetweenRounds)
			{
				lock (TeamHuman)
				{
					KnownPlayerCount = TeamHuman.Count + TeamZombie.Count;
				}
			}
		}

		public override void OnPlayerTeamChange(string soldierName, int teamId, int squadId)
		{
			if (ZombieModeEnabled == false)
				return;

			bool wasZombie = false;
			bool wasHuman = false;

			lock (TeamHuman)
			{
				wasZombie = TeamZombie.Contains(soldierName);
				wasHuman = TeamHuman.Contains(soldierName);
			}

			// Ignore squad changes within team
			if (teamId == 1 && wasHuman) return;
			if (teamId == 2 && wasZombie) return;
			if (!(teamId == 1 || teamId == 2)) {
				ConsoleError("OnPlayerTeamChange unknown teamId = " + teamId);
				return;
			}

			if (GameState == GState.Idle || GameState == GState.Waiting || GameState == GState.CountingDown)
				return;
			
			string team = (wasHuman) ? "HUMAN" : "ZOMBIE";
			DebugWrite("OnPlayerTeamChange: " + soldierName + "(" + team + ") to " + teamId, 3);
			
			if (GameState != GState.BetweenRounds)
			{
				if (teamId == 1 && wasZombie) // to humans
				{
					// Switching to human team is not allowed
					TellPlayer("Don't switch to the human team! Sending you back to zombies!", soldierName); // $$$ - custom message

					ForceMove(soldierName, ZOMBIE_TEAM, AnnounceDisplayLength);

					lock (TeamHuman)
					{
						if (TeamHuman.Contains(soldierName)) TeamHuman.Remove(soldierName);
						if (!TeamZombie.Contains(soldierName)) TeamZombie.Add(soldierName);
					}

				} 
				else if (teamId == 2 && wasHuman) // to zombies
				{
					// Switching to the zombie team is okay
					FreshZombie.Add(soldierName);

					lock (TeamHuman)
					{
						if (TeamHuman.Contains(soldierName)) TeamHuman.Remove(soldierName);
						if (!TeamZombie.Contains(soldierName)) TeamZombie.Add(soldierName);
					}
				} 
				else if (!wasHuman && !wasZombie && GameState == GState.Playing)
				{
					// New player joining in the middle of the match
					
					DebugWrite("OnPlayerTeamChange: new player " + soldierName + " just joined on team " + teamId, 3);

					if (teamId != ((NewPlayersJoinHumans) ? 1 : 2))
					{
						string Which = (NewPlayersJoinHumans) ? HUMAN_TEAM : ZOMBIE_TEAM;
						TellPlayer("You are a new player, sending you to the other team ...", soldierName);
						
						DebugWrite("OnPlayerTeamChange: switching new player " + soldierName + " to team " + Which, 3);
						
						ForceMove(soldierName, Which, AnnounceDisplayLength);
						
						if (NewPlayersJoinHumans)
						{
							if (!TeamHuman.Contains(soldierName)) TeamHuman.Add(soldierName);
						}
						else
						{
							if (!TeamZombie.Contains(soldierName)) TeamZombie.Add(soldierName);
						}
						
					}
				}
				
			} else if (GameState == GState.BetweenRounds) { // server is swapping teams
				
				int ZombieCount = 0;
				
				if (teamId == 1) // to humans
				{
					++ServerSwitchedCount;
					
					// Add to the lottery if eligible
					if (!PatientZeroes.Contains(soldierName)) Lottery.Add(soldierName);
					
					lock (TeamHuman)
					{
						if (TeamZombie.Contains(soldierName)) TeamZombie.Remove(soldierName);
						if (!TeamHuman.Contains(soldierName)) TeamHuman.Add(soldierName);
					}
				} 
				else if (teamId == 2) // to zombies
				{
					++ServerSwitchedCount;

					// Switch back
					MakeHuman(soldierName);
				}
				
				// When the server is done swapping players, process patient zero
				if (ServerSwitchedCount >= KnownPlayerCount)
				{
					while (ZombieCount < MinimumZombies)
					{
						if (Lottery.Count == 0)
						{
							// loop through players, adding to Lottery if eligible
							foreach (CPlayerInfo p in PlayerList)
							{
								if (!PatientZeroes.Contains(p.SoldierName))
								{
									Lottery.Add(p.SoldierName);
								}
							}
						}
						
						if (Lottery.Count == 0)
						{
							ConsoleWarn("OnPlayerTeamChange, can't find an eligible player for patient zero!");
							PatientZeroes.Clear();
							Lottery.Add(soldierName);
						}
						
						Random rand = new Random();
						int choice = (Lottery.Count == 1) ? 0 : (rand.Next(Lottery.Count));
						PatientZero = Lottery[choice];
						DebugWrite("OnPlayerTeamChange: lottery selected " + PatientZero + " as a zombie!", 3);
						Lottery.Remove(PatientZero);
						

						MakeZombie(PatientZero);

						if (PatientZeroes.Count > (KnownPlayerCount/2)) PatientZeroes.Clear();

						PatientZeroes.Add(PatientZero);
						
						++ZombieCount;
					}
					
					DebugWrite("OnPlayerTeamChange: making " + PatientZero + " the first zombie!", 2);
					
					ServerSwitchedCount = 0;
				}
				/*
				GameState stays in BetweenRounds state because we don't know when the
				actual round starts until a player spawns. See OnPlayerSpawned.
				*/
			}
			
		}


		public override void OnPlayerSpawned(string soldierName, Inventory spawnedInventory)
		{
			if (ZombieModeEnabled == false) 
			{
				GameState = GState.Idle;
				return;
			}
			
			String WhichTeam = (GameState == GState.Playing) ? "UNKNOWN" : GameState.ToString();
			
			lock (TeamHuman)
			{
				if (TeamZombie.Contains(soldierName))
				{
					WhichTeam = "ZOMBIE";
				}
				else if (TeamHuman.Contains(soldierName))
				{
					WhichTeam = "HUMAN";
				}
			}
			DebugWrite("OnPlayerSpawned: " + soldierName + "(" + WhichTeam + ")", 5);
			
			PlayerState.UpdateSpawnTime(soldierName);
			PlayerState.SetSpawned(soldierName, true);


			// Check if we have enough players spawned
			int Need = MinimumHumans + MinimumZombies;
			if (PlayerList.Count < Need)
			{
				if (GameState == GState.Playing)
				{
					TellAll("Not enough players left to finish match ... MATCH HALTED!");
					HaltMatch(); // Sets GameState to Waiting
				}
				else
				{
					TellAll("Welcome to Zombie Mode! Need " + (Need-PlayerList.Count) + " more players to join AND spawn to start the match ..."); // $$$ - custom message
				}
				GameState = GState.Waiting;
				return; // Don't count this spawn
			} 
			else if (PlayerList.Count >= Need && GameState == GState.Waiting)
			{
				TellAll("New match starting ... counting down ..."); // $$$ - custom message
				CountdownNextRound(ZOMBIE_TEAM); // Sets GameState to CountingDown or NeedSpawn
				return;
			}
			
			// Check if this is the first spawn of the round/match
			if (GameState == GState.BetweenRounds || GameState == GState.NeedSpawn)
			{
				GameState = GState.Playing;
				DebugWrite("--- Version " + GetPluginVersion() + " ---", 3);
				DebugWrite("^b^2****** MATCH STARTING WITH " + PlayerList.Count + " players!^0^n", 1);
				DebugWrite("OnPlayerSpawned: announcing first zombie is " + PatientZero, 3);
				TellAll(PatientZero + " is the first zombie!"); // $$$ - custom message
			}
			
			int n = PlayerState.GetSpawnCount(soldierName);
			
			// Tell zombies they can only use hand to hand weapons
			if (FreshZombie.Contains(soldierName)) 
			{
				DebugWrite("OnPlayerSpawned " + soldierName + " is fresh zombie!", 3);
				FreshZombie.Remove(soldierName);
				TellPlayer("You are now a zombie! Use a knife/defib/repair tool only!", soldierName); // $$$ - custom message
			} 
			else if (PlayerState.GetWelcomeCount(soldierName) == 0)
			{
				String Separator = " ";
				if (CommandPrefix.Length == 1) Separator = "";
				TellPlayer("Welcome to Zombie Mode! Type '" + CommandPrefix + Separator + "rules' for instructions on how to play", soldierName); // $$$ - custom message
				PlayerState.SetWelcomeCount(soldierName, 1);
			}
			else if (n == 0)
			{
				lock (TeamHuman)
				{
					if (!TeamHuman.Contains(soldierName)) ConsoleError("OnPlayerSpawned: " + soldierName + " should be human, but not present in TeamHuman list!");
				}
				TellPlayer("You are a human! Shoot zombies, don't use explosives, don't let zombies get near you!", soldierName); // $$$ - custom message
			}
			
			PlayerState.SetSpawnCount(soldierName, n+1);
			
			AdaptDamage();
		}

		public override void OnLevelLoaded(string mapFileName, string Gamemode, int roundsPlayed, int roundsTotal)
		{
			if (ZombieModeEnabled == false) 
			{
				GameState = GState.Idle;
				return;
			}

			DebugWrite("OnLevelLoaded, updating player list", 3);
			
			// We have 5 seconds before the server swaps teams, make sure we are up to date
			RequestPlayersList();
			
			// Reset the team switching counter
			ServerSwitchedCount = 0;
			
			// Reset the known player count
			KnownPlayerCount = 0;
			
			// Reset the utility lists
			FreshZombie.Clear();
			Lottery.Clear();
			
			// Reset patient zero
			PatientZero = null;
			
			// Reset per-round player states
			PlayerState.ResetPerRound();
			
			// Reset kill tracker
			KillTracker.ResetPerRound();
			
			// Sanity check
			DebugWrite("OnLevelLoaded: GameState is " + GameState, 3);
		}

		public override void OnRoundOver(int winningTeamId)
		{
			if (ZombieModeEnabled == false) 
			{
				GameState = GState.Idle;
				return;
			}

			DebugWrite("OnRoundOver, GameState set to BetweenRounds", 4);
			
			GameState = GState.BetweenRounds;

			// Reset the team switching counter
			ServerSwitchedCount = 0;
			
			// Reset the known player count
			KnownPlayerCount = 0;

			// Reset the utility lists
			FreshZombie.Clear();
			Lottery.Clear();
			
			// Reset patient zero
			PatientZero = null;
			
			// Reset per-round player states
			PlayerState.ResetPerRound();
			
			// Reset kill tracker
			KillTracker.ResetPerRound();
		}

		public override void OnPlayerLeft(CPlayerInfo playerInfo) {
			if (ZombieModeEnabled == false) 
			{
				GameState = GState.Idle;
				return;
			}

			DebugWrite("OnPlayerLeft: " + playerInfo.SoldierName, 4);

			RequestPlayersList();
		}


		#endregion


		#region PluginMethods
		/** PLUGIN RELATED SHIT **/
		#region PluginEventHandlers
		public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
		{
			RegisterEvents(GetType().Name, 
				"OnPlayerKilled",
				"OnListPlayers",
				"OnSquadChat",
				"OnTeamChat",
				"OnGlobalChat",
				"OnPlayerJoin",
				"OnPlayerAuthenticated",
				"OnPlayerKickedByAdmin",
				"OnServerInfo",
				"OnPlayerTeamChange",
				"OnPlayerSpawned",
				"OnLevelLoaded",
				"OnRoundOver",
				"OnPlayerLeft"
				);
		}

		public void OnPluginEnable()
		{
			//System.Diagnostics.Debugger.Break();
			ConsoleLog("^b^2Enabled... It's Game Time!");
			ConsoleLog("--- Version " + GetPluginVersion() + " ---");
		}

		public void OnPluginDisable()
		{
			ConsoleLog("--- Version " + GetPluginVersion() + " ---");
			ConsoleLog("^b^2Disabled :(");
			Reset();
		}
		#endregion

		// Plugin details
		public string GetPluginName()
		{
			return "Zombie Mode";
		}

		public string GetPluginVersion()
		{
			return "0.1.1";
		}

		public string GetPluginAuthor()
		{
			return "m4xxd3v";
		}

		public string GetPluginWebsite()
		{
			return "http://www.phogue.net";
		}

		public string GetPluginDescription()
		{
			return Description.HTML;
		}


		// Plugin variables
		public List<CPluginVariable> GetDisplayPluginVariables()
		{
			List<CPluginVariable> lstReturn = new List<CPluginVariable>();

			lstReturn.Add(new CPluginVariable("Game Settings|Zombie Mode Enabled", typeof(enumBoolYesNo), ZombieModeEnabled ? enumBoolYesNo.Yes : enumBoolYesNo.No));

			lstReturn.Add(new CPluginVariable("Admin Settings|Command Prefix", CommandPrefix.GetType(), CommandPrefix));

			lstReturn.Add(new CPluginVariable("Admin Settings|Announce Display Length", AnnounceDisplayLength.GetType(), AnnounceDisplayLength));

			lstReturn.Add(new CPluginVariable("Admin Settings|Warning Display Length", WarningDisplayLength.GetType(), WarningDisplayLength));

			lstReturn.Add(new CPluginVariable("Admin Settings|Human Max Idle Seconds", HumanMaxIdleSeconds.GetType(), HumanMaxIdleSeconds));

			lstReturn.Add(new CPluginVariable("Admin Settings|Max Idle Seconds", MaxIdleSeconds.GetType(), MaxIdleSeconds));

			lstReturn.Add(new CPluginVariable("Admin Settings|Warns Before Kick For Rules Violations", WarnsBeforeKickForRulesViolations.GetType(), WarnsBeforeKickForRulesViolations));

			lstReturn.Add(new CPluginVariable("Admin Settings|Debug Level", DebugLevel.GetType(), DebugLevel));

			lstReturn.Add(new CPluginVariable("Admin Settings|Admin Users", typeof(string[]), AdminUsers.ToArray()));

			lstReturn.Add(new CPluginVariable("Game Settings|Max Players", MaxPlayers.GetType(), MaxPlayers));

			lstReturn.Add(new CPluginVariable("Game Settings|Minimum Zombies", MinimumZombies.GetType(), MinimumZombies));

			lstReturn.Add(new CPluginVariable("Game Settings|Minimum Humans", MinimumHumans.GetType(), MinimumHumans));

			lstReturn.Add(new CPluginVariable("Game Settings|Zombie Kill Limit Enabled", typeof(enumBoolOnOff), ZombieKillLimitEnabled ? enumBoolOnOff.On : enumBoolOnOff.Off));

			/* to be removed
			if (ZombieKillLimitEnabled)
				lstReturn.Add(new CPluginVariable("Game Settings|Zombies Killed To Survive", ZombiesKilledToSurvive.GetType(), ZombiesKilledToSurvive));
			*/

			lstReturn.Add(new CPluginVariable("Game Settings|Deaths Needed To Be Infected", DeathsNeededToBeInfected.GetType(), DeathsNeededToBeInfected));
			
			lstReturn.Add(new CPluginVariable("Game Settings|Infect Suicides", typeof(enumBoolOnOff), InfectSuicides ? enumBoolOnOff.On : enumBoolOnOff.Off));

			lstReturn.Add(new CPluginVariable("Game Settings|New Players Join Humans", typeof(enumBoolOnOff), NewPlayersJoinHumans ? enumBoolOnOff.On : enumBoolOnOff.Off));

			lstReturn.Add(new CPluginVariable("Game Settings|Rematch Enabled", typeof(enumBoolOnOff), RematchEnabled ? enumBoolOnOff.On : enumBoolOnOff.Off));
			
			if (ZombieKillLimitEnabled)
			{

				lstReturn.Add(new CPluginVariable("Goal For Humans|Kills If 8 Or Less Players", KillsIf8OrLessPlayers.GetType(), KillsIf8OrLessPlayers));

				lstReturn.Add(new CPluginVariable("Goal For Humans|Kills If 12 To 9 Players", KillsIf12To9Players.GetType(), KillsIf12To9Players));

				lstReturn.Add(new CPluginVariable("Goal For Humans|Kills If 16 To 13 Players", KillsIf16To13Players.GetType(), KillsIf16To13Players));

				lstReturn.Add(new CPluginVariable("Goal For Humans|Kills If 20 To 17 Players", KillsIf20To17Players.GetType(), KillsIf20To17Players));

				lstReturn.Add(new CPluginVariable("Goal For Humans|Kills If 24 To 21 Players", KillsIf24To21Players.GetType(), KillsIf24To21Players));

				lstReturn.Add(new CPluginVariable("Goal For Humans|Kills If 28 To 25 Players", KillsIf28To25Players.GetType(), KillsIf28To25Players));

				lstReturn.Add(new CPluginVariable("Goal For Humans|Kills If 32 To 29 Players", KillsIf32To29Players.GetType(), KillsIf32To29Players));
			}
		
			lstReturn.Add(new CPluginVariable("Human Damage Percentage|Against 1 Or 2 Zombies", Against1Or2Zombies.GetType(), Against1Or2Zombies));

			lstReturn.Add(new CPluginVariable("Human Damage Percentage|Against A Few Zombies", AgainstAFewZombies.GetType(), AgainstAFewZombies));

			lstReturn.Add(new CPluginVariable("Human Damage Percentage|Against Equal Numbers", AgainstEqualNumbers.GetType(), AgainstEqualNumbers));

			lstReturn.Add(new CPluginVariable("Human Damage Percentage|Against Many Zombies", AgainstManyZombies.GetType(), AgainstManyZombies));

			lstReturn.Add(new CPluginVariable("Human Damage Percentage|Against Countless Zombies", AgainstCountlessZombies.GetType(), AgainstCountlessZombies));

			foreach (PRoCon.Core.Players.Items.Weapon Weapon in WeaponDictionaryByLocalizedName.Values)
			{
				String WeaponDamage = Weapon.Damage.ToString();

				if (WeaponDamage.Equals("Nonlethal") || WeaponDamage.Equals("None") || WeaponDamage.Equals("Suicide"))
					continue;

				String WeaponName = Weapon.Name.ToString();
				lstReturn.Add(new CPluginVariable(String.Concat("Zombie Weapons|Z -", WeaponName), typeof(enumBoolOnOff), ZombieWeaponsEnabled.IndexOf(WeaponName) >= 0 ? enumBoolOnOff.On : enumBoolOnOff.Off));
				lstReturn.Add(new CPluginVariable(String.Concat("Human Weapons|H -", WeaponName), typeof(enumBoolOnOff), HumanWeaponsEnabled.IndexOf(WeaponName) >= 0 ? enumBoolOnOff.On : enumBoolOnOff.Off));
			}


			return lstReturn;
		}

		public List<CPluginVariable> GetPluginVariables()
		{
			List<CPluginVariable> lstReturn = GetDisplayPluginVariables();

			return lstReturn;
		}

		public void SetPluginVariable(string Name, string Value)
		{
			ThreadStart MyThread = delegate
			{
				try
				{
					int PipeIndex = Name.IndexOf('|');
					if (PipeIndex >= 0)
					{
						PipeIndex++;
						Name = Name.Substring(PipeIndex, Name.Length - PipeIndex);
					}

					BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

					String PropertyName = Name.Replace(" ", "");

					FieldInfo Field = GetType().GetField(PropertyName, Flags);

					Dictionary<int, Type> EasyTypeDict = new Dictionary<int, Type>();
					EasyTypeDict.Add(0, typeof(int));
					EasyTypeDict.Add(1, typeof(Int16));
					EasyTypeDict.Add(2, typeof(Int32));
					EasyTypeDict.Add(3, typeof(Int64));
					EasyTypeDict.Add(4, typeof(float));
					EasyTypeDict.Add(5, typeof(long));
					EasyTypeDict.Add(6, typeof(String));
					EasyTypeDict.Add(7, typeof(string));

					Dictionary<int, Type> BoolDict = new Dictionary<int, Type>();
					BoolDict.Add(0, typeof(Boolean));
					BoolDict.Add(1, typeof(bool));

					Dictionary<int, Type> ListStrDict = new Dictionary<int, Type>();
					ListStrDict.Add(0, typeof(List<String>));
					ListStrDict.Add(1, typeof(List<string>));
					
					

					if (Field != null)
					{
						
						Type FieldType = Field.GetValue(this).GetType();
						if (EasyTypeDict.ContainsValue(FieldType))
							Field.SetValue(this, TypeDescriptor.GetConverter(FieldType).ConvertFromString(Value));
						else if (ListStrDict.ContainsValue(FieldType))
							Field.SetValue(this, new List<string>(CPluginVariable.DecodeStringArray(Value)));
						else if (BoolDict.ContainsValue(FieldType))
							if (Value == "Yes" || Value == "On")
								Field.SetValue(this, true);
							else
								Field.SetValue(this, false);
					}
					else
					{
						String WeaponName = Name.Substring(3, Name.Length - 3);

						if (WeaponList.IndexOf(WeaponName) >= 0)
						{
							String WeaponType = Name.Substring(0, 3);

							if (WeaponType == "H -")
							{
								if (Value == "On")
									EnableHumanWeapon(WeaponName);
								else
									DisableHumanWeapon(WeaponName);
							}
							else
							{
								if (Value == "On")
									EnableZombieWeapon(WeaponName);
								else
									DisableZombieWeapon(WeaponName);
							}

						}
					}
				}
				catch (System.Exception e)
				{
					ConsoleException("MyThread: " + e.ToString());
				}
				finally
				{
					// Validate all values and correct if needed
					if (DebugLevel < 0)
					{
						DebugValue("Debug Level", DebugLevel.ToString(), "must be greater than 0", "3");
						DebugLevel = 3; // default
					}
					if (String.IsNullOrEmpty(CommandPrefix))
					{
						DebugValue("Command Prefix", "(empty)", "must not be empty", "!zombie");
						CommandPrefix = "!zombie"; // default
					}
					if (AnnounceDisplayLength < 5 || AnnounceDisplayLength > 20)
					{
						DebugValue("Announce Display Length", AnnounceDisplayLength.ToString(), "must be between 5 and 20, inclusive", "10");
						AnnounceDisplayLength = 10; // default
					}
					if (WarningDisplayLength < 5 || WarningDisplayLength > 20)
					{
						DebugValue("Warning Display Length", WarningDisplayLength.ToString(), "must be between 5 and 20, inclusive", "15");
						WarningDisplayLength = 15; // default
					}
					if (MaxPlayers < 8 || MaxPlayers > 32)
					{
						DebugValue("Max Players", MaxPlayers.ToString(), "must be between 8 and 32, inclusive", "32");
						MaxPlayers = 32; // default
					}
					if (MinimumHumans < 2 || MinimumHumans > (MaxPlayers-1))
					{
						DebugValue("Minimum Humans", MinimumHumans.ToString(), "must be between 3 and " + (MaxPlayers-1), "2");
						MinimumHumans = 3; // default
					}
					if (MinimumZombies < 1 || MinimumZombies > (MaxPlayers-MinimumHumans))
					{
						DebugValue("Minimum Zombies", MinimumZombies.ToString(), "must be between 1 and " + (MaxPlayers-MinimumHumans), "1");
						MinimumZombies = 1; // default
					}
					if (DeathsNeededToBeInfected < 1 || DeathsNeededToBeInfected > 10)
					{
						DebugValue("Deaths Needed To Be Infected", DeathsNeededToBeInfected.ToString(), "must be between 1 and 10, inclusive", "1");
						DeathsNeededToBeInfected = 1; // default
					}
					/* To be removed
					if (ZombiesKilledToSurvive < MaxPlayers)
					{
						DebugValue("Zombies Killed To Survive", ZombiesKilledToSurvive.ToString(), "must be more than " + MaxPlayers, "50");
						ZombiesKilledToSurvive = 50; // default
					}
					*/
					if (HumanMaxIdleSeconds < 0 )
					{
						DebugValue("Human Max Idle Seconds", HumanMaxIdleSeconds.ToString(), "must not be negative", "120");
						HumanMaxIdleSeconds = 120; // default
					}
					if (MaxIdleSeconds < 0)
					{
						DebugValue("Max Idle Seconds", MaxIdleSeconds.ToString(), "must not be negative", "600");
						MaxIdleSeconds = 600; // default
					}
					if (KillsIf8OrLessPlayers < 6)
					{
						DebugValue("Kills If 8 Or Less Players", KillsIf8OrLessPlayers.ToString(), "must be 6 or more", "6");
						KillsIf8OrLessPlayers = 6; // default
					}
				}
			};

			Thread t = new Thread(MyThread);

			t.Start();

			
		}
		#endregion



		/** PRIVATE METHODS **/

		#region RoundCommands

		private void RestartRound()
		{
			ExecuteCommand("procon.protected.send", "mapList.restartRound");
		}

		private void NextRound()
		{
			ExecuteCommand("procon.protected.send", "mapList.runNextRound");
		}
		
		private void CountdownNextRound(string WinningTeam)
		{
			
			GameState = GState.CountingDown;
			
			DebugWrite("CountdownNextRound started", 2);
			
			ThreadStart countdown = delegate
			{
				try
				{
					if (RematchEnabled)
					{
						Sleep(AnnounceDisplayLength);
						TellAll("New match will start in 5 seconds ... prepare to be moved!");
						Sleep(5);
						
						DebugWrite("CountdownNextRound ended with rematch mode enabled", 2);
						
						MakeTeams(); // Sets GameState to NeedSpawn
					}
					else
					{
						Sleep(AnnounceDisplayLength);
						TellAll("Next round will start in 5 seconds");
						Sleep(5);
						
						DebugWrite("CountdownNextRound thread: end round with winner teamID = " + "WinningTeam", 3);
						
						ExecuteCommand("procon.protected.send", "mapList.endRound", WinningTeam);
						
						GameState = GState.BetweenRounds;
					}
					
				}
				catch (Exception e)
				{
					ConsoleException("countdown: " + e.ToString());
				}
			};
			
			String Separator = " ";
			if (CommandPrefix.Length == 1) Separator = "";
			TellAll("Type '" + CommandPrefix + Separator + "rules' for instructions on how to play", false); // $$$ - custom message

			Thread t = new Thread(countdown);

			t.Start();
			
			Thread.Sleep(2);
		}

		#endregion


		#region PlayerPunishmentCommands

		private void Warn(String PlayerName, String Message)
		{
			ExecuteCommand("procon.protected.send", "admin.yell", Message, WarningDisplayLength.ToString(), PlayerName);
		}

		private void KillPlayerAfterDelay(string PlayerName, int Delay)
		{
			DebugWrite("KillPlayerAfterDelay: " + PlayerName + " after " + Delay + " seconds", 3);
			
			ThreadStart killerThread = delegate
			{
				try 
				{
					Sleep(Delay);
					ExecuteCommand("procon.protected.send", "admin.killPlayer", PlayerName);
				}
				catch (Exception e)
				{
					ConsoleException(e.ToString());
				}
			};
			
			Thread t = new Thread(killerThread);
			t.Start();
			Thread.Sleep(1);
		}

		private void KillPlayer(string PlayerName)
		{
			KillPlayerAfterDelay(PlayerName, 1);
		}

		private void KickPlayer(string PlayerName, string Reason)
		{
			ExecuteCommand("procon.protected.send", "admin.kickPlayer", PlayerName, Reason);
		}

		private bool CheckIdle(List<CPlayerInfo> Players)
		{
			bool KickedSomeone = false;
			
			foreach (CPlayerInfo Player in Players)
			{
				String Name = Player.SoldierName;
				double MaxTime = MaxIdleSeconds;
				lock (TeamHuman)
				{
					if (GameState == GState.Playing && TeamHuman.Contains(Name))
					{
						MaxTime = HumanMaxIdleSeconds;
					}
				}
				if (PlayerState.IdleTimeExceedsMax(Name, MaxTime))
				{
					DebugWrite("CheckIdle: " + Name + " ^8^bexceeded idle time of " + MaxTime + " seconds, KICKING ...^n^0", 2);
					KickPlayer(Name, "Idle for more than " + MaxTime + " seconds");
					KickedSomeone = true;
				}
			}
			
			return KickedSomeone;
		}

		#endregion

		#region TeamMethods

		private void RequestPlayersList()
		{
			ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
		}

		public void Infect(string Carrier, string Victim)
		{
			TellAll(String.Concat(Carrier, " just infected ", Victim)); // $$$ - custom message

			MakeZombie(Victim);
			
			AdaptDamage();
		}

		private void MakeHuman(string PlayerName)
		{
			TellAll(String.Concat(PlayerName, " has joined the fight for survival!"), false); // $$$ - custom message
			
			DebugWrite("MakeHuman: " + PlayerName, 3);

			ExecuteCommand("procon.protected.send", "admin.movePlayer", PlayerName, HUMAN_TEAM, BLANK_SQUAD, FORCE_MOVE);
			
			lock (TeamHuman)
			{
				if (TeamZombie.Contains(PlayerName)) TeamZombie.Remove(PlayerName);
				if (!TeamHuman.Contains(PlayerName)) TeamHuman.Add(PlayerName);
			}
		}
		
		private void ForceMove(string PlayerName, string TeamId, int DelaySecs)
		{
			ThreadStart forceMove = delegate
			{
				try
				{
					// Delay for message?
					if (DelaySecs != 0) Thread.Sleep(DelaySecs * 1000);
					
					// Kill player requires a delay to work correctly
					
					ExecuteCommand("procon.protected.send", "admin.killPlayer", PlayerName);
					
					Thread.Sleep(200);
					
					// Now do the move

					ExecuteCommand("procon.protected.send", "admin.movePlayer", PlayerName, TeamId, BLANK_SQUAD, FORCE_MOVE);
					
					Thread.Sleep(300);
					
					// Now update TeamHuman & TeamZombie
					RequestPlayersList();
					
				}
				catch (Exception e)
				{
					ConsoleException("forceMove: " + e.ToString());
				}
			};
			
			Thread t = new Thread(forceMove);

			t.Start();
			
			Thread.Sleep(2);
			
			DebugWrite("ForceMove " + PlayerName + " to " + TeamId, 3);
		}

		private void ForceMove(string PlayerName, string TeamId)
		{
			ForceMove(PlayerName, TeamId, 0);
		}


		private void MakeZombie(string PlayerName)
		{
			DebugWrite("MakeZombie: " + PlayerName, 3);

			ForceMove(PlayerName, ZOMBIE_TEAM);
			
			lock (TeamHuman)
			{
				if (TeamHuman.Contains(PlayerName)) TeamHuman.Remove(PlayerName);
				if (!TeamZombie.Contains(PlayerName)) TeamZombie.Add(PlayerName);	
			}
			
			FreshZombie.Add(PlayerName);
		}


		private void MakeTeams()
		{
			ThreadStart makeTeams = delegate
			{
				try
				{
					Sleep(5); // allow time to update player list
					
					// First, kill all the former zombies to prepare for team switches
					
					List<String> tmp = new List<String>();
					
					List<String> ZombieCopy = new List<String>();
					
					lock (TeamHuman) // Only lock this object for both humans and zombies
					{
						ZombieCopy.AddRange(TeamZombie);
					}
					
					foreach (String z in ZombieCopy)
					{
						// We are managing the delay manually, so don't use KillPlayerAfterDelay
						ExecuteCommand("procon.protected.send", "admin.killPlayer", z);
						tmp.Add(z);
						Thread.Sleep(100);
					}


					// Then, move them to human team
					// We can't use TeamZombie here, because MakeHuman modifies it
					
					foreach (String z in tmp)
					{
						MakeHuman(z);
						Thread.Sleep(25);
					}
					
					// Fill the lottery pool for selecting patient zero
					
					Lottery.Clear();
					
					lock (TeamHuman)
					{
						foreach (String h in TeamHuman)
						{
							if (!PatientZeroes.Contains(h)) Lottery.Add(h);
						}
					}

					// Sanity check
					
					if (Lottery.Count < MinimumZombies)
					{
						ConsoleWarn("makeTeams, can't find enough eligible players for patient zero!");
						
						PatientZeroes.Clear();
						Lottery.Clear();
						
						lock (TeamHuman)
						{
							for (int i = 0; i < TeamHuman.Count; ++i)
							{
								Lottery.Add(TeamHuman[i]);
								if ((i + 1) >= MinimumZombies) break;
							}
						}
					}
					
					// Choose patient zero randomly from lottery pool
					
					FreshZombie.Clear();
					
					Random rand = new Random();
					
					int ZombieCount = 0;
					
					while (ZombieCount < MinimumZombies)
					{
						int choice = (Lottery.Count == 1) ? 0 : (rand.Next(Lottery.Count));
						PatientZero = Lottery[choice];
						Lottery.Remove(PatientZero);
						
						Infect("Patient Zero ", PatientZero);
						++ZombieCount;
						
						if (PatientZeroes.Count > (KnownPlayerCount/2)) PatientZeroes.Clear();
						
						PatientZeroes.Add(PatientZero);
					}
					

					DebugWrite("makeTeams: lottery selected " + PatientZero + " as first zombie!", 2);

					DebugWrite("makeTeams: ready for another round!", 2);
					
					TellAll("*** Spawn now, Zombie Mode is on!"); // $$$ - custom message
					
					// Reset state

					Lottery.Clear();
					KnownPlayerCount = 0;
					ServerSwitchedCount = 0;
					
					PlayerState.ResetPerMatch();
					KillTracker.ResetPerMatch();

					/* GameState is set to Playing in OnPlayerSpawned */
					
				} 
				catch (Exception e)
				{
					ConsoleException("nukeZombies: " + e.ToString());
				}
				finally
				{
					GameState = GState.NeedSpawn;
				}
			};
			
			// Update the player lists
			
			RequestPlayersList();
			
			// Tell everyone to hold on tight
			
			// TellAll("*** PREPARE TO BE MOVED, new match starting, same map level!"); // $$$ - custom message
			
			Thread t = new Thread(makeTeams);

			t.Start();
			
			Thread.Sleep(2);
		}
		
		private int GetKillsNeeded(int TotalCount)
		{
			int Needed = 0;
			
			if (TotalCount <= 8)
			{
				Needed = KillsIf8OrLessPlayers;
			}
			else if (TotalCount <= 12 && TotalCount >= 9)
			{
				Needed = KillsIf12To9Players;
			}
			else if (TotalCount <= 16 && TotalCount >= 13)
			{
				Needed = KillsIf16To13Players;
			}
			else if (TotalCount <= 20 && TotalCount >= 17)
			{
				Needed = KillsIf20To17Players;
			}
			else if (TotalCount <= 24 && TotalCount >= 21)
			{
				Needed = KillsIf24To21Players;
			}
			else if (TotalCount <= 28 && TotalCount >= 25)
			{
				Needed = KillsIf28To25Players;
			}
			else if (TotalCount <= 32 && TotalCount >= 29)
			{
				Needed = KillsIf32To29Players;
			}
			else
			{
				ConsoleError("CheckVictoryConditions: bad TotalCount");
				return 0;
			}
			
			return Needed;
		}

		private void CheckVictoryConditions()
		{
			// Victory conditions
			
			int Needed = 0;
			int TotalCount = 0;
			int HCount = 0;
			int ZCount = 0;
			lock(TeamHuman)
			{
				HCount = TeamHuman.Count;
				ZCount = TeamZombie.Count;
				TotalCount = HCount + ZCount;
			}

			Needed = GetKillsNeeded(TotalCount);

			// All zombies left the server?
			if (ZCount == 0 && HCount > 0)
			{
				string msg = "HUMANS WIN, no zombies left on the server!"; // $$$ - custom message
				DebugWrite("^2^b ***** " + msg + "^n^0", 1);
				TellAll(msg);
				CountdownNextRound(HUMAN_TEAM);
				return;
			}

			// Humans got enough kills?
			if (HCount > 0 && KillTracker.GetZombiesKilled() >= Needed)
			{
				string msg = "HUMANS WIN with " + KillTracker.GetZombiesKilled() + " zombies killed!"; // $$$ - custom message
				DebugWrite("^2^b ***** " + msg + "^n^0", 1);
				TellAll(msg);
				CountdownNextRound(HUMAN_TEAM);
			}
			else 
			{
				// All humans infected?
				if (HCount == 0 && ZCount > MinimumZombies)
				{
					string msg = "ZOMBIES WIN, all humans infected!"; // $$$ - custom message
					DebugWrite("^7^b ***** " + msg + "^n^0", 1);
					TellAll(msg);
					CountdownNextRound(ZOMBIE_TEAM);
				}
			}
		}

		#endregion

		#region WeaponMethods

		private void DisableZombieWeapon(String WeaponName)
		{
			int Index = ZombieWeaponsEnabled.IndexOf(WeaponName);
			if (Index >= 0)
				ZombieWeaponsEnabled.RemoveAt(Index);
		}

		private void DisableHumanWeapon(String WeaponName)
		{
			int Index = HumanWeaponsEnabled.IndexOf(WeaponName);
			if (Index >= 0)
				HumanWeaponsEnabled.RemoveAt(Index);
		}

		private void EnableZombieWeapon(String WeaponName)
		{
			int Index = ZombieWeaponsEnabled.IndexOf(WeaponName);
			if (Index < 0)
				ZombieWeaponsEnabled.Add(WeaponName);
		}

		private void EnableHumanWeapon(String WeaponName)
		{
			int Index = HumanWeaponsEnabled.IndexOf(WeaponName);
			if (Index < 0)
				HumanWeaponsEnabled.Add(WeaponName);

		}

		private bool ValidateWeapon(string Weapon, string TEAM_CONST)
		{

			if (
				(TEAM_CONST == HUMAN_TEAM && HumanWeaponsEnabled.IndexOf(Weapon) >= 0) || 
				(TEAM_CONST == ZOMBIE_TEAM && ZombieWeaponsEnabled.IndexOf(Weapon) >= 0)
				)
				return true;
			
			return false;
		}
		
		private void AdaptDamage()
		{
			double HumanCount = 1;
			double ZombieCount = 1;
			lock (TeamHuman)
			{
				HumanCount = (TeamHuman.Count == 0) ? 1 : TeamHuman.Count;
				ZombieCount = (TeamZombie.Count == 0) ? 1 : TeamZombie.Count;
			}
			double RatioHumansToZombies = (HumanCount / ZombieCount);
			int NewBulletDamage = 5;
			int OldBulletDamage = BulletDamage;
			
			
			if (RatioHumansToZombies >= 3.0)
			{
				NewBulletDamage = Against1Or2Zombies;
			}
			else if (RatioHumansToZombies < 3.0 && RatioHumansToZombies >= 1.5)
			{
				NewBulletDamage = AgainstAFewZombies;
			}
			else if (RatioHumansToZombies < 1.5 && RatioHumansToZombies >= 0.4)
			{
				NewBulletDamage = AgainstEqualNumbers;
			}
			else if (RatioHumansToZombies < 0.4 && RatioHumansToZombies > 0.20)
			{
				NewBulletDamage = AgainstManyZombies;
			}
			else // <= 0.20
			{
				NewBulletDamage = AgainstCountlessZombies;
			}
			
			// Cap damage for small numbers of players
			if (NewBulletDamage > AgainstManyZombies && HumanCount == 1 && ZombieCount <= 6)
			{
				NewBulletDamage = AgainstManyZombies;
			}

			
			if (NewBulletDamage != BulletDamage)
			{
				BulletDamage = NewBulletDamage;
				
				ExecuteCommand("procon.protected.send", "vars.bulletDamage", BulletDamage.ToString());
				
				TellAll("Bullet damage is now " + BulletDamage + "%", false);
			}
			
			if (BulletDamage != OldBulletDamage) DebugWrite("AdaptDamage: Humans(" + HumanCount + "):Zombies(" + ZombieCount + "), bullet damage set to " + BulletDamage + "% (was " + OldBulletDamage + "%)", 3);
			

		}

		#endregion


		#region Utilities

		private bool IsAdmin(string PlayerName)
		{
			bool AdminFlag = AdminUsers.Contains(PlayerName);
			if (AdminFlag)
			{
				TellAll(PlayerName + " is an admin", false);
				DebugWrite("IsAdmin: " + PlayerName + " is an admin", 3);
			}
			return AdminFlag;
		}

		private void ConsoleWrite(string str)
		{
			ExecuteCommand("procon.protected.pluginconsole.write", str);
		}
		
		private void LogChat(string Message, string Who)
		{
			ExecuteCommand("procon.protected.chat.write", "ZMODE to " + Who + "> " + Message);
		}
		
		private void LogChat(string Message)
		{
			ExecuteCommand("procon.protected.chat.write", "ZMODE> " + Message);
		}

		private void Announce(string Message)
		{
			if (GameState == GState.BetweenRounds) return;
			ExecuteCommand("procon.protected.send", "admin.yell", Message, AnnounceDisplayLength.ToString(), "all");
			LogChat(Message);
		}

		private void TellAll(string Message, bool AlsoYell)
		{
			// Yell and say
			if (GameState == GState.BetweenRounds) return;
			if (AlsoYell) Announce(Message);
			ExecuteCommand("procon.protected.send", "admin.say", Message, "all");
			if (!AlsoYell) LogChat(Message);
		}

		private void TellAll(string Message)
		{
			TellAll(Message, true);
		}
		
		private void TellTeam(string Message, string TeamId, bool AlsoYell)
		{
			// Yell and say
			if (GameState == GState.BetweenRounds) return;
			if (AlsoYell) ExecuteCommand("procon.protected.send", "admin.yell", Message, AnnounceDisplayLength.ToString(), "team", TeamId);
			ExecuteCommand("procon.protected.send", "admin.say", Message, "team", TeamId);
			LogChat(Message, (TeamId == HUMAN_TEAM) ? "humans" : "zombies");
		}

		private void TellTeam(string Message, string TeamId)
		{
			TellTeam(Message, TeamId, true);
		}
		
		private void TellPlayer(string Message, string SoldierName, bool AlsoYell)
		{
			// Yell and say
			if (GameState == GState.BetweenRounds) return;
			if (AlsoYell) ExecuteCommand("procon.protected.send", "admin.yell", Message, AnnounceDisplayLength.ToString(), "player", SoldierName);
			ExecuteCommand("procon.protected.send", "admin.say", Message, "player", SoldierName);
		}
				
		private void TellPlayer(string Message, string SoldierName)
		{
			TellPlayer(Message, SoldierName, true);
			LogChat(Message, SoldierName);
		}

		private void TellRules(string SoldierName)
		{
			int Delay = 4;
			List<String> Rules = new List<String>();
			// $$$ - custom message
			Rules.Add("US team are humans, RU team are zombies");
			Rules.Add("Zombies use knife/defib/repair tool only!");
			Rules.Add("Zombies are hard to kill");
			Rules.Add("Humans use guns only, no explosives!");
			Rules.Add("Zombies win by infecting all humans");
			if (ZombieKillLimitEnabled) 
			{
				int TotalCount = 0;
				lock (TeamHuman)
				{
					TotalCount = TeamHuman.Count + TeamZombie.Count;
				}
				Rules.Add("Humans win by killing " + GetKillsNeeded(TotalCount) + " zombies");
			}
			Rules.Add("When a zombie kills you, you are infected and moved to the zombie team!");
			
			String RuleNum = null;
			int i = 1;
			
			ThreadStart tellRules = delegate
			{
				try
				{
					foreach (String r in Rules)
					{
						RuleNum = "R" + i + " of " + Rules.Count + ") ";
						i = i + 1;
						TellPlayer(RuleNum + r, SoldierName);
						Sleep(Delay);
					}
				}
				catch (Exception e)
				{
					ConsoleException("tellRules: " + e.ToString());
				}
				finally
				{
					lock (NumRulesThreads)
					{
						NumRulesThreads.IntVal = NumRulesThreads.IntVal - 1;
						if (NumRulesThreads.IntVal < 0) NumRulesThreads.IntVal = 0;
					}
				}
			};
			
			bool IsTooMany = false;
			
			lock (NumRulesThreads)
			{
				if (NumRulesThreads.IntVal >= 4) 
				{
					IsTooMany = true;
				}
				else
				{
					NumRulesThreads.IntVal = NumRulesThreads.IntVal + 1;
				}
			}
			
			if (IsTooMany)
			{
				TellPlayer("Rules plugin is busy, try again in 15 seconds", SoldierName);
				return;
			}
			
			Thread t = new Thread(tellRules);

			t.Start();
			
			Thread.Sleep(2);
		}
		
		private void TellStatus(string SoldierName)
		{
			String status = "Zombie mode is disabled!";
			bool IsPlaying = false;

			
			if (ZombieModeEnabled) switch(GameState)
			{
				case GState.Idle:
					status = "No one is playing zombie mode (Idle)!";
					break;
				case GState.Waiting:
					lock (TeamHuman)
					{
						status = "Waiting for " + (MinimumHumans+MinimumZombies-TeamHuman.Count-TeamZombie.Count) + " more players to spawn (Waiting)!";
					}
					break;
				case GState.Playing:
					status = "A match is in progress (Playing)!";
					IsPlaying = true;
					break;
				case GState.CountingDown:
					status = "Counting down to next match/round (CountingDown)!";
					break;
				case GState.BetweenRounds:
					status = "ERROR (BetweenRounds)!"; // should never happen
					break;
				case GState.NeedSpawn:
					status = "ERROR (NeedSpawn)!"; // should never happen
					break;
				default:
					status = "Unknown";
					break;
			}
			
			TellPlayer("Status: " + status, SoldierName);
			
			if (IsPlaying)
			{
				lock (TeamHuman)
				{
					TellPlayer("HUMANS: N=" + TeamHuman.Count + ",K=" + KillTracker.GetZombiesKilled(), SoldierName, false);
					TellPlayer("ZOMBIES: N=" + TeamZombie.Count + ",D=" + BulletDamage, SoldierName, false);
				}
			}
		}

		private void Reset()
		{
			PlayerList.Clear();
			lock (TeamHuman)
			{
				TeamHuman.Clear();
				TeamZombie.Clear();
			}
			FreshZombie.Clear();
			PatientZeroes.Clear();
			Lottery.Clear();
			PlayerState.ClearAll();
			KnownPlayerCount = 0;
			ServerSwitchedCount = 0;
			PatientZero = null;
			GameState = GState.Idle;
			BulletDamage = 100;
			ExecuteCommand("procon.protected.send", "vars.bulletDamage", BulletDamage.ToString());
		}
		
		private void HaltMatch()
		{
			FreshZombie.Clear();
			Lottery.Clear();
			PlayerState.ResetPerMatch();
			PatientZero = null;
			GameState = GState.Waiting;
			BulletDamage = 100;
			ExecuteCommand("procon.protected.send", "vars.bulletDamage", BulletDamage.ToString());
		}

		private enum MessageType { Warning, Error, Exception, Normal };

		private String FormatMessage(String msg, MessageType type)
		{
			String prefix = "[^b" + GetPluginName() + "^n] ";

			if (type.Equals(MessageType.Warning))
				prefix += "^1^bWARNING^0^n: ";
			else if (type.Equals(MessageType.Error))
				prefix += "^1^bERROR^0^n: ";
			else if (type.Equals(MessageType.Exception))
				prefix += "^1^bEXCEPTION^0^n: ";

			return prefix + msg;
		}


		private void ConsoleLog(string msg, MessageType type)
		{
			ConsoleWrite(FormatMessage(msg, type));
		}

		private void ConsoleLog(string msg)
		{
			ConsoleLog(msg, MessageType.Normal);
		}

		private void ConsoleWarn(String msg)
		{
			ConsoleLog(msg, MessageType.Warning);
		}

		private void ConsoleError(String msg)
		{
			ConsoleLog(msg, MessageType.Error);
		}

		private void ConsoleException(String msg)
		{
			ConsoleLog(msg, MessageType.Exception);
		}

		private void DebugWrite(string msg, int level)
		{
			if (DebugLevel >= level) ConsoleLog("[" + level + "] " + msg, MessageType.Normal);
		}
		
		private void DebugValue(string Name, string BadValue, string Message, string NewValue)
		{
			DebugWrite("^b^8SetPluginVariable: ^0" + Name + "^n set to invalid value = " + BadValue + ", " + Message + ". Value forced to = " + NewValue, 0);
		}
		
		private void Sleep(int Seconds)
		{
			Thread.Sleep(Seconds * 1000);
		}

		private String PlayerNameMatch(string Name)
		{
			if (String.IsNullOrEmpty(Name) || PlayerList.Count == 0) return null;

			foreach (CPlayerInfo Player in PlayerList)
			{
				try
				{
					if (Regex.Match(Player.SoldierName, Name, RegexOptions.IgnoreCase).Success)
					{
						return Player.SoldierName;
					}
				}
				catch (Exception e)
				{
					return null;
				}
			}

			// Otherwise
			return null;
		}

		#endregion

	}

	enum ZombieModeTeam  {Human,Zombie};

	class ZombieModeKillTrackerKills
	{
		public int KillsAsZombie = 0;

		public int KillsAsHuman = 0;

		public int DeathsAsZombie = 0;

		public int DeathsAsHuman = 0;
		
		public int RulesViolations = 0; // never reset this value
	}

	class ZombieModeKillTracker
	{
		protected Dictionary<String, ZombieModeKillTrackerKills> Kills = new Dictionary<String, ZombieModeKillTrackerKills>();

		protected int ZombiesKilled = 0;

		protected int HumansKilled = 0;

		public void HumanKilled(String KillerName, String VictimName)
		{
			ZombieModeKillTrackerKills Killer = Kills[KillerName];
			Killer.KillsAsZombie++;

			ZombieModeKillTrackerKills Victim = Kills[VictimName];
			Victim.DeathsAsHuman++;

			HumansKilled++;
		}

		public void ZombieKilled(String KillerName, String VictimName)
		{
			ZombieModeKillTrackerKills Killer = Kills[KillerName];
			Killer.KillsAsHuman++;

			ZombieModeKillTrackerKills Victim = Kills[VictimName];
			Victim.DeathsAsZombie++;

			ZombiesKilled++;
		}

		protected Boolean PlayerExists(String PlayerName)
		{
			return Kills.ContainsKey(PlayerName);
		}

		public void AddPlayer(String PlayerName)
		{
			if (!PlayerExists(PlayerName))
				Kills.Add(PlayerName, new ZombieModeKillTrackerKills());
		}

		public void RemovePlayer(String PlayerName)
		{
			if (!PlayerExists(PlayerName))
				return;

			Kills.Remove(PlayerName);
		}

		public int GetZombiesKilled()
		{
			return ZombiesKilled;
		}

		public int GetHumansKilled()
		{
			return HumansKilled;
		}

		public int GetPlayerHumanDeathCount(String PlayerName)
		{
			return Kills[PlayerName].DeathsAsHuman;
		}
		
		public int GetViolations(String PlayerName)
		{
			return Kills[PlayerName].RulesViolations;
		}
		
		public void SetViolations(String PlayerName, int Times)
		{
			Kills[PlayerName].RulesViolations = Times;
		}

		public void ResetPerMatch()
		{
			HumansKilled = 0;
			ZombiesKilled = 0;
			
			foreach (String key in Kills.Keys)
			{
				ZombieModeKillTrackerKills Tracker = Kills[key];
				Tracker.KillsAsZombie = 0;
				Tracker.KillsAsHuman = 0;
				Tracker.DeathsAsZombie = 0;
				Tracker.DeathsAsHuman = 0;
			}
		}
		
		public void ResetPerRound()
		{
			ResetPerMatch();
		}

	}
	
	class APlayerState
	{
		// A bunch of counters and flags
		
		public int WelcomeCount = 0;
		
		public int SpawnCount = 0;
		
		public DateTime LastSpawnTime = DateTime.Now;
		
		public bool IsSpawned = false;
	}

	class ZombieModePlayerState
	{
		protected Dictionary<String, APlayerState> AllPlayerStates = new Dictionary<String, APlayerState>();
		
		public void AddPlayer(String soldierName)
		{
			if (AllPlayerStates.ContainsKey(soldierName)) return;
			AllPlayerStates[soldierName] = new APlayerState();
		}

		public int GetWelcomeCount(String soldierName)
		{
			if (!AllPlayerStates.ContainsKey(soldierName)) AddPlayer(soldierName);
			return AllPlayerStates[soldierName].WelcomeCount;
		}
		
		public void SetWelcomeCount(String soldierName, int n)
		{
			if (!AllPlayerStates.ContainsKey(soldierName)) AddPlayer(soldierName);
			AllPlayerStates[soldierName].WelcomeCount = n;
		}
		
		public int GetSpawnCount(String soldierName)
		{
			if (!AllPlayerStates.ContainsKey(soldierName)) AddPlayer(soldierName);
			return AllPlayerStates[soldierName].SpawnCount;
		}
		
		public void SetSpawnCount(String soldierName, int n)
		{
			if (!AllPlayerStates.ContainsKey(soldierName)) AddPlayer(soldierName);
			AllPlayerStates[soldierName].SpawnCount = n;
		}
		
		public void UpdateSpawnTime(String soldierName)
		{
			if (!AllPlayerStates.ContainsKey(soldierName)) AddPlayer(soldierName);
			AllPlayerStates[soldierName].LastSpawnTime = DateTime.Now;			
		}
		
		public bool IdleTimeExceedsMax(String soldierName, double maxSecs)
		{
			if (!AllPlayerStates.ContainsKey(soldierName)) return false;
			APlayerState ps = AllPlayerStates[soldierName];
			if (ps.IsSpawned == true) return false;
			// Fix for idle kicks before someone spawns the first time!
			if (maxSecs < 300 && ps.SpawnCount < 2) return false;
			DateTime last = ps.LastSpawnTime;
			TimeSpan time = DateTime.Now - last;
			return(time.TotalSeconds > maxSecs);
		}
		
		
		public void SetSpawned(String soldierName, bool SpawnStatus)
		{
			if (!AllPlayerStates.ContainsKey(soldierName)) AddPlayer(soldierName);
			AllPlayerStates[soldierName].IsSpawned = SpawnStatus;			
		}

		public bool GetSpawned(String soldierName)
		{
			if (!AllPlayerStates.ContainsKey(soldierName)) AddPlayer(soldierName);
			return AllPlayerStates[soldierName].IsSpawned;			
		}


		public double GetLastSpawnTime(String soldierName)
		{
			if (!AllPlayerStates.ContainsKey(soldierName)) return 0;
			APlayerState ps = AllPlayerStates[soldierName];
			DateTime last = ps.LastSpawnTime;
			TimeSpan time = DateTime.Now - last;
			return(time.TotalSeconds);
		}
		
		

		public void ResetPerMatch()
		{
			foreach (String key in AllPlayerStates.Keys)
			{
				SetSpawnCount(key, 0);
			}
		}
		
		public void ResetPerRound()
		{
			ResetPerMatch();
			
			foreach (String key in AllPlayerStates.Keys)
			{
				SetSpawnCount(key, 0);
			}
		}

		public void ClearAll()
		{
			AllPlayerStates.Clear();
		}
	}
	
	class SynchronizedNumbers
	{
		public int IntVal = 0;
		public double DoubleVal = 0;
	}
	
	/* Always at the end of the file */

	class DescriptionClass
	{
		public String HTML = "x";
/*
<h1>THIS PLUGIN IS STILL UNDER DEVELOPMENT!</h1>
<h2>Description</h2>

<p>BF3 Zombie Mode is a ProCon 1.0 plugin that turns Team Deathmatch into the _Infected_ or _Zombie_ variant play.</p>

<p><b>NOTE:</b> the game server <b>must</b> be run in unranked mode (vars.ranked false). Zombie Mode will not work on a ranked server.</p>

<p>When there are a minimum number of players spawned, all of the players are moved to the human team (US), except for one zombie (RU). With default settings, Zombies can use knife/defib/repair tool <i>only</i> for weapons and Humans can use any weapon <i>except</i> explosives (grenades, C4, Claymores) or missiles; the allowed/forbidden weapon settings are configurable. Zombies are hard to kill. Every time a zombie kills a human, the human becomes infected and is moved to the zombie team. Humans win by killing a minimum number of zombies (configurable) or when all the zombies leave the server. Zombies win by infecting all the humans or when all the humans leave the server.</p>

<p>The maximum number of players is half your server slots, so if you have a 64-slot server, you can have a max of 32 players.</p>

<p>The plugin is driven by players spawning. Until a minimum number of individual players spawns, the match won't start. See <b>Minimum Zombies</b> and <b>Minimum Humans</b> below.</p>

<p>Recommended server settings are here: <a href=http://www.phogue.net/forumvb/forum.php>TBD</a></p>

<h2>Settings</h2>
<p>There are a large number of configurable setttings, divided into sections.</p>

<h3>Admin Settings</h3>
<p><b>Zombie Mode Enabled</b>: <i>On/Off</i>, default is <i>On</i>.</p>

<p><b>Command Prefix</b>: Chat text that represents an in-game command, default is <i>!zombie</i>. May be set to a single character, for example <i>@</i>, so that instead of the rules command being <i>!zombie rules</i>, the command would just be <i>@rules</i>.</p>

<p><b>Announce Display Length</b>: Time in seconds that announcements are shown as yells, default is <i>10</i>.</p>

<p><b>Warning Display Length</b>: Time in seconds that warnings are shown as yells, default is <i>15</i>.</p>

<p><b>Human Max Idle Seconds</b>: Time in seconds that a human is allowed to be idle (no spawns and no kills/deaths) before being kicked. This idle time applies only when a match is in progress. Since zombies can't win unless they can kill humans, the match can stall if a human remains idle and never spawns. The idle time for humans should therefore be relatively short. The default value is <i>120</i> seconds, or 2 minutes.</p>

<p><b>Max Idle Seconds</b>: Time in seconds that any player is allowed to be idle (no spawns and no kills/deaths) before being kicked. This idle time applies as long as Zombie Mode is enabled (On). The default value is <i>600</i> seconds, or 10 minutes.</p>

<p><b>Warns Before Kick For Rules Violations</b>: Number of warnings given before a player is kicked for violating the Zombie Mode rules, particularly for using a forbidden weapon type. The default value is <i>1</i>.</p>

<p><b>Debug Level</b>: A number that represents the amount of debug logging  that is sent to the plugin.log file in PRoCon. The higher the number, the more spam is logged. The default value is <i>2</i>. Note: if you have a problem using the plugin, set your <b>Debug Level</b> to <i>5</i> and save the plugin.log for posting to phogue.net.</p>

<p><b>Admin Users</b>: A table of soldier names that will be permitted to use in-game admin commands (see below). The default value is <i>PapaCharlieNiner</i>.</p>

<h3>Game Settings</h3>

<p><b>Max Players</b>: Any players that try to join above this number will be kicked immediately. Make sure this number is equal to or less than <b>half</b> of your maximum slot count for your game server. For example, if you have a 48 slot server, set the maximum no higher than 24. This is a limitation of BF3 game servers, you can only use half your slots for this mode. The default value is <i>32</i>.</p>

<p><b>Minimum Zombies</b>: The number of players that will start a match as zombies. The default value is <i>1</i>.</p>

<p><b>Minimum Humans</b>: The number of players that will start a match as humans. The default value is <i>3</i>. Note: the sum of <b>Minimum Zombies</b> and <b>Minimum Humans</b> (default: 4) is the minimum number of players needed to start a match. Until that minimum number spawns into the round, the Zombie Mode will wait and normal Team Deathmatch rules will apply.</p>

<p><b>Zombie Kill Limit Enabled</b>: <i>On/Off</i>, default is <i>On</i>. If <i>On</i>, Humans must kill the number of zombies specified in <b>Zombies Killed To Survive</b> in order to win. If <i>Off</i>, the last human left standing is the winner.</p>

<p><b>Zombies Killed To Survive</b>: The number of zombies that the human team must kill in order to win the match. The default value is <i>50</i>.</p>

<p><b>Deaths Needed To Be Infected</b>: The number of times a human must be killed by a zombie before the human becomes infected and is forced to switch to the zombie team. The default value is <i>1</i>.</p>

<p><b>Infect Suicides</b>: <i>On/Off</i>, default is <i>On</i>. If <i>On</i>, a human that suicides becomes a zombie. If <i>Off</i>, the human stays human but still dies.</p>

<p><b>New Players Join Humans</b>: <i>On/Off</i>, default is <i>On</i>. If <i>On</i>, any new players that join the server will be force moved to the human team. If <i>Off</i>, any new players that join the server will be force moved to the zombie team.</p>

<p><b>Rematch Enabled</b>: <i>On/Off</i>, default is <i>On</i>.  If <i>On</i>, when a team wins and the match is over, a new match will be started after a short countdown during the same map round/level. When <i>Off</i>, the current map round/level will be ended, the winning team will be declared the winner of the whole round and the next map round/level will be loaded and started. Turning this <i>On</i> makes matches happen quicker and back-to-back on the same map, while turning this <i>Off</i> takes longer between matches, but lets your players try out all the maps in your rotation.</p>

<h3>Human Damage Percentage</h3>

<p>At the start of a match, when there is only one or a very few zombies, zombies have to be very tough and hard to kill or else they will never get close to a human to infect them. This is implemented with vars.bulletDamage. The values of the following settings specify the vars.bulletDamage depending on the number of zombies that the humans face. The lower the numbers, the harder the zombies are to kill.</p>

<p><b>Against 1 Or 2 Zombies</b>: the default value is <i>5</i>. When humans outnumber zombies 3-to-1 or more (e.g., 18 vs 1).</p>

<p><b>Against A Few Zombies</b>: the default value is <i>10</i>. When humans outnumber zombies between 3-to-1 and 3-to-2 (e.g., 12 vs 7).</p>

<p><b>Against Equal Numbers</b>: the default value is <i>15</i>. When humans and zombies are roughly equal in number, betwee 3-to-2 and 2-to-3 (e.g., 8 vs 11).</p>

<p><b>Against Many Zombies</b>: the default value is <i>30</i>. When zombies outnumber humans between 3-to-2 and 4-to-1 (e.g., 5 vs 14).</p>

<p><b>Against Countless Zombies</b>: the default value is <i>100</i>. When zombies outnumber humans 4-to-1 or more (e.g., 2 vs 17).</p>

<h3>Zombie Weapons</h3>

<p>This is a lists of weapon types zombies are allowed to use. Weapons that are <i>On</i> are allowed, weapons that are <i>Off</i> are not allowed and will result in warnings and a kick if a zombie player uses them. The default settings allow knife, melee, defib and repair tool and do not allow anything else.</p>

<h3>Human Weapons</h3>

<p>This is a lists of weapon types humans are allowed to use. Weapons that are <i>On</i> are allowed, weapons that are <i>Off</i> are not allowed and will result in warnings and a kick if a human player uses them. The default settings are all guns allowed and do not allow explosives (grenades, C4, claymore, M320 noob tube, etc.) or missiles (RPG, SMAW).</p>

<h2>Commands</h2>

<p>These are in-game commands for managing players and the mode. Some are available to all players, some are for admins only (see <b>Admin Users</b> in <b>Settings</b>). For all of the following descriptions, the default <b>Command Prefix</b> of <i>!zombie</i> is assumed. If you set a different prefix, substitute your prefix into the following.</p>

<h3>Commands for all players</h3>

<p><b>!zombie help</b>: Shows list of commands available to the player.</p>

<p><b>!zombie idle</b>: Shows how long the player typing the command has been idle and whether or not the player is spawned into the round.</p>

<p><b>!zombie rules</b>: Scrolls all of the Zombie Mode rules to the player.</p>

<p><b>!zombie status</b>: Shows the status of the match to the player, for example, if the mode is waiting for more players to join, or if it is Idle (waiting for a player to spawn so that it can reset), counting down to the next match, etc. If a match is in progress (Playing), it also shows some statistics for the match, for example:
<pre>
HUMANS: N=4,K=23
ZOMBIES: N=16,D=100
</pre>
Where <b>N</b> is the number of players on that team, <b>K</b> is the number of zombies the humans have killed, and <b>D</b> is the current bullet damage.</p>

<p><b>!zombie warn</b> <i>name</i> <i>reason</i>: Sends a warning yell to the player with the specified <i>name</i>. The <i>reason</i> is one or more words. For example:
<pre>
!zombie warn PapaCharlie9 Quit glitching u noob!
</pre>
will yell the message 'Quit glitching u noob!' to PapaCharlie9.</p>

<h3>Commands for Admins only</h3>

<p><b>!zombie force</b>: Force a match to start, even if there are not enough players. Useful if players aren't spawning fast enough to get a match started or if the plugin gets into a confused state (please report a bug so we can fix it).</p>

<p><b>!zombie heal</b> <i>name</i>: Kills the player with the specified <i>name</i> and if they are on the zombie team, force moves them to the human team. Useful for correting mistakes that the plugin might make (please report a bug so we can fix it).</p>

<p><b>!zombie infect</b> <i>name</i>: Kills the player with the specified <i>name</i> and if they are on the human team, force moves them to the zombie team. Useful for dealing with human glitchers or idlers.</p>

<p><b>!zombie kick</b> <i>name</i> <i>reason</i>: Kicks the player with the specified <i>name</i>. The <i>reason</i> is one or more words. For example:
<pre>
!zombie kick PapaCharlie9 Too much glitching!
</pre>
will kick PapaCharlie9 for 'Too much glitching!'. Useful to get rid of cheaters.</p>

<p><b>!zombie kill</b> <i>name</i>: Kills the player with the specified <i>name</i>. Useful to force a glitcher to respawn or a player ignoring warnings to pay more attention.</p>

<p><b>!zombie mode</b> <i>on</i>/<i>off</i>: Changes the <b>Zombie Mode Enabled</b> setting. Useful if you want to switch a normal TDM round to Zombie Mode or vice versa.</p>

<p><b>!zombie next</b>: Ends the current map round/level and loads the next map round/level. Useful to try a new map if you have <b>Rematch Enabled</b> set to <i>On</i>.</p>

<p><b>!zombie rematch</b> <i>on</i>/<i>off</i>: Changes the <b>Rematch Enabled</b> setting</p>

<p><b>!zombie restart</b>: Restarts the current map round/level. Useful if the tickets/kills for TDM are getting close to the maximum to end a normal TDM round, which might happen in the middle of a quick rematch.</p>

<h2>Hints & Tips</h2>
<p>TBD</p>

<h2>Download</h2>

<p>Do links work? <a href=https://github.com/m4xxd3v/BF3ZombieMode/downloads>Download from this GitHub page!</a></p>

<h3>Changelog</h3>
<blockquote><h4>0.1.1 (15-OCT-2012)</h4>
	- fixes after first round of live testing<br/>
</blockquote>
<blockquote><h4>0.1.0 (14-OCT-2012)</h4>
	- initial version<br/>
</blockquote>
";
*/
	}
}


