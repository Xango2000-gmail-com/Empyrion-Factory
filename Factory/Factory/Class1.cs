﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Eleon.Modding;
//using ProtoBuf;
using YamlDotNet.Serialization;
using Eleon;


namespace Factory
{
    public class MyEmpyrionMod : ModInterface, IMod
    {
        public static string ModShortName = "Factory";
        public static string ModVersion = ModShortName + " v0.0.12 made by Xango2000 (Tested: Alpha 1.2 build 3124)";
        public static string ModPath = "..\\Content\\Mods\\" + ModShortName + "\\";
        internal static bool debug = false;
        internal static IModApi modApi;
        internal static Dictionary<int, Storage.StorableData> SeqNrStorage = new Dictionary<int, Storage.StorableData> { };
        public int thisSeqNr = 2000;
        internal static SetupYaml.Root SetupYamlData = new SetupYaml.Root { };
        public ItemStack[] blankItemStack = new ItemStack[] { };
        private Dictionary<string, string> LastUseLog = new Dictionary<string, string> { };
        private Dictionary<int, string> Players = new Dictionary<int, string> { };

        List<string> OnlinePlayers = new List<string> { };
        bool LiteVersion = false;
        bool Disable = false;
        //########################################################################################################################################################
        //################################################ This is where the actual Empyrion Modding API stuff Begins ############################################
        //########################################################################################################################################################
        public void Game_Start(ModGameAPI gameAPI)
        {
            Storage.GameAPI = gameAPI;
            if (debug) { File.WriteAllText(ModPath + "ERROR.txt", ""); }
            if (debug) { File.WriteAllText(ModPath + "debug.txt", ""); }
            SetupYaml.Setup();
            CommonFunctions.Log("--------------------" + CommonFunctions.TimeStamp() + "----------------------------");
        }

