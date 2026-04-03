using System.Collections.Generic;
using TopSpeed.Network;
using TopSpeed.Protocol;

namespace TopSpeed.Core.Multiplayer
{
    internal sealed partial class MultiplayerCoordinator
    {
        public void HandleProtocolMessage(PacketProtocolMessage message)
        {
            _chatFlow.HandleProtocolMessage(message);
        }

        internal void HandleProtocolMessageCore(PacketProtocolMessage message)
        {
            if (message == null)
                return;

            var effects = new List<PacketEffect>();
            AddProtocolMessageEffects(message, effects);

            if (!string.IsNullOrWhiteSpace(message.Message))
                effects.Add(PacketEffect.Speak(message.Message));

            DispatchPacketEffects(effects);
        }

        private static void AddProtocolMessageEffects(PacketProtocolMessage message, List<PacketEffect> effects)
        {
            switch (message.Code)
            {
                case ProtocolMessageCode.ServerPlayerConnected:
                    effects.Add(PacketEffect.PlaySound("online.ogg"));
                    effects.Add(PacketEffect.AddConnectionHistory(message.Message));
                    break;

                case ProtocolMessageCode.ServerPlayerDisconnected:
                    effects.Add(PacketEffect.PlaySound("offline.ogg"));
                    effects.Add(PacketEffect.AddConnectionHistory(message.Message));
                    break;

                case ProtocolMessageCode.Chat:
                    effects.Add(PacketEffect.PlaySound("chat.ogg"));
                    effects.Add(PacketEffect.AddGlobalChatHistory(message.Message));
                    break;

                case ProtocolMessageCode.RoomChat:
                    effects.Add(PacketEffect.PlaySound("room_chat.ogg"));
                    effects.Add(PacketEffect.AddRoomChatHistory(message.Message));
                    break;

                default:
                    effects.Add(PacketEffect.AddRoomEventHistory(message.Message));
                    break;
            }
        }
    }
}

