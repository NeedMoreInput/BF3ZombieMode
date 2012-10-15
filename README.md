BF3 Zombie Mode
===============

##STILL IN DEVELOPMENT, DO NOT USE YET

<h2>Description</h2>

<p>BF3 Zombie Mode is a ProCon 1.0 plugin that turns Team Deathmatch into the _Infected_ or _Zombie_ variant play.</p>

<p><b>NOTE:</b> the game server <b>must</b> be run in unranked mode (vars.ranked false). Zombie Mode will not work on a ranked server.</p>

<p>When there are a minimum number of players spawned, all of the players are moved to the human team (US), except for one zombie (RU). With default settings, Zombies can use knife/defib/repair tool <i>only</i> for weapons and Humans can use any weapon <i>except</i> explosives (grenades, C4, Claymores) or missiles; the allowed/forbidden weapon settings are configurable. Zombies are hard to kill. Every time a zombie kills a human, the human becomes infected and is moved to the zombie team. Humans win by killing a minimum number of zombies (configurable) or when all the zombies leave the server. Zombies win by infecting all the humans or when all the humans leave the server.</p>

<p>The maximum number of players is half your server slots, so if you have a 64-slot server, you can have a max of 32 players.</p>

<p>The plugin is driven by players spawning. Until a minimum number of individual players spawns, the match won't start. See <b>Minimum Zombies</b> and <b>Minimum Humans</b> below.</p>

<p>Recommended server settings are here: <a href="http://www.phogue.net/forumvb/forum.php">TBD</a></p>

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

<p><b>Against Many Zombies</b>: the default value is <i>30</i>. When zombies outnumber humans between 3-to-2 and 3-to-1 (e.g., 5 vs 14).</p>

<p><b>Against Countless Zombies</b>: the default value is <i>100</i>. When zombies outnumber humans 3-to-1 or more (e.g., 2 vs 17).</p>

<h3>Zombie Weapons</h3>

<p>This is a lists of weapon types zombies are allowed to use. Weapons that are <i>On</i> are allowed, weapons that are <i>Off</i> are not allowed and will result in warnings and a kick if a zombie player uses them. The default settings allow knife, melee, defib and repair tool and do not allow anything else.</p>

<h3>Human Weapons</h3>

<p>This is a lists of weapon types humans are allowed to use. Weapons that are <i>On</i> are allowed, weapons that are <i>Off</i> are not allowed and will result in warnings and a kick if a human player uses them. The default settings are all guns allowed and do not allow explosives (grenades, C4, claymore, M320 noob tube, etc.) or missiles (RPG, SMAW).</p>

<h2>Commands</h2>
<p>TBD</p>

<h2>Development</h2>
<p>TBD</p>

<h2>Download</h2>

<p>Do links work? <a href=https://github.com/m4xxd3v/BF3ZombieMode/downloads>Download from this GitHub page!</a></p>

<h3>Changelog</h3>


