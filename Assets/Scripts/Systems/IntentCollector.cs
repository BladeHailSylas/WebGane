using System;
using System.Collections.Generic;
using UnityEngine;

namespace Intents
{
    public class IntentCollector
    {
        private List<IIntent> _intentCluster = new();

        public bool QueueIntent(IIntent intent)
        {
            try
            {
                _intentCluster.Add(intent);
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("Error in QueueIntent: " + ex.Message);
                return false;
            }
        }

        void OnEnable()
        {
            BattleCore.Ticker.OnTick += TickHandler;
        }

        void OnDisable()
        {
            BattleCore.Ticker.OnTick -= TickHandler;
        }
        void TickHandler(ushort tick)
        {
            if (_intentCluster.Count > 0)
            {
                BattleCore.Validator.GetFlush(_intentCluster.ToArray());
            }
            _intentCluster.Clear();
        }
    }

    public struct MoveIntent : IIntent
    {
        public ushort OwnerID { get; set; }
        public int IntentID { get; set; }
        public IntentType Type { get; set; } 
        public ushort GeneratedTick { get; set; }
        public FixedVector2 Movement { get; set; }

        public MoveIntent(ushort ownerID, int intentID, ushort generatedTick, FixedVector2 movement)
        {
            OwnerID = ownerID;
            IntentID = intentID;
            Type = IntentType.Move;
            GeneratedTick = generatedTick;
            Movement = movement;
        }
    }

    public enum IntentType
    {
        None, Move, Cast,
    }
    public interface IIntent
    {
        public ushort OwnerID { get; }
        public int IntentID { get; }
        public IntentType Type { get; }
        public ushort GeneratedTick { get; }
    }
}