        public void Game_Event(CmdId cmdId, ushort seqNr, object data)
        {
            try
            {
                switch (cmdId)
                {
                    case CmdId.Event_ChatMessage:
                        //Triggered when player says something in-game
                        ChatInfo Received_ChatInfo = (ChatInfo)data;
                        if (!Disable)
                        {
                            string msg = Received_ChatInfo.msg.ToLower();
                            if (msg == SetupYamlData.ReinitializeCommand) //Reinitialize
                            {
                                SetupYaml.Setup();
                                API.ServerTell(Received_ChatInfo.playerId, ModShortName, "Reinitialized", true);
                            }
                            else if (msg == "/mods" || msg == "!mods")
                            {
                                API.ServerTell(Received_ChatInfo.playerId, ModShortName, ModVersion, true);
                            }
                            else if (msg == SetupYamlData.FinishBlueprint.Command.ToLower()) //Finish Blueprint Command
                            {
                                try
                                {
                                    Storage.StorableData function = new Storage.StorableData
                                    {
                                        function = "FBP",
                                        Match = Convert.ToString(Received_ChatInfo.playerId),
                                        Requested = "PlayerInfo",
                                        ChatInfo = Received_ChatInfo
                                    };
                                    API.PlayerInfo(Received_ChatInfo.playerId, function);
                                }
                                catch
                                {
                                    CommonFunctions.Debug("FBP Fail: at ChatInfo");
                                }
                            }
                            else if (msg == SetupYamlData.FactoryExtract.Command.ToLower()) //Factory Extract Command
                            {
                                try
                                {
                                    Storage.StorableData function = new Storage.StorableData
                                    {
                                        function = "FE",
                                        Match = Convert.ToString(Received_ChatInfo.playerId),
                                        Requested = "PlayerInfo",
                                        ChatInfo = Received_ChatInfo
                                    };
                                    API.PlayerInfo(Received_ChatInfo.playerId, function);
                                }
                                catch
                                {
                                    CommonFunctions.Debug("FE Fail: at ChatInfo");
                                }
                            }
                        }
                        break;


                    case CmdId.Event_Player_Connected:
                        //Triggered when a player logs on
                        Id Received_PlayerConnected = (Id)data;
                        string SteamID = modApi.Application.GetPlayerDataFor(Received_PlayerConnected.id).Value.SteamId;
                        if (!OnlinePlayers.Contains(SteamID))
                        {
                            OnlinePlayers.Add(SteamID);
                        }
                        if (OnlinePlayers.Count > 10 && LiteVersion)
                        {
                            Disable = true;
                        }
                        break;


                    case CmdId.Event_Player_Disconnected:
                        //Triggered when a player logs off
                        Id Received_PlayerDisconnected = (Id)data;
                        break;


                    case CmdId.Event_Player_ChangedPlayfield:
                        //Triggered when a player changes playfield
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_ChangePlayfield, (ushort)CurrentSeqNr, new IdPlayfieldPositionRotation( [PlayerID], [Playfield Name], [PVector3 position], [PVector3 Rotation] ));
                        IdPlayfield Received_PlayerChangedPlayfield = (IdPlayfield)data;
                        break;


                    case CmdId.Event_Playfield_Loaded:
                        //Triggered when a player goes to a playfield that isnt currently loaded in memory
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Load_Playfield, (ushort)CurrentSeqNr, new PlayfieldLoad( [float nSecs], [string nPlayfield], [int nProcessId] ));
                        PlayfieldLoad Received_PlayfieldLoaded = (PlayfieldLoad)data;
                        break;


                    case CmdId.Event_Playfield_Unloaded:
                        //Triggered when there are no players left in a playfield
                        PlayfieldLoad Received_PlayfieldUnLoaded = (PlayfieldLoad)data;
                        break;


                    case CmdId.Event_Faction_Changed:
                        //Triggered when an Entity (player too?) changes faction
                        FactionChangeInfo Received_FactionChange = (FactionChangeInfo)data;
                        break;


                    case CmdId.Event_Statistics:
                        //Triggered on various game events like: Player Death, Entity Power on/off, Remove/Add Core
                        StatisticsParam Received_EventStatistics = (StatisticsParam)data;
                        break;


                    case CmdId.Event_Player_DisconnectedWaiting:
                        //Triggered When a player is having trouble logging into the server
                        Id Received_PlayerDisconnectedWaiting = (Id)data;
                        break;


                    case CmdId.Event_TraderNPCItemSold:
                        //Triggered when a player buys an item from a trader
                        TraderNPCItemSoldInfo Received_TraderNPCItemSold = (TraderNPCItemSoldInfo)data;
                        break;


                    case CmdId.Event_Player_List:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_List, (ushort)CurrentSeqNr, null));
                        IdList Received_PlayerList = (IdList)data;
                        break;


