﻿using Newtonsoft.Json;
using NextGenSoftware.Holochain.HoloNET.Client.Core;
using NextGenSoftware.OASIS.API.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.Core
{
    public abstract class HoloOASISBase : IOASISNET, IOASISStorage
    {
        private const string OURWORLD_ZOME = "our_world_core";
        private const string LOAD_PROFILE_FUNC = "load_profile";
        private const string SAVE_PROFILE_FUNC = "save_profile";

        private int _currentId = 0;
        private Dictionary<string, Profile> _savingProfiles = new Dictionary<string, Profile>();
        //private string _holochainURI;
        private string _hcinstance;
        private TaskCompletionSource<IProfile> _taskCompletionSourceIProfile = new TaskCompletionSource<IProfile>();
        // private TaskCompletionSource<IProfile> _taskCompletionSourceGetInstance = new TaskCompletionSource<IProfile>();

        public delegate void Initialized(object sender, EventArgs e);
        public event Initialized OnInitialized;

        public delegate void ProfileSaved(object sender, ProfileSavedEventArgs e);
        public event ProfileSaved OnPlayerProfileSaved;

        public delegate void ProfileLoaded(object sender, ProfileLoadedEventArgs e);
        public event ProfileLoaded OnPlayerProfileLoaded;

        public delegate void HoloOASISError(object sender, ErrorEventArgs e);
        public event HoloOASISError OnHoloOASISError;

        public HoloNETClientBase HoloNETClient { get; private set; }


        public HoloOASISBase(HoloNETClientBase holoNETClient)
        {
            this.HoloNETClient = holoNETClient;
          //  _holochainURI = holochainURI;
            Initialize();
        }

        public async Task Initialize()
        {
            //HoloNETClient = new HoloNETClientBase(_holochainURI);

            HoloNETClient.OnConnected += HoloOASIS_OnConnected;
            HoloNETClient.OnDisconnected += HoloOASIS_OnDisconnected;
            HoloNETClient.OnError += HoloNETClient_OnError;
            HoloNETClient.OnDataReceived += HoloOASIS_OnDataReceived;
            HoloNETClient.OnGetInstancesCallBack += HoloOASIS_OnGetInstancesCallBack;
            HoloNETClient.OnSignalsCallBack += HoloOASIS_OnSignalsCallBack;
            HoloNETClient.OnZomeFunctionCallBack += HoloOASIS_OnZomeFunctionCallBack;

            await HoloNETClient.Connect();
           // await HoloNETClient.GetHolochainInstancesAsync();
            //GetInstancesCallBackEventArgs args = await HoloNETClient.GetHolochainInstancesAsync();
            //_hcinstance = args.Instances[0];
            //_hcinstance = await GetHolochainInstancesAsync().Result.Instances[0];
        }

        private void HoloNETClient_OnError(object sender, Holochain.HoloNET.Client.Core.ErrorEventArgs e)
        {
            OnHoloOASISError?.Invoke(this, new ErrorEventArgs { EndPoint = HoloNETClient.EndPoint, Reason = "Error occured in HoloNET. See ErrorDetial for reason.", HoloNETErrorDetails = e });
        }

        private void HoloOASIS_OnZomeFunctionCallBack(object sender, ZomeFunctionCallBackEventArgs e)
        {
            if (!e.IsCallSuccessful)
                OnHoloOASISError(this, new ErrorEventArgs() { EndPoint = HoloNETClient.EndPoint, Reason = string.Concat("Zome function ", e.ZomeFunction, " on zome ", e.Zome, " returned an error. Error Details: ", e.ZomeReturnData) });
            else
            {
                switch (e.ZomeFunction)
                {
                    case LOAD_PROFILE_FUNC:
                        OnPlayerProfileLoaded?.Invoke(this, new ProfileLoadedEventArgs { Profile = JsonConvert.DeserializeObject<Profile>(string.Concat("{", e.ZomeReturnData, "}")) });

                        //TODO: Want to use these eventually so the async methods can return the results without having to use events/callbacks!
                        //_taskCompletionSourceIProfile.SetResult(JsonConvert.DeserializeObject<IProfile>(e.ZomeReturnData));
                        break;

                    case SAVE_PROFILE_FUNC:
                        // TODO: Eventually want to return the Profile object from HC with the HCAddressHash set when I work out how! ;-)
                        // The dictonary below can then be removed.

                        //if (string.IsNullOrEmpty(_savingProfiles[e.Id].HcAddressHash))
                        //{
                        //    //TODO: Forced to re-save the object with the address (wouldn't that create a new hash entry?!)
                             _savingProfiles[e.Id].HcAddressHash = e.ZomeReturnData;
                        //    SaveProfileAsync(_savingProfiles[e.Id]);
                        //}
                        //else
                        //{
                            OnPlayerProfileSaved?.Invoke(this, new ProfileSavedEventArgs { Profile = _savingProfiles[e.Id] });
                            _savingProfiles.Remove(e.Id);
                        //}

                        //TODO: Want to use these eventually so the async methods can return the results without having to use events/callbacks!
                        // _taskCompletionSourceIProfile.SetResult(JsonConvert.DeserializeObject<IProfile>(e.ZomeReturnData));
                        break;
                }
            }
        }

        private void HoloOASIS_OnSignalsCallBack(object sender, SignalsCallBackEventArgs e)
        {
            
        }

        private void HoloOASIS_OnGetInstancesCallBack(object sender, GetInstancesCallBackEventArgs e)
        {
            _hcinstance = e.Instances[0];
            OnInitialized?.Invoke(this, new EventArgs());
        }

        private void HoloOASIS_OnDisconnected(object sender, DisconnectedEventArgs e)
        {
            
        }

        private void HoloOASIS_OnDataReceived(object sender, DataReceivedEventArgs e)
        {
            
        }

        private void HoloOASIS_OnConnected(object sender, ConnectedEventArgs e)
        {
            HoloNETClient.GetHolochainInstancesAsync();
        }

        #region IOASISStorage Implementation

        public async Task<API.Core.IProfile> LoadProfileAsync(string profileEntryHash)
        {
            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                await HoloNETClient.CallZomeFunctionAsync(_hcinstance, OURWORLD_ZOME, LOAD_PROFILE_FUNC, new { address = profileEntryHash });
                //return await _taskCompletionSourceIProfile.Task;
            }

            return null;
        }

        public async Task<API.Core.IProfile> LoadProfileAsync(Guid id)
        {
            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                //TODO: Implement in HC/Rust
                await HoloNETClient.CallZomeFunctionAsync(_hcinstance, OURWORLD_ZOME, LOAD_PROFILE_FUNC, new { id });
                //return await _taskCompletionSourceIProfile.Task;
            }

            return null;
        }

        public async Task<API.Core.IProfile> LoadProfileAsync(string username, string password)
        {
            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                //TODO: Implement in HC/Rust
                await HoloNETClient.CallZomeFunctionAsync(_hcinstance, OURWORLD_ZOME, LOAD_PROFILE_FUNC, new { username, password });
                //return await _taskCompletionSourceIProfile.Task;
            }

            return null;
        }

        public async Task<bool> SaveProfileAsync(API.Core.IProfile profile)
        {
            if (HoloNETClient.State == System.Net.WebSockets.WebSocketState.Open && !string.IsNullOrEmpty(_hcinstance))
            {
                if (profile.Id == Guid.Empty)
                    profile.Id = Guid.NewGuid();

                if (((Profile)profile).HcAddressHash == null)
                    ((Profile)profile).HcAddressHash = string.Empty;

                _currentId++;
                _savingProfiles[_currentId.ToString()] = (Profile)profile;
                await HoloNETClient.CallZomeFunctionAsync(_currentId.ToString(), _hcinstance, OURWORLD_ZOME, SAVE_PROFILE_FUNC, new { entry = profile });
                //  return await _taskCompletionSourceIProfile.Task;

                return true; //TODO: Need to get the above line working so it waits for the event to come back from HC so we know it succeeded for sure!
            }

            return false;
        }

        public Task<bool> AddKarmaToProfileAsync(API.Core.IProfile profile, int karma)
        {
            throw new NotImplementedException();
        }

        public Task<bool> RemoveKarmaFromProfileAsync(API.Core.IProfile profile, int karma)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region IOASISNET Implementation

        public List<IHolon> GetHolonsNearMe(HolonType type)
        {
            throw new NotImplementedException();
        }

        public List<IPlayer> GetPlayersNearMe()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
