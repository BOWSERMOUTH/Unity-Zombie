
using System;
using System.Collections.Generic;
using System.Linq;
using ExitGames.Client.Photon;
using POpusCodec;
using POpusCodec.Enums;
using UnityEngine;
using Voice = ExitGames.Client.Photon.Voice;
using LoadBalancing = ExitGames.Client.Photon.LoadBalancing;

using VoiceIdPair = System.Collections.Generic.KeyValuePair<int, byte>;

/// <summary>
/// The singleton encapsulating Photon Voice client.
/// Accessed via class static methods.
/// </summary>
/// <remarks>
/// Extends MonoBehaviour and attached to an object in scene to call Photon Voice Client Service() in Update().
/// </remarks>
public class PhotonVoiceNetwork : MonoBehaviour
{
    private static PhotonVoiceNetwork _instance;
    private static GameObject _singleton;
    private static object instanceLock = new object();
    private static bool destroyed = false;

    internal static PhotonVoiceNetwork instance
    {
        get
        {
            lock (instanceLock)
            {
                if (destroyed)
                {
                    return null;
                }
                if (_instance == null)
                {
                    _singleton = new GameObject();
                    _instance = _singleton.AddComponent<PhotonVoiceNetwork>();
                    _singleton.name = "PhotonVoiceNetworkSingleton";

                    DontDestroyOnLoad(_singleton);
                }
                return _instance;
            }
        }
    }

    void OnDestroy()
    {
        if (this != _instance)
        {
            return;
        }

        destroyed = true;
    }

    internal UnityVoiceClient client;
    PhotonVoiceNetwork()
    {
        client = new UnityVoiceClient(this);
        //client.loadBalancingPeer.DebugOut = DebugLevel.ALL;
        //PhotonNetwork.logLevel = PhotonLogLevel.Full;

        if (Microphone.devices.Length < 1)
        {
            Debug.LogError("PUNVoice: No microphone device found");
        }

        // debug only
        // client.UseLossCompensation = false;
        
		// client.loadBalancingPeer.QuickResendAttempts = 3;
        // client.loadBalancingPeer.SentCountAllowance = 7;
        // PhotonNetwork.networkingPeer.QuickResendAttempts = 3;
        // PhotonNetwork.networkingPeer.SentCountAllowance = 7;
        
        //client.loadBalancingPeer.DebugOut = PhotonVoiceSettings.Instance.DebugLevel; // null ref while PhotonVoiceSettings is object's script
    }

    /// <summary>
    /// Connects Voice client to a Master Server of region specified in settings, using the Name Server to find the IP.
    /// </summary>
    /// <returns>If the operation could be sent. If false, no operation was sent.</returns>
    public static bool Connect()
    {
        instance.client.AppId = PhotonNetwork.PhotonServerSettings.VoiceAppID;
        instance.client.AppVersion = "1.0"; // hardcoded, customization does not make sense?

        if (PhotonNetwork.PhotonServerSettings.HostType == ServerSettings.HostingOption.SelfHosted)
        {
            Debug.Log("PUNVoice: connecting to master " + PhotonNetwork.networkingPeer.MasterServerAddress);
            return instance.client.Connect(PhotonNetwork.networkingPeer.MasterServerAddress, null, null, null, null);
        }
        else {
            Debug.Log("PUNVoice: connecting to region " + PhotonNetwork.networkingPeer.CloudRegion.ToString());
            return instance.client.ConnectToRegionMaster(PhotonNetwork.networkingPeer.CloudRegion.ToString());
        }
    }

    /// <summary>Disconnects voice client from any server.</summary>
    public static void Disconnect()
    {
        instance.client.Disconnect();
    }

    /// <summary>Returns underlying Photon Voice client.</summary>
    public static Voice.Client Client { get { return instance.client; } }

    /// <summary>Returns Photon Voice client state.</summary>
    public static LoadBalancing.ClientState ClientState { get { return instance.client.State; } }

    /// <summary>Room name currently joined by Photon Voice client or empty string.</summary>
    public static string CurrentRoomName { get { return instance.client.CurrentRoom == null ? "" : instance.client.CurrentRoom.Name; } }

