using System;
using System.Collections.Generic;
using Njoror.Configuration;
using UnityEngine;

namespace Njoror.Logic
{
    public sealed class SailingStateNetwork : MonoBehaviour
    {
        private const string SailingStateRpc = "Njoror_SailingState";
        private const string AuthoritativeWindRpc = "Njoror_AuthoritativeWind";
        private const float ReportIntervalSeconds = 0.5f;
        private const float StateExpirySeconds = 2f;
        private const float MinimumWindExpirySeconds = 5f;
        private const float MinimumHeadingMagnitude = 0.8f;
        private const float ShipDetectionRadius = 12f;

        private static readonly Dictionary<long, SailingState> ServerStates = new Dictionary<long, SailingState>();
        private static bool _rpcRegistered;
        private static Vector3 _authoritativeWindDirection;
        private static float _authoritativeWindIntensity;
        private static float _authoritativeWindReceivedAt;
        private static bool _hasAuthoritativeWind;
        private float _nextReportTime;

        private struct SailingState
        {
            public SailingState(Vector3 heading, bool inOcean, float receivedAt)
            {
                Heading = heading;
                InOcean = inOcean;
                ReceivedAt = receivedAt;
            }

            public Vector3 Heading { get; }
            public bool InOcean { get; }
            public float ReceivedAt { get; }
        }

        public static void Initialize()
        {
            GameObject networkObject = new GameObject("Njoror_SailingStateNetwork");
            DontDestroyOnLoad(networkObject);
            networkObject.AddComponent<SailingStateNetwork>();
            NjororPlugin.LogDebug("[Njoror] Sailing-state network initialized.");
        }

        private void Update()
        {
            EnsureRpcRegistered();

            if (ZNet.instance == null || ZNet.instance.IsServer() || Time.unscaledTime < _nextReportTime)
                return;

            _nextReportTime = Time.unscaledTime + ReportIntervalSeconds;
            ReportLocalSailingState();
        }

        private static void EnsureRpcRegistered()
        {
            if (_rpcRegistered || ZRoutedRpc.instance == null)
                return;

            _rpcRegistered = true;

            try
            {
                ZRoutedRpc.instance.Register<bool, Vector3>(SailingStateRpc, RPC_SailingState);
            }
            catch (ArgumentException)
            {
                // Already registered by a previous session or instance
            }
            catch (Exception ex)
            {
                NjororPlugin.Log?.LogWarning($"[Njoror] Failed to register SailingState RPC: {ex.Message}");
            }

            try
            {
                ZRoutedRpc.instance.Register<Vector3, float>(AuthoritativeWindRpc, RPC_AuthoritativeWind);
            }
            catch (ArgumentException)
            {
                // Already registered by a previous session or instance
            }
            catch (Exception ex)
            {
                NjororPlugin.Log?.LogWarning($"[Njoror] Failed to register AuthoritativeWind RPC: {ex.Message}");
            }

            NjororPlugin.LogDebug("[Njoror] Sailing-state and authoritative-wind RPCs registered.");
        }

        private static void ReportLocalSailingState()
        {
            Player? localPlayer = Player.m_localPlayer;
            Ship? ship = localPlayer != null ? localPlayer.GetControlledShip() : null;
            string detectionMethod = "ship helm";
            if (ship == null && localPlayer != null)
            {
                ship = localPlayer.GetStandingOnShip();
                detectionMethod = "GetStandingOnShip";
            }

            if (ship == null && localPlayer != null)
            {
                ship = FindNearbyShip(localPlayer.transform.position);
                detectionMethod = "nearby ship fallback";
            }

            bool isSailing = ship != null;
            Vector3 heading = ship != null ? FlattenAndNormalize(ship.transform.forward) : Vector3.zero;

            ZRoutedRpc.instance.InvokeRoutedRPC(0L, SailingStateRpc, isSailing, heading);
            NjororPlugin.LogDebug($"[Njoror][Diag] Reported local sailing state: active={isSailing}, heading={heading}, detection={detectionMethod}.");
        }

        private static Ship? FindNearbyShip(Vector3 playerPosition)
        {
            Ship? closestShip = null;
            float closestDistanceSquared = ShipDetectionRadius * ShipDetectionRadius;
            foreach (Ship candidate in UnityEngine.Object.FindObjectsOfType<Ship>())
            {
                if (candidate == null)
                    continue;

                float distanceSquared = (candidate.transform.position - playerPosition).sqrMagnitude;
                if (distanceSquared < closestDistanceSquared)
                {
                    closestShip = candidate;
                    closestDistanceSquared = distanceSquared;
                }
            }

            return closestShip;
        }

        private static void RPC_SailingState(long sender, bool reportedSailing, Vector3 reportedHeading)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer())
                return;

