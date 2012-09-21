/*  Copyright 2010 Geoffrey 'Phogue' Green

    This file is part of BFBC2 PRoCon.

    BFBC2 PRoCon is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    BFBC2 PRoCon is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with BFBC2 PRoCon.  If not, see <http://www.gnu.org/licenses/>.
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

		private string CommandPrefix = "!";

		private int AnnounceDisplayLength = 3;

		private bool MakeTeamsRequested = false;

		private NoticeDisplayType AnnounceDisplayType = NoticeDisplayType.yell;

		private int WarningDisplayLength = 10;

		private List<String> AdminUsers = new List<String>();

		private List<String> PlayerKickQueue = new List<String>();

		private ZombieModeKillTracker KillTracker = new ZombieModeKillTracker();

		#endregion


		#region GamePlayVars

		private List<CPlayerInfo> PlayerList = new List<CPlayerInfo>();

		private bool ZombieModeEnabled = false;

		private int MaxPlayers = 12;

		private int MinimumHumans = 1;

		private int MinimumZombies = 1;

		private int DeathsNeededToBeInfected = 1;

		private int ZombiesKilledToSurvive = 50;

		private bool ZombieKillLimitEnabled = true;

		private int HumanBulletDamage = 0;

		private int ZombieBulletDamage = 0;

		private bool InfectSuicides = true;

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
			"Knife_RazorBlade"
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

		#region EventHandlers

		/** EVENT HANDLERS **/
		public override void OnPlayerKickedByAdmin(string SoldierName, string reason) 
		{
			if (ZombieModeEnabled == false)
				return;

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

		public override void OnPlayerAuthenticated(string SoldierName, string guid)
		{
			if (ZombieModeEnabled == false)
				return;

			if (PlayerList.Count <= MaxPlayers)
				return;

			base.OnPlayerAuthenticated(SoldierName, guid);
			
			PlayerKickQueue.Add(SoldierName);

			ThreadStart kickPlayer = delegate
			{
				try
				{
					Thread.Sleep(10000);
					ExecuteCommand("procon.protected.tasks.add", "CZombieMode", "0", "1", "1", "procon.protected.send", "admin.kickPlayer", SoldierName, String.Concat("Sorry, zombie mode is enabled and all slots are full :( Please join when there are less than ", MaxPlayers.ToString(), " players"));
					while (true)
					{
						if (!PlayerKickQueue.Contains(SoldierName))
							break;

						ExecuteCommand("procon.protected.tasks.add", "CZombieMode", "0", "1", "1", "procon.protected.send", "admin.kickPlayer", SoldierName, String.Concat("Sorry, zombie mode is enabled and all slots are full :( Please join when there are less than ", MaxPlayers.ToString(), " players"));
						Thread.Sleep(500);
					}
				}
				catch (System.Exception e)
				{

				}
			};

			Thread t = new Thread(kickPlayer);

			t.Start();
		}
		
		public override void OnPlayerJoin(string SoldierName)
		{
			if (ZombieModeEnabled)
				MakeHuman(SoldierName);

			KillTracker.AddPlayer(SoldierName);
		}
		
		public override void OnPlayerKilled(Kill info)
		{
			if (ZombieModeEnabled == false)
				return;

			// Killed by admin?
			if (info.DamageType == "Death")
				return;

			String KillerName = info.Killer.SoldierName.ToString();

			String KillerTeam = info.Killer.TeamID.ToString();

			String VictimName = info.Victim.SoldierName.ToString();

			String DamageType = info.DamageType;

			if (KillerName == VictimName)
			{
				if (InfectSuicides)
				{
					Infect("Suicide ", VictimName);
					return;
				}
			}

			if (KillerName == "")
			{
				if (InfectSuicides)
				{
					Infect("Misfortune ", VictimName);
					return;
				}
			}


			if (ValidateWeapon(info.DamageType, KillerTeam) == false)
			{
				ConsoleLog(String.Concat(KillerName, " invalid kill with ", info.DamageType, "!"));

				KillPlayer(KillerName, "Bad weapon choice!");

				return;
			}



			if (KillerTeam == HUMAN_TEAM)
			{
				KillTracker.ZombieKilled(KillerName, VictimName);

				ConsoleLog(String.Concat("Human ", KillerName, " just killed zombie ", VictimName, " with ", DamageType));
			}
			else
			{
				ConsoleLog(String.Concat("Zombie ",KillerName, " just killed human ", VictimName, " with ", DamageType));

				KillTracker.HumanKilled(KillerName, VictimName);

				if (KillTracker.GetPlayerHumanDeathCount(VictimName) == DeathsNeededToBeInfected)
					Infect(KillerName, VictimName);
			}

		}

		public override void OnListPlayers(List<CPlayerInfo> Players, CPlayerSubset Subset)
		{
			PlayerList = Players;

			foreach (CPlayerInfo Player in Players)
			{
				KillTracker.AddPlayer(Player.SoldierName.ToString());
			}
		}

		public override void OnSquadChat(string PlayerName, String Message, int TeamId, int SquadId)
		{
			if (!IsAdmin(PlayerName))
				return;

			List<string> MessagePieces = new List<string>(Message.Split(' '));

			String Command = MessagePieces[0];

			if (!Command.StartsWith(CommandPrefix))
				return;

			switch (Command.TrimStart(CommandPrefix.ToCharArray()))
			{
				case "infect":
					if (MessagePieces.Count != 2) return;
					Infect("Admin", MessagePieces[1]);
					break;
				case "heal":
					if (MessagePieces.Count != 2) return;
					MakeHuman(MessagePieces[1]);
					break;
				case "teams":
					MakeTeamsRequest();
					break;
				case "restart":
					RestartRound();
					break;
				case "next":
					NextRound();
					break;
				case "zombie":
					if (MessagePieces.Count < 2) return;
					if (MessagePieces[1] == "on")
						ZombieModeEnabled = true;
					else if (MessagePieces[1] == "off")
						ZombieModeEnabled = false;
					break;
				case "rules":
					break;
				case "warn":
					if (MessagePieces.Count < 3) return;
					string WarningMessage = String.Join(" ", MessagePieces.GetRange(2, MessagePieces.Count - 2).ToArray());

					ConsoleLog(WarningMessage);
					Warn(MessagePieces[1], WarningMessage);
					break;

				case "kill":
					if (MessagePieces.Count < 2) return;
					string KillMessage = (MessagePieces.Count >= 3) ? String.Join(" ", MessagePieces.GetRange(2, MessagePieces.Count - 2).ToArray()) : "";

					ConsoleLog(KillMessage);
					KillPlayer(MessagePieces[1], KillMessage);
					break;

				case "kick":
					if (MessagePieces.Count < 2) return;
					string KickMessage = (MessagePieces.Count >= 3) ? String.Join(" ", MessagePieces.GetRange(2, MessagePieces.Count - 2).ToArray()) : "";

					KickPlayer(MessagePieces[1], KickMessage);
					break;
				case "test":
					ConsoleLog("loopz");
					ConsoleLog(FrostbitePlayerInfoList.Values.Count.ToString());
					foreach (CPlayerInfo Player in FrostbitePlayerInfoList.Values)
					{
						ConsoleLog("looping");
						String testmessage = Player.SoldierName;
						ConsoleLog(testmessage);
					}
					break;
			}

		}

		#endregion


		#region PluginMethods
		/** PLUGIN RELATED SHIT **/
		#region PluginEventHandlers
		public void OnPluginLoaded(string strHostName, string strPort, string strPRoConVersion)
		{
			RegisterEvents(GetType().Name, "OnPlayerKilled", "OnListPlayers", "OnSquadChat", "OnPlayerAuthenticated", "OnPlayerKickedByAdmin");
		}

		public void OnPluginEnable()
		{
			//System.Diagnostics.Debugger.Break();
			ConsoleLog(String.Concat("^b", GetPluginName(), " ^2Enabled... It's Game Time!"));
		}

		public void OnPluginDisable()
		{
			ConsoleLog(String.Concat("^b", GetPluginName(), " ^2Disabled :("));
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
			return "0.1.0";
		}

		public string GetPluginAuthor()
		{
			return "m4xxd3v";
		}

		public string GetPluginWebsite()
		{
			return "http://google.com";
		}

		public string GetPluginDescription()
		{
			return "This plugin enables a zombie infection mode type game play";
		}


		// Plugin variables
		public List<CPluginVariable> GetDisplayPluginVariables()
		{
			List<CPluginVariable> lstReturn = new List<CPluginVariable>();

			lstReturn.Add(new CPluginVariable("Game Settings|Zombie Mode Enabled", typeof(enumBoolYesNo), ZombieModeEnabled ? enumBoolYesNo.Yes : enumBoolYesNo.No));

			lstReturn.Add(new CPluginVariable("Admin Settings|Command Prefix", CommandPrefix.GetType(), CommandPrefix));

			lstReturn.Add(new CPluginVariable("Admin Settings|Announce Display Length", AnnounceDisplayLength.GetType(), AnnounceDisplayLength));

			lstReturn.Add(new CPluginVariable("Admin Settings|Warning Display Length", WarningDisplayLength.GetType(), WarningDisplayLength));

			lstReturn.Add(new CPluginVariable("Admin Settings|Admin Users", typeof(string[]), AdminUsers.ToArray()));

			lstReturn.Add(new CPluginVariable("Game Settings|Max Players", MaxPlayers.GetType(), MaxPlayers));

			lstReturn.Add(new CPluginVariable("Game Settings|Minimum Zombies", MinimumZombies.GetType(), MinimumZombies));

			lstReturn.Add(new CPluginVariable("Game Settings|Minimum Humans", MinimumHumans.GetType(), MinimumHumans));

			lstReturn.Add(new CPluginVariable("Game Settings|Zombie Kill Limit Enabled", typeof(enumBoolOnOff), ZombieKillLimitEnabled ? enumBoolOnOff.On : enumBoolOnOff.Off));

			if (ZombieKillLimitEnabled)
				lstReturn.Add(new CPluginVariable("Game Settings|Zombies Killed To Survive", ZombiesKilledToSurvive.GetType(), ZombiesKilledToSurvive));

			lstReturn.Add(new CPluginVariable("Game Settings|Deaths Needed To Be Infected", DeathsNeededToBeInfected.GetType(), DeathsNeededToBeInfected));
			

			lstReturn.Add(new CPluginVariable("Game Settings|Infect Suicide Players", typeof(enumBoolOnOff), InfectSuicides ? enumBoolOnOff.On : enumBoolOnOff.Off));


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

		#endregion


		#region PlayerPunishmentCommands

		private void Warn(String PlayerName, String Message)
		{
			ExecuteCommand("procon.protected.send", "admin.yell", Message, WarningDisplayLength.ToString(), "all", PlayerName);
		}

		private void KillPlayer(string PlayerName, string Reason)
		{
			ExecuteCommand("procon.protected.send", "admin.killPlayer", PlayerName);

			if (Reason.Length > 0)
				Announce(String.Concat(PlayerName, ": ", Reason));
		}

		private void KickPlayerDelayed(string PlayerName, string Reason, int SecsToDelay)
		{
			ExecuteCommand("procon.protected.tasks.add", "ZombieKickUser", SecsToDelay.ToString(), "1", "1", "admin.kickPlayer", PlayerName, Reason);
		}

		private void KickPlayer(string PlayerName, string Reason)
		{
			ExecuteCommand("procon.protected.send", "admin.kickPlayer", PlayerName, Reason);

			if (Reason.Length > 0)
				Announce(String.Concat(PlayerName, "kicked for: ", Reason));
		}

		#endregion

		private void Rules(String PlayerName)
		{

		}

		#region TeamMethods

		private void ShufflePlayersList()
		{
			List<CPlayerInfo> randomList = new List<CPlayerInfo>();

			Random r = new Random();
			int randomIndex = 0;
			while (PlayerList.Count > 0)
			{
				randomIndex = r.Next(0, PlayerList.Count); //Choose a random object in the list
				randomList.Add(PlayerList[randomIndex]); //add it to the new, random list
				PlayerList.RemoveAt(randomIndex); //remove to avoid duplicates
			}

			PlayerList = randomList; //return the new random list

			if (MakeTeamsRequested == true)
				MakeTeams();

		}

		private void RequestPlayersList()
		{
			ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
		}

		private void MakeTeamsRequest()
		{
			Announce("Teams are being generated!");

			ConsoleLog("Teams being generated");

			MakeTeamsRequested = true;

			RequestPlayersList();
		}

		private void MakeTeams()
		{
			MakeTeamsRequested = false;

			ShufflePlayersList();

			int ZombieCount = 0;
			foreach (CPlayerInfo Player in PlayerList)
			{
				if (ZombieCount < MinimumZombies)
				{
					ZombieCount++;
					ConsoleLog(String.Concat("Making ", Player, " a zombie"));
					MakeZombie(Player.SoldierName);

				}
				else
				{
					ConsoleLog(String.Concat("Making ", Player, " a human"));
					MakeHuman(Player.SoldierName);
				}
			}

			ConsoleLog("Team generation complete.");
		}

		public void Infect(string Carrier, string Victim)
		{
			Announce(String.Concat(Carrier, " just infected ", Victim));

			MakeZombie(Victim);
		}

		private void MakeHuman(string PlayerName)
		{
			Announce(String.Concat(PlayerName, " has join the fight for survival!"));

			ExecuteCommand("procon.protected.send", "admin.movePlayer", PlayerName, HUMAN_TEAM, BLANK_SQUAD, FORCE_MOVE);
		}

		private void MakeZombie(string PlayerName)
		{
			ExecuteCommand("procon.protected.send", "admin.movePlayer", PlayerName, ZOMBIE_TEAM, BLANK_SQUAD, FORCE_MOVE);
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

		#endregion

		private bool IsAdmin(string PlayerName)
		{
			return AdminUsers.IndexOf(PlayerName) >= 0 ? true : false;
		}

		private void ConsoleLog(string str)
		{
			ExecuteCommand("procon.protected.pluginconsole.write", str);
		}

		private void Announce(string Message)
		{
			ExecuteCommand("procon.protected.send", "admin.yell", Message, AnnounceDisplayLength.ToString(), AnnounceDisplayType.ToString());
		}

		private void Reset()
		{
			PlayerList.Clear();
		}

	}

	enum ZombieModeTeam  {Human,Zombie};

	struct ZombieModeKillTrackerKills
	{
		public int KillsAsZombie;

		public int KillsAsHuman;

		public int DeathsAsZombie;

		public int DeathsAsHuman;
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
	}

}