    private static string microphoneDevice = null;

    /// <summary>Global microphone device setting. Applied to all PhotonVoiceRecorders owned by client during initialization in Start().
    /// If changed while joined PUN room, rejoin (recorders recreation) required.
    /// </summary>
    /// <remarks>
    /// Use PhotonVoiceRecorder.MicrophoneDevice to set device per recorder.
    /// </remarks>    
    public static string MicrophoneDevice
    {
        get { return microphoneDevice; }
        set
        {
            if (value != null && !Microphone.devices.Contains(value))
            {
                Debug.LogError("PUNVoice: " + value + " is not a valid microphone device");
                return;
            }

            microphoneDevice = value;
            if (PhotonVoiceSettings.Instance.DebugInfo)
            {
                Debug.Log("PUNVoice: Setting global microphone device to " + microphoneDevice);
            }
            foreach (var r in FindObjectsOfType<PhotonVoiceRecorder>())
            {
                if (r.photonView.isMine)
                {
                    if (r.MicrophoneDevice == null)
                    {
                        // update mic device
                        r.MicrophoneDevice = null;
                    }
                }
            }
        }
    }

    protected void OnEnable()
    {
        if (this != _instance)
        {
            return;
        }

        Application.RequestUserAuthorization(UserAuthorization.Microphone);        
    }

    protected void OnApplicationQuit()
    {
        if (this != _instance)
        {
            return;
        }

        client.Disconnect();
    }

    protected void Update()
    {
        if (this != _instance)
        {
            return;
        }

        // to be able change percentage in runtime
        this.client.DebugLostPercent = PhotonVoiceSettings.Instance.DebugLostPercent;

        client.Service();
    }

    /// <summary>
    /// Creates new local voice (outgoing audio stream).
    /// </summary>
    /// <param name="audioStream">Object providing audio data for the outgoing stream.</param>
    /// <param name="voiceInfo">Outgoing audio stream parameters (should be set according to Opus encoder restrictions).</param>
    /// <returns>Outgoing stream handler.</returns>
    /// <remarks>
    /// audioStream.SamplingRate and voiceInfo.SamplingRate may do not match. Automatic resampling will occur in this case.
    /// </remarks>
    public static Voice.LocalVoice CreateLocalVoice(Voice.IAudioStream audioClip, Voice.VoiceInfo voiceInfo)
    {
        return instance.client.CreateLocalVoice(audioClip, voiceInfo);
    }

    /// <summary>
    /// Removes local voice (outgoing audio stream).
    /// <param name="voice">Handler of outgoing stream to be removed.</param>
    /// </summary>
    public static void RemoveLocalVoice(Voice.LocalVoice voice)
    {
        // can be called from OnDestroy, check if still exists
        if (!destroyed)
        {
            instance.client.RemoveLocalVoice(voice);
        }
    }

    // PUN room joined
    void OnJoinedRoom()
    {
        if (this != _instance)
        {
            return;
        }

        // voice room check
        switch (this.client.State)
        {
            case ExitGames.Client.Photon.LoadBalancing.ClientState.Joined:
                if (PhotonVoiceSettings.Instance.AutoConnect)
                {
                    // trigger rejoin to the (possible other) room                    
                    this.client.OpLeaveRoom();
                }
                break;
            default:
                if (PhotonVoiceSettings.Instance.AutoConnect)
                {
                    // trigger reconnect to the (possible other) room                    
                    this.client.Reconnect();
                }
                break;
        }
    }

    void OnLeftRoom()
    {
        if (this != _instance)
        {
            return;
        }

        if (PhotonVoiceSettings.Instance.AutoDisconnect)
        {
            client.Disconnect();
        }
    }

    void OnDisconnectedFromPhoton()
    {
        if (this != _instance)
        {
            return;
        }

        if (PhotonVoiceSettings.Instance.AutoDisconnect)
        {
            client.Disconnect();
        }
    }

    internal static void LinkSpeakerToRemoteVoice(PhotonVoiceSpeaker speaker)
    {
        instance.client.LinkSpeakerToRemoteVoice(speaker);
    }

