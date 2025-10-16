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
        private void TickHandler(ushort tick)
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
        public ushort OwnerID { get; }
        public int IntentID { get; }
        public IntentType Type { get; }
        public ushort GeneratedTick { get; }
        public FixedVector2 Movement { get; private set; }

        public ushort MoverID { get; }

    public MoveIntent(ushort ownerID, int intentID, ushort generatedTick, FixedVector2 movement, ushort moverID)
        {
            OwnerID = ownerID;
            IntentID = intentID;
            Type = IntentType.Move;
            GeneratedTick = generatedTick;
            Movement = movement;
            MoverID = moverID;
        }
    }

    public struct SkillIntent
    {
        
    }
    //IntentOrchestrator를 IntentRouter로 변환하고 기능을 축소한다, CastIntent도 전면 폐기하고 Intent 파이프라인을 간결하게 만든다
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