            ZNetPeer? peer = ZNet.instance.GetPeer(sender);
            if (peer == null || peer.m_characterID.IsNone())
            {
                NjororPlugin.LogDebug($"[Njoror][Diag] Rejected sailing state from {sender}: no connected character.");
                return;
            }

            ZDO? characterZdo = ZDOMan.instance != null ? ZDOMan.instance.GetZDO(peer.m_characterID) : null;
            if (characterZdo == null)
            {
                NjororPlugin.LogDebug($"[Njoror][Diag] Rejected sailing state from {sender}: character ZDO unavailable.");
                return;
            }

            if (!reportedSailing)
            {
                ServerStates.Remove(sender);
                NjororPlugin.LogDebug($"[Njoror][Diag] Cleared sailing state for {sender}.");
                return;
            }

            Vector3 heading = FlattenAndNormalize(reportedHeading);
            if (heading.sqrMagnitude < MinimumHeadingMagnitude * MinimumHeadingMagnitude)
            {
                NjororPlugin.LogDebug($"[Njoror][Diag] Rejected sailing state from {sender}: invalid heading.");
                return;
            }

            Vector3 playerPosition = characterZdo.GetPosition();
            bool inOcean = WorldGenerator.instance != null &&
                           WorldGenerator.instance.GetBiome(playerPosition.x, playerPosition.z) == Heightmap.Biome.Ocean;
            ServerStates[sender] = new SailingState(heading, inOcean, Time.unscaledTime);
            NjororPlugin.LogDebug($"[Njoror][Diag] Accepted sailing state from {sender}: ocean={inOcean}, heading={heading}.");
        }

        private static void RPC_AuthoritativeWind(long sender, Vector3 direction, float intensity)
        {
            if (ZNet.instance == null || ZNet.instance.IsServer())
                return;

            direction = FlattenAndNormalize(direction);
            if (direction.sqrMagnitude < MinimumHeadingMagnitude * MinimumHeadingMagnitude || intensity < 0.05f || intensity > 1f)
            {
                NjororPlugin.LogDebug($"[Njoror][Diag] Rejected authoritative wind from {sender}: invalid payload.");
                return;
            }

            _authoritativeWindDirection = direction;
            _authoritativeWindIntensity = intensity;
            _authoritativeWindReceivedAt = Time.unscaledTime;
            _hasAuthoritativeWind = true;
            NjororPlugin.LogDebug($"[Njoror][Diag] Received authoritative wind: direction={direction}, intensity={intensity:F3}.");
        }

        public static void BroadcastAuthoritativeWind(Vector3 direction, float intensity)
        {
            if (ZNet.instance == null || !ZNet.instance.IsServer() || ZRoutedRpc.instance == null)
                return;

            foreach (ZNetPeer peer in ZNet.instance.GetPeers())
            {
                if (peer != null)
                    ZRoutedRpc.instance.InvokeRoutedRPC(peer.m_uid, AuthoritativeWindRpc, direction, intensity);
            }

            NjororPlugin.LogDebug($"[Njoror][Diag] Broadcast authoritative wind: direction={direction}, intensity={intensity:F3}.");
        }

        public static bool TryGetAuthoritativeWind(out Vector3 direction, out float intensity)
        {
            direction = Vector3.zero;
            intensity = 0f;

            float expirySeconds = Mathf.Max(MinimumWindExpirySeconds, ModConfig.CheckDeflectTimeSeconds.Value + 2f);
            if (!_hasAuthoritativeWind || Time.unscaledTime - _authoritativeWindReceivedAt > expirySeconds)
                return false;

            direction = _authoritativeWindDirection;
            intensity = _authoritativeWindIntensity;
            return true;
        }

        public static bool TryGetActiveSailingState(out Vector3 heading, out bool inOcean)
        {
            heading = Vector3.zero;
            inOcean = false;

            if (ZNet.instance == null || !ZNet.instance.IsServer())
                return false;

            long selectedPeer = 0L;
            float newestStateTime = float.MinValue;
            foreach (KeyValuePair<long, SailingState> pair in ServerStates)
            {
                if (Time.unscaledTime - pair.Value.ReceivedAt > StateExpirySeconds)
                    continue;

                if (pair.Value.ReceivedAt > newestStateTime)
                {
                    selectedPeer = pair.Key;
                    newestStateTime = pair.Value.ReceivedAt;
                    heading = pair.Value.Heading;
                    inOcean = pair.Value.InOcean;
                }
            }

            if (selectedPeer == 0L)
                return false;

            return true;
        }

        private static Vector3 FlattenAndNormalize(Vector3 vector)
        {
            vector.y = 0f;
            return vector.sqrMagnitude > 0f ? vector.normalized : Vector3.zero;
        }
    }
}