    internal static void UnlinkSpeakerFromRemoteVoice(PhotonVoiceSpeaker speaker)
    {
        // can be called from OnDestroy, check if still exists
        if (!destroyed)
        {
            instance.client.UnlinkSpeakerFromRemoteVoice(speaker);
        }
    }
}

internal class UnityVoiceClient : Voice.Client
{
    // or could be dict of dicts like VoiceClient.remoteVoices counterpart
    private Dictionary<VoiceIdPair, PhotonVoiceSpeaker> voiceSpeakers = new Dictionary<VoiceIdPair, PhotonVoiceSpeaker>();

    internal UnityVoiceClient(PhotonVoiceNetwork network)
    {
        this.OnRemoteVoiceInfoAction = OnRemoteVoiceInfo;
        this.OnRemoteVoiceRemoveAction = OnRemoteVoiceRemove;
        this.OnAudioFrameAction = OnAudioFrame;

        this.OnStateChangeAction = OnStateChange;
        this.OnOpResponseAction = OnOpResponse;
        this.loadBalancingPeer.DebugOut = DebugLevel.INFO;
    }

    private bool reconnect = false;

    /// <summary>
    /// Trigger reconnect to the (possible other) room.
    /// </summary>
    /// /// <remarks>
    /// When connecting manually, call this to switch Voice client to new voice room if PUN room changed.
    /// </remarks>
    public void Reconnect()
    {
        if (this.State == LoadBalancing.ClientState.Disconnected)
        {
            PhotonVoiceNetwork.Connect();
        }
        else
        {
            this.reconnect = true;
            this.Disconnect();
        }
    }

    public override void DebugReturn(DebugLevel level, string message)
    {
        message = "PUNVoice: " + message;
        if (level == DebugLevel.ERROR)
        {
            Debug.LogError(message);
        }
        else if (level == DebugLevel.WARNING)
        {
            Debug.LogWarning(message);
        }
        else if (level == DebugLevel.INFO && PhotonNetwork.logLevel >= PhotonLogLevel.Informational)
        {
            Debug.Log(message);
        }
        else if (level == DebugLevel.ALL && PhotonNetwork.logLevel == PhotonLogLevel.Full)
        {
            Debug.Log(message);
        }
    }

    public void OnOpResponse(OperationResponse resp)
    {
        if (resp.ReturnCode == 0)
        {
            switch (resp.OperationCode)
            {
                case ExitGames.Client.Photon.LoadBalancing.OperationCode.JoinGame:
                    PhotonVoiceRecorder[] recs = GameObject.FindObjectsOfType<PhotonVoiceRecorder>();
                    foreach (var r in recs)
                    {
                        r.SendMessage("OnJoinedVoiceRoom");
                    }
                    break;
            }
        }
    }

    private void linkVoice(int playerId, byte voiceId, Voice.VoiceInfo voiceInfo, PhotonVoiceSpeaker speaker)
    {
        speaker.OnVoiceLinked(voiceInfo.SamplingRate, voiceInfo.Channels, voiceInfo.EncoderDelay, PhotonVoiceSettings.Instance.PlayDelayMs);
        var key = new VoiceIdPair(playerId, voiceId);
        PhotonVoiceSpeaker oldSpeaker;
        if (this.voiceSpeakers.TryGetValue(key, out oldSpeaker))
        {
            if (oldSpeaker == speaker)
            {
                return;
            }
            else 
            {
                Debug.Log("PUNVoice: Player " + playerId + " voice #" + voiceId + " speaker replaced");                
            }
        }
        else 
        {
            Debug.Log("PUNVoice: Player " + playerId + " voice #" + voiceId + " speaker created");
        }
        this.voiceSpeakers[key] = speaker;
    }

