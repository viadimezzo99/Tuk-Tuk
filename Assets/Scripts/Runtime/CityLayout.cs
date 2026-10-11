namespace TukTuk.Gameplay
{
    /// <summary>
    /// CONTRATTO DELLE MISURE DEL MONDO (metri). Unico punto di verità per strada, marciapiedi,
    /// facciate, lotti e arredi: nessun sistema usa numeri "a occhio".
    ///
    /// Sezione trasversale (asse X, 0 = centro strada):
    ///   strada 0..5.0 | marciapiede 5.0..7.0 (cordolo verticale, nessuna rampa) | facciate da 7.0
    ///   pedoni a 6.0 | banchi/fioriere a 5.5 | alberi e pali a 6.4 | centro del lotto a 10.0
    ///
    /// Lungo la strada (asse Z) il mondo è diviso in blocchi di BlockLength metri,
    /// ognuno composto da LotsPerBlock lotti di LotWidth metri.
    /// </summary>
    public static class CityLayout
    {
        // Strada
        public const float RoadWidth = 10f;
        public const float RoadHalfWidth = RoadWidth * 0.5f;
        public const int LaneCount = 4;
        public const float LaneWidth = RoadWidth / LaneCount;

        // Marciapiede: cordolo verticale, senza rampe di accesso
        public const float RampRun = 0f;
        public const float CurbHeight = 0.4f;
        public const float SidewalkWidth = 2f;

        // Facciate e lotti edificio (sempre della stessa misura: i modelli 3D sono costruiti su questa)
        public const float FacadeX = RoadHalfWidth + RampRun + SidewalkWidth;
        public const float LotWidth = 6f;       // lungo la strada (Z)
        public const float LotDepth = 6f;       // dalla facciata verso l'esterno (X)
        public const float BuildingCenterX = FacadeX + LotDepth * 0.5f;

        // Blocco ripetuto lungo la strada
        public const int LotsPerBlock = 3;
        public const float BlockLength = LotWidth * LotsPerBlock; // 18 m

        // Posizioni sul marciapiede (asse X)
        public const float SidewalkCenterX = RoadHalfWidth + RampRun + SidewalkWidth * 0.5f; // pedoni
        public const float StallX = RoadHalfWidth + 0.5f;   // banchi, fioriere, bidoni (lato strada)
        public const float OuterX = FacadeX - 0.6f;         // alberi e pali (lato edifici)
    }
}
