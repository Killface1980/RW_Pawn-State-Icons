using UnityEngine;
using Verse;

namespace PSI
{
    // ReSharper disable once UnusedMember.Global
    internal class Initializer : ITab
    {
        public Initializer()
        {
            Log.Message("Initialized the Pawn State Icons mod");

            var iconControllerObject = new GameObject("Initializer");

            iconControllerObject.AddComponent<InitializerBehavior>();

            Object.DontDestroyOnLoad(iconControllerObject);
        }

        protected override void FillTab()
        {
        }
    }
}