                    case CmdId.Event_Player_Info:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_Info, (ushort)CurrentSeqNr, new Id( [playerID] ));
                        PlayerInfo Received_PlayerInfo = (PlayerInfo)data;
                        if (SeqNrStorage.Keys.Contains(seqNr))
                        {
                            Storage.StorableData RetrievedData = SeqNrStorage[seqNr];
                            if (RetrievedData.Requested == "PlayerInfo" && RetrievedData.function == "FBP" && Convert.ToString(Received_PlayerInfo.entityId) == RetrievedData.Match)
                            {
                                try
                                {
                                    SeqNrStorage.Remove(seqNr);
                                    string query = String.Format(SetupYamlData.FinishBlueprint.Querry, SetupYamlData.FinishBlueprint.Cost);
                                    if (Received_PlayerInfo.credits < SetupYamlData.FinishBlueprint.Cost)
                                    {
                                        API.ServerTell(Received_PlayerInfo.entityId, ModShortName, "Finish Blueprint Fail: Not Enough Credits for transaction.", true);
                                    }
                                    else if (Received_PlayerInfo.bpRemainingTime == 0)
                                    {
                                        API.ServerTell(Received_PlayerInfo.entityId, ModShortName, "Blueprint timer says time remaining = " + Received_PlayerInfo.bpRemainingTime + ". This command only works if the timer is counting down.", true);
                                    }
                                    else if (Received_PlayerInfo.credits >= (SetupYamlData.FinishBlueprint.Cost) && Received_PlayerInfo.bpRemainingTime > 0)
                                    {
                                        RetrievedData.function = "FBP";
                                        RetrievedData.Match = Convert.ToString(Received_PlayerInfo.entityId);
                                        RetrievedData.Requested = "DialogBox";
                                        RetrievedData.TriggerPlayer = Received_PlayerInfo;
                                        API.TextWindowOpen(Received_PlayerInfo.entityId, query, "Yes", "No", RetrievedData);
                                        string Playfield = modApi.Application.GetPlayerDataFor(Received_PlayerInfo.entityId).Value.PlayfieldName;
                                        
                                    }
                                    else
                                    {
                                        API.ServerTell(Received_PlayerInfo.entityId, ModShortName, "Unknown Error", true);
                                    }
                                }
                                catch
                                {
                                    CommonFunctions.Debug("FBP Fail: at PlayerInfo");
                                }
                            }
                            if (RetrievedData.Requested == "PlayerInfo" && RetrievedData.function == "FE" && Convert.ToString(Received_PlayerInfo.entityId) == RetrievedData.Match)
                            {
                                try
                                {
                                    SeqNrStorage.Remove(seqNr);
                                    if ( Received_PlayerInfo.credits < SetupYamlData.FactoryExtract.Cost)
                                    {
                                        API.ServerTell(Received_PlayerInfo.entityId, ModShortName, "Factory Extract Fail: Not Enough Credits for transaction.", true);
                                    }
                                    else
                                    {
                                        string querry = "Factory Extract costs " + SetupYamlData.FactoryExtract.Cost + " credits. Are you sure you want to do this?";
                                        if (Received_PlayerInfo.credits > (SetupYamlData.FinishBlueprint.Cost - 1) && Received_PlayerInfo.bpRemainingTime == 0)
                                        {
                                            RetrievedData.function = "FE";
                                            RetrievedData.Match = Convert.ToString(Received_PlayerInfo.entityId);
                                            RetrievedData.Requested = "DialogBox";
                                            RetrievedData.TriggerPlayer = Received_PlayerInfo;
                                            API.TextWindowOpen(Received_PlayerInfo.entityId, querry, "Yes", "No", RetrievedData);
                                        }
                                        else if (Received_PlayerInfo.bpRemainingTime > 0)
                                        {
                                            API.ServerTell(Received_PlayerInfo.entityId, ModShortName, "Factory is currently busy, Please wait till construction is complete.", true);
                                        }
                                    }
                                }
                                catch
                                {
                                    CommonFunctions.Debug("FE Fail: at PlayerInfo");
                                }
                            }
                        }
                        break;


                    case CmdId.Event_Player_Inventory:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_GetInventory, (ushort)CurrentSeqNr, new Id( [playerID] ));
                        Inventory Received_PlayerInventory = (Inventory)data;
                        break;


                    case CmdId.Event_Player_ItemExchange:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_ItemExchange, (ushort)CurrentSeqNr, new ItemExchangeInfo( [id], [title], [description], [buttontext], [ItemStack[]] ));
                        ItemExchangeInfo Received_ItemExchangeInfo = (ItemExchangeInfo)data;
                        
                        break;


                    case CmdId.Event_DialogButtonIndex:
                        //All of This is a Guess
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_ShowDialog_SinglePlayer, (ushort)CurrentSeqNr, new IdMsgPrio( [int nId], [string nMsg], [byte nPrio], [float nTime] )); //for Prio: 0=Red, 1=Yellow, 2=Blue
                        //Save/Pos = 0, Close/Cancel/Neg = 1
                        IdAndIntValue Received_DialogButtonIndex = (IdAndIntValue)data;
                        if (SeqNrStorage.Keys.Contains(seqNr))
                        {
                            Storage.StorableData RetrievedData = SeqNrStorage[seqNr];
                            if (RetrievedData.Requested == "DialogBox" && RetrievedData.function == "FBP" && Convert.ToString(Received_DialogButtonIndex.Id) == RetrievedData.Match)
                            {
                                try
                                {
                                    SeqNrStorage.Remove(seqNr);
                                    //CommonFunctions.Debug("FBP Dialog box close");
                                    if (Received_DialogButtonIndex.Value == 0)
                                    {
                                        API.Credits(RetrievedData.TriggerPlayer.entityId, -SetupYamlData.FinishBlueprint.Cost);
                                        PlayerInfoSet Playerinfo = new PlayerInfoSet()
                                        {
                                            entityId = RetrievedData.TriggerPlayer.entityId,
                                            bpRemainingTime = 2
                                        };
                                        API.PlayerInfoChange(Playerinfo);
                                        CommonFunctions.Log(RetrievedData.TriggerPlayer.entityId + " purchased FBP for " + SetupYamlData.FinishBlueprint.Cost);
                                    }
                                }
                                catch
                                {
                                    CommonFunctions.Debug("FBP Fail: at DialogBox");
                                }
                            }
                            if (RetrievedData.Requested == "DialogBox" && RetrievedData.function == "FE" && Convert.ToString(Received_DialogButtonIndex.Id) == RetrievedData.Match)
                            {
                                try
                                {
                                    SeqNrStorage.Remove(seqNr);
                                    if (Received_DialogButtonIndex.Value == 0)
                                    {
                                        SeqNrStorage.Remove(seqNr);
                                        API.Credits(RetrievedData.TriggerPlayer.entityId, -SetupYamlData.FactoryExtract.Cost);
                                        int count = 0;
                                        if (RetrievedData.TriggerPlayer.bpResourcesInFactory.Keys.Count() > 0)
                                        {
                                            List<ItemStack> ISlist = new List<ItemStack> { };
                                            foreach (int resource in RetrievedData.TriggerPlayer.bpResourcesInFactory.Keys)
                                            {
                                                count++;
                                                ItemStack IStack = new ItemStack
                                                {
                                                    id = resource,
                                                    count = Convert.ToInt32(RetrievedData.TriggerPlayer.bpResourcesInFactory[resource]),
                                                    slotIdx = Convert.ToByte(count)
                                                };
                                                ISlist.Add(IStack);
                                            }
                                            ItemStack[] itemStackArray = ISlist.ToArray();
                                            API.OpenItemExchange(RetrievedData.TriggerPlayer.entityId, "Factory Extract", "Cannot be used to insert", "Close", itemStackArray, RetrievedData);
                                        }
                                        else
                                        {
                                            //API.Chat("Player", RetrievedData.TriggerPlayer.entityId, "Factory Extract Fail: Nothing to Extract.");
                                            API.ServerTell(RetrievedData.TriggerPlayer.entityId, ModShortName, "Factory Extract Fail: Nothing to Extract.", true);
                                        }
                                        List<ItemStack> blankISlist = new List<ItemStack> { };
                                        ItemStack blankIS = new ItemStack()
                                        {
                                            id = 0,
                                            count = 0
                                        };
                                        blankISlist.Add(blankIS);
                                        //ItemStack[] itStack = blankISlist.ToArray();
                                        BlueprintResources bpResources = new BlueprintResources()
                                        {
                                            PlayerId = RetrievedData.TriggerPlayer.entityId,
                                            ReplaceExisting = true,
                                            ItemStacks = blankISlist
                                        };
                                        API.Factory(bpResources);
                                        CommonFunctions.Log(RetrievedData.TriggerPlayer.entityId + " purchased FE for " + SetupYamlData.FactoryExtract.Cost);
                                    }
                                }
                                catch
                                {
                                    CommonFunctions.Debug("FE Fail: at DialogBox");
                                }
                            }
                        }

                        break;


                    case CmdId.Event_Player_Credits:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_Credits, (ushort)CurrentSeqNr, new Id( [PlayerID] ));
                        IdCredits Received_PlayerCredits = (IdCredits)data;
                        break;


                    case CmdId.Event_Player_GetAndRemoveInventory:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_GetAndRemoveInventory, (ushort)CurrentSeqNr, new Id( [playerID] ));
                        Inventory Received_PlayerGetRemoveInventory = (Inventory)data;
                        break;


                    case CmdId.Event_Playfield_List:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Playfield_List, (ushort)CurrentSeqNr, null));
                        PlayfieldList Received_PlayfieldList = (PlayfieldList)data;
                        break;


                    case CmdId.Event_Playfield_Stats:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Playfield_Stats, (ushort)CurrentSeqNr, new PString( [Playfield Name] ));
                        PlayfieldStats Received_PlayfieldStats = (PlayfieldStats)data;
                        break;


                    case CmdId.Event_Playfield_Entity_List:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Playfield_Entity_List, (ushort)CurrentSeqNr, new PString( [Playfield Name] ));
                        PlayfieldEntityList Received_PlayfieldEntityList = (PlayfieldEntityList)data;
                        break;


                    case CmdId.Event_Dedi_Stats:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Dedi_Stats, (ushort)CurrentSeqNr, null));
                        DediStats Received_DediStats = (DediStats)data;
                        break;


                    case CmdId.Event_GlobalStructure_List:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_GlobalStructure_List, (ushort)CurrentSeqNr, null));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_GlobalStructure_Update, (ushort)CurrentSeqNr, new PString( [Playfield Name] ));
                        GlobalStructureList Received_GlobalStructureList = (GlobalStructureList)data;
                        break;


                    case CmdId.Event_Entity_PosAndRot:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_PosAndRot, (ushort)CurrentSeqNr, new Id( [EntityID] ));
                        IdPositionRotation Received_EntityPosRot = (IdPositionRotation)data;
                        break;


                    case CmdId.Event_Get_Factions:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Get_Factions, (ushort)CurrentSeqNr, new Id( [int] )); //Requests all factions from a certain Id onwards. If you want all factions use Id 1.
                        FactionInfoList Received_FactionInfoList = (FactionInfoList)data;
                        break;


                    case CmdId.Event_NewEntityId:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_NewEntityId, (ushort)CurrentSeqNr, null));
                        Id Request_NewEntityId = (Id)data;
                        break;


                    case CmdId.Event_Structure_BlockStatistics:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Structure_BlockStatistics, (ushort)CurrentSeqNr, new Id( [EntityID] ));
                        IdStructureBlockInfo Received_StructureBlockStatistics = (IdStructureBlockInfo)data;
                        break;


                    case CmdId.Event_AlliancesAll:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_AlliancesAll, (ushort)CurrentSeqNr, null));
                        AlliancesTable Received_AlliancesAll = (AlliancesTable)data;
                        break;


                    case CmdId.Event_AlliancesFaction:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_AlliancesFaction, (ushort)CurrentSeqNr, new AlliancesFaction( [int nFaction1Id], [int nFaction2Id], [bool nIsAllied] ));
                        AlliancesFaction Received_AlliancesFaction = (AlliancesFaction)data;
                        break;


                    case CmdId.Event_BannedPlayers:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_GetBannedPlayers, (ushort)CurrentSeqNr, null ));
                        BannedPlayerData Received_BannedPlayers = (BannedPlayerData)data;
                        break;


                    case CmdId.Event_GameEvent:
                        //Triggered by PDA Events
                        GameEventData Received_GameEvent = (GameEventData)data;
                        break;


                    case CmdId.Event_Ok:
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_SetInventory, (ushort)CurrentSeqNr, new Inventory(){ [changes to be made] });
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_AddItem, (ushort)CurrentSeqNr, new IdItemStack(){ [changes to be made] });
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_SetCredits, (ushort)CurrentSeqNr, new IdCredits( [PlayerID], [Double] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Player_AddCredits, (ushort)CurrentSeqNr, new IdCredits( [PlayerID], [+/- Double] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Blueprint_Finish, (ushort)CurrentSeqNr, new Id( [PlayerID] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Blueprint_Resources, (ushort)CurrentSeqNr, new BlueprintResources( [PlayerID], [List<ItemStack>], [bool ReplaceExisting?] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_Teleport, (ushort)CurrentSeqNr, new IdPositionRotation( [EntityId OR PlayerID], [Pvector3 Position], [Pvector3 Rotation] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_ChangePlayfield , (ushort)CurrentSeqNr, new IdPlayfieldPositionRotation( [EntityId OR PlayerID], [Playfield],  [Pvector3 Position], [Pvector3 Rotation] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_Destroy, (ushort)CurrentSeqNr, new Id( [EntityID] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_Destroy2, (ushort)CurrentSeqNr, new IdPlayfield( [EntityID], [Playfield] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_SetName, (ushort)CurrentSeqNr, new Id( [EntityID] )); Wait, what? This one doesn't make sense. This is what the Wiki says though.
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Entity_Spawn, (ushort)CurrentSeqNr, new EntitySpawnInfo()); Doesn't make sense to me.
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_Structure_Touch, (ushort)CurrentSeqNr, new Id( [EntityID] ));
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_InGameMessage_SinglePlayer, (ushort)CurrentSeqNr, new IdMsgPrio( [int nId], [string nMsg], [byte nPrio], [float nTime] )); //for Prio: 0=Red, 1=Yellow, 2=Blue
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_InGameMessage_Faction, (ushort)CurrentSeqNr, new IdMsgPrio( [int nId], [string nMsg], [byte nPrio], [float nTime] )); //for Prio: 0=Red, 1=Yellow, 2=Blue
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_InGameMessage_AllPlayers, (ushort)CurrentSeqNr, new IdMsgPrio( [int nId], [string nMsg], [byte nPrio], [float nTime] )); //for Prio: 0=Red, 1=Yellow, 2=Blue
                        //Triggered by API mod request GameAPI.Game_Request(CmdId.Request_ConsoleCommand, (ushort)CurrentSeqNr, new PString( [Telnet Command] ));

                        //uh? Not Listed in Wiki... Received_ = ()data;
                        break;


                    case CmdId.Event_Error:
                        //Triggered when there is an error coming from the API
                        ErrorInfo Received_ErrorInfo = (ErrorInfo)data;
                        if (SeqNrStorage.Keys.Contains(seqNr))
                        {
                            CommonFunctions.LogFile("Debug.txt", "API Error:");
                            CommonFunctions.LogFile("Debug.txt", "ErrorType: " + Received_ErrorInfo.errorType);
                            CommonFunctions.LogFile("Debug.txt", "");
                        }
                        break;


                    case CmdId.Event_PdaStateChange:
                        //Triggered by PDA: chapter activated/deactivated/completed
                        PdaStateInfo Received_PdaStateChange = (PdaStateInfo)data;
                        break;


                    case CmdId.Event_ConsoleCommand:
                        //Triggered when a player uses a Console Command in-game
                        ConsoleCommandInfo Received_ConsoleCommandInfo = (ConsoleCommandInfo)data;
                        break;


                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                CommonFunctions.LogFile("ERROR.txt", "Message: " + ex.Message);
                CommonFunctions.LogFile("ERROR.txt", "Data: " + ex.Data);
                CommonFunctions.LogFile("ERROR.txt", "HelpLink: " + ex.HelpLink);
                CommonFunctions.LogFile("ERROR.txt", "InnerException: " + ex.InnerException);
                CommonFunctions.LogFile("ERROR.txt", "Source: " + ex.Source);
                CommonFunctions.LogFile("ERROR.txt", "StackTrace: " + ex.StackTrace);
                CommonFunctions.LogFile("ERROR.txt", "TargetSite: " + ex.TargetSite);
                CommonFunctions.LogFile("ERROR.txt", "");
            }
        }
        public void Game_Update()
        {
            //Triggered whenever Empyrion experiences "Downtime", roughly 75-100 times per second
        }
        public void Game_Exit()
        {
            //Triggered when the server is Shutting down. Does NOT pause the shutdown.
        }

        public void Init(IModApi modAPI)
        {
            modApi = modAPI;
            //modApi.Application.OnPlayfieldLoaded += Application_OnPlayfieldLoaded;
        }

        private void Application_OnPlayfieldLoaded(IPlayfield playfield)
        {
            Dictionary<int, IPlayer> players = playfield.Players;
            foreach (int player in players.Keys)
            {
                //players[player].
            }
        }

        public void Shutdown()
        {
            
        }
    }
}