using System;
using System.Collections.Generic;
using UnityEngine;

namespace Intents
{
    public class IntentCollector
    {
        private List<IIntent> _intentCluster = new();
        public static IntentCollector Instance { get; private set; }
        public bool QueueIntent(IIntent intent)
        {
            try
            {
                if (intent.Type == IntentType.Cast || intent.Type == IntentType.Move) _intentCluster.Add(intent);
                else throw new UndefinedIntentTypeException($"Cannot find such Intent Type: {intent.Type}. Probably typo.");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError("Error in QueueIntent: " + ex.Message);
                return false;
            }
        }

        void Awake()
        {
            if(Instance is null) Instance = this;
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

    public class UndefinedIntentTypeException : Exception
    {
        public UndefinedIntentTypeException()
        {
            
        }

        public UndefinedIntentTypeException(string msg) : base(msg)
        {
            
        }
    }
}