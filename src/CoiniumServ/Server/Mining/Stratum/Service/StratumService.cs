﻿#region License
// 
//     CoiniumServ - Crypto Currency Mining Pool Server Software
//     Copyright (C) 2013 - 2014, CoiniumServ Project - http://www.coinium.org
//     http://www.coiniumserv.com - https://github.com/CoiniumServ/CoiniumServ
// 
//     This software is dual-licensed: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//    
//     For the terms of this license, see licenses/gpl_v3.txt.
// 
//     Alternatively, you can license this software under a commercial
//     license or white-label it as set out in licenses/commercial.txt.
// 
#endregion

using AustinHarris.JsonRpc;
using CoiniumServ.Jobs;
using CoiniumServ.Pools;
using CoiniumServ.Server.Mining.Service;
using CoiniumServ.Server.Mining.Stratum.Responses;
using CoiniumServ.Shares;

namespace CoiniumServ.Server.Mining.Stratum.Service
{
    /// <summary>
    /// Stratum protocol implementation.
    /// </summary>
    public class StratumService : JsonRpcService, IRpcService
    {
        private readonly IShareManager _shareManager;

        public StratumService(IPoolConfig poolConfig, IShareManager shareManager):
            base(poolConfig.Coin.Name)
        {
            _shareManager = shareManager;
        }

        /// <summary>
        /// Subscribes a Miner to allow it to recieve work to begin hashing and submitting shares.
        /// </summary>
        /// <param name="signature">Miner Connection</param>
        [JsonRpcMethod("mining.subscribe")]
        public SubscribeResponse SubscribeMiner(string signature)
        {
            var context = (SocketServiceContext) JsonRpcContext.Current().Value;
            var miner = (IStratumMiner)(context.Miner);            

            var response = new SubscribeResponse
            {
                ExtraNonce1 = miner.ExtraNonce.ToString("x8"), // Hex-encoded, per-connection unique string which will be used for coinbase serialization later. (http://mining.bitcoin.cz/stratum-mining)
                ExtraNonce2Size = ExtraNonce.ExpectedExtraNonce2Size // Represents expected length of extranonce2 which will be generated by the miner. (http://mining.bitcoin.cz/stratum-mining)
            };

            miner.Subscribe(signature);

            return response;
        }

        /// <summary>
        /// Authorise a miner based on their username and password
        /// </summary>
        /// <param name="user">Worker Username (e.g. "coinium.1").</param>
        /// <param name="password">Worker Password (e.g. "x").</param>
        [JsonRpcMethod("mining.authorize")]
        public bool AuthorizeMiner(string user, string password)
        {
            var context = (SocketServiceContext)JsonRpcContext.Current().Value;
            var miner = (IStratumMiner)(context.Miner);

            return miner.Authenticate(user, password);
        }

        /// <summary>
        /// Allows a miner to submit the work they have done 
        /// </summary>
        /// <param name="user">Worker Username.</param>
        /// <param name="jobId">Job ID(Should be unique per Job to ensure that share diff is recorded properly) </param>
        /// <param name="extraNonce2">Hex-encoded big-endian extranonce2, length depends on extranonce2_size from mining.notify</param>
        /// <param name="nTime"> UNIX timestamp (32bit integer, big-endian, hex-encoded), must be >= ntime provided by mining,notify and <= current time'</param>
        /// <param name="nonce"> 32bit integer hex-encoded, big-endian </param>
        [JsonRpcMethod("mining.submit")]
        public bool SubmitWork(string user, string jobId, string extraNonce2, string nTime, string nonce)
        {
            var context = (SocketServiceContext)JsonRpcContext.Current().Value;
            var miner = (IStratumMiner)(context.Miner);

            return _shareManager.ProcessShare(miner, jobId, extraNonce2, nTime, nonce).IsValid;
        }
    }
}
