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
}