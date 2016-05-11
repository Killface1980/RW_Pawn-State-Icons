namespace PSI
{
    internal class ModSettings
    {
        public float iconSize = 1f;
        public float iconSizeMult = 1f;
        public float iconDistanceX = 1f;
        public float iconDistanceY = 1f;
        public float iconOffsetX = 1f;
        public float iconOffsetY = 1f;

        public int iconsInColumn = 3;
        public bool iconsHorizontal;
        public bool iconsScreenScale = true;
        public string iconSet = "default";

        public bool show_TargetPoint = true;
        public bool show_Aggressive = true;
        public bool show_Dazed = true;
        public bool show_Leave = true;
        public bool show_Draft = true;
        public bool show_Idle = true;
        public bool show_Unarmed = true;
        public bool show_Hungry = true;
        public bool show_Sad = true;
        public bool show_Tired = true;
        public bool show_Disease = true;
        public bool show_Effectiveness = true;
        public bool show_Bloodloss = true;
        public bool show_Hot = true;
        public bool show_Cold = true;
        public bool show_Naked = true;
        public bool show_Drunk = true;
        public bool show_ApparelHealth = true;
        public bool show_Pacific = true;
        public bool show_Prosthophile = true;
        public bool show_Prosthophobe = true;
        public bool show_NightOwl = true;
        public bool show_Greedy = true;
        public bool show_Jealous = true;
        public bool show_Lovers = true;
        public bool show_DeadColonists = true;
        public bool show_LeftUnburied = true;

        public float limit_MoodLess = 0.25f;
        public float limit_FoodLess = 0.25f;
        public float limit_RestLess = 0.25f;
        public float limit_EfficiencyLess = 0.33f;
        public float limit_DiseaseLess = 1f;
        public float limit_BleedMult = 3f;
        public float limit_ApparelHealthLess = 0.33f;
        public float limit_TempComfortOffset;
    }
}