    public void OnRemoteVoiceInfo(int playerId, byte voiceId, Voice.VoiceInfo voiceInfo)
    {
        var key = new VoiceIdPair(playerId, voiceId);
        if (this.voiceSpeakers.ContainsKey(key))
        {
            Debug.LogWarning("PUNVoice: Info duplicate for voice #" + voiceId + " of player " + playerId);
        }

        PhotonVoiceSpeaker speaker = null;
        PhotonVoiceSpeaker[] speakers = GameObject.FindObjectsOfType<PhotonVoiceSpeaker>();
        foreach (var s in speakers)
        {
            if (s.photonView.viewID == (int)voiceInfo.UserData)
            {
                speaker = s;
                break;
            }
        }

        if (speaker == null)
        {
            //            Debug.LogWarning("PUNVoice: No PhotonVoiceMic found for info of voice #" + voiceId + " of player " + playerId);
        }
        else
        {
            this.linkVoice(playerId, voiceId, voiceInfo, speaker);
        }

    }

    // Try to link new PUN object with Speaker attached to remote voice.
    // If remote voice is not available yet, will link as soon as remote voice info arrives in OnRemoteVoiceInfo.
    public void LinkSpeakerToRemoteVoice(PhotonVoiceSpeaker speaker)
    {
        foreach (var i in this.RemoteVoiceInfos)
        {
            if (speaker.photonView.viewID == (int)i.Info.UserData)
            {
                this.linkVoice(i.PlayerId, i.VoiceId, i.Info, speaker);
            }
        }
    }

    public void OnRemoteVoiceRemove(int playerId, byte voiceId)
    {
        var key = new VoiceIdPair(playerId, voiceId);
        if (!this.unlinkSpeaker(key))
        {
            Debug.LogWarning("PUNVoice: Voice #" + voiceId + " of player " + playerId + " not found.");
        }
        else
        {
            Debug.Log("PUNVoice: Player " + playerId + " voice #" + voiceId + " speaker unlinked");
        }
    }

    private bool unlinkSpeaker(VoiceIdPair key)
    {
        PhotonVoiceSpeaker s;
        if (this.voiceSpeakers.TryGetValue(key, out s))
        {
            s.OnVoiceUnlinked();
        }
        return this.voiceSpeakers.Remove(key);
    }

    // Unlinks destroyed PUN object from remote voice (if not done yet in OnRemoteVoiceRemove)
    public void UnlinkSpeakerFromRemoteVoice(PhotonVoiceSpeaker speaker)
    {
        var toRemove = new List<VoiceIdPair>();
        foreach (var s in this.voiceSpeakers)
        {
            if (s.Value == speaker)
            {
                toRemove.Add(s.Key);
                Debug.Log("PUNVoice: Player " + s.Key.Key + " voice #" + s.Key.Value + " speaker unlinked");
            }
        }
        foreach (var k in toRemove)
        {
            this.unlinkSpeaker(k);
        }
    }

    public void OnAudioFrame(int playerId, byte voiceId, float[] frame)
    {
        PhotonVoiceSpeaker voiceSpeaker = null;
        if (this.voiceSpeakers.TryGetValue(new VoiceIdPair(playerId, voiceId), out voiceSpeaker))
        {
            voiceSpeaker.OnAudioFrame(frame);
        }
        else
        {
            Debug.LogWarning("PUNVoice: Audio Frame event for not existing speaker for voice #" + voiceId + " of player " + playerId);
        }
    }

    public void OnStateChange(LoadBalancing.ClientState state)
    {
        if (PhotonNetwork.logLevel >= PhotonLogLevel.Informational)
        {
            Debug.Log("PUNVoice: Voice Client state: " + state);
        }
        switch (state)
        {
            case LoadBalancing.ClientState.JoinedLobby:
                if (PhotonNetwork.inRoom)
                {
                    this.OpJoinOrCreateRoom(PhotonNetwork.room.name + "_voice_", new LoadBalancing.RoomOptions() { IsVisible = false }, null);
                }
                else
                {
                    Debug.LogWarning("PUNVoice: PUN client is not in room yet. Disconnecting voice client.");
                    this.Disconnect();
                }
                break;
            case LoadBalancing.ClientState.Disconnected: // triggered by PUN room join or network failure                
                if (this.reconnect)
                {
                    PhotonVoiceNetwork.Connect();
                }
                this.reconnect = false;
                break;
        }
    }